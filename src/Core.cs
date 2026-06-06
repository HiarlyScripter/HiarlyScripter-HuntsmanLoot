using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace HuntsmanLoot
{
    [BepInPlugin("com.hiarlyscripter.huntsmanloot", "Huntsman Loot", "1.1.3")]
    public sealed class HuntsmanLootPlugin : BaseUnityPlugin
    {
        internal static HuntsmanLootPlugin Instance { get; private set; }
        internal static ManualLogSource    Log      { get; private set; }

        // ── Configs ───────────────────────────────────────────────────────────
        internal static ConfigEntry<int>  DropChance;
        internal static ConfigEntry<bool> BerserkerOnly;
        internal static ConfigEntry<bool> MasterClientOnly;
        internal static ConfigEntry<bool> RandomizeAmmo;
        internal static ConfigEntry<bool> EnableDebugLogging;

        // ── Reflexao: campos internos do jogo ─────────────────────────────────
        internal static readonly FieldInfo _hasHealthField       = AccessTools.Field(typeof(Enemy),       "HasHealth");
        internal static readonly FieldInfo _healthField          = AccessTools.Field(typeof(Enemy),       "Health");
        internal static readonly FieldInfo _hpCurrentField       = AccessTools.Field(typeof(EnemyHealth), "healthCurrent");
        internal static readonly FieldInfo _numberOfBulletsField = AccessTools.Field(typeof(ItemGun),     "numberOfBullets");
        internal static readonly FieldInfo _enemyParentField     = AccessTools.Field(typeof(EnemyParent), "Enemy");

        private void Awake()
        {
            Instance = this;
            Log      = Logger;

            DropChance = Config.Bind(
                "Drop", "DropChance", 100,
                new ConfigDescription(
                    "Chance (%) de a arma cair quando o Huntsman morre. 100 = sempre, 1 = rarissimo.",
                    new AcceptableValueRange<int>(1, 100)));

            BerserkerOnly = Config.Bind(
                "Drop", "BerserkerOnly", false,
                "true  = arma so cai de Huntsman no modo berserk (requer BerserkerEnemies).\n" +
                "false = cai de qualquer Huntsman (padrao).");

            MasterClientOnly = Config.Bind(
                "Drop", "MasterClientOnly", true,
                "true  = apenas o HOST processa o drop (evita duplicatas em multiplayer).\n" +
                "false = qualquer cliente pode gerar o drop.");

            RandomizeAmmo = Config.Bind(
                "Drop", "RandomizeAmmo", true,
                "true  = arma cai com municao aleatoria (entre 1 e o maximo).\n" +
                "false = sempre cai com municao completa.");

            EnableDebugLogging = Config.Bind(
                "Debug", "EnableDebugLogging", false,
                "true  = logs tecnicos detalhados para diagnostico.\n" +
                "false = logs normais reduzidos.");

            LogReflectionStatus();

            new Harmony("com.hiarlyscripter.huntsmanloot").PatchAll(typeof(HuntsmanPatches));
            Log.LogInfo("[HuntsmanLoot] v1.1.3 carregado. Patch Harmony aplicado.");
        }

        internal static bool DebugLoggingEnabled => EnableDebugLogging != null && EnableDebugLogging.Value;

        internal static void DebugLog(string message)
        {
            if (DebugLoggingEnabled)
                Log?.LogInfo(message);
        }

        internal static void DebugWarning(string message)
        {
            if (DebugLoggingEnabled)
                Log?.LogWarning(message);
        }

        private void LogReflectionStatus()
        {
            DebugLog($"[HuntsmanLoot] Reflexao: " +
                     $"HasHealth={_hasHealthField != null}, " +
                     $"Health={_healthField != null}, " +
                     $"healthCurrent={_hpCurrentField != null}, " +
                     $"numberOfBullets={_numberOfBulletsField != null}");

            if (_hasHealthField == null || _healthField == null || _hpCurrentField == null)
                Log.LogError("[HuntsmanLoot] ATENCAO: campos de saude nao encontrados — deteccao de morte pode falhar!");
        }

        // ── Ajusta municao e sincroniza barra visual (ItemBattery) ────────────
        internal static void ApplyAmmoSync(GameObject spawned)
        {
            if (spawned == null) return;

            var gun = spawned.GetComponentInChildren<ItemGun>();
            if (gun == null || _numberOfBulletsField == null) return;

            int current = (int)_numberOfBulletsField.GetValue(gun);
            var battery = spawned.GetComponentInChildren<ItemBattery>();
            int maxAmmo = Mathf.Max(1, GetAmmoCapacity(current, battery));
            int finalAmmo;

            if (RandomizeAmmo.Value)
            {
                finalAmmo = RollWeightedAmmo(maxAmmo);
                DebugLog($"[HuntsmanLoot] Ammo randomized: current={current} max={maxAmmo} final={finalAmmo}");
            }
            else
            {
                finalAmmo = maxAmmo;
                DebugLog($"[HuntsmanLoot] Ammo kept full: current={current} max={maxAmmo}");
            }

            finalAmmo = Mathf.Clamp(finalAmmo, 1, maxAmmo);
            _numberOfBulletsField.SetValue(gun, finalAmmo);

            if (battery != null)
            {
                int pct = Mathf.Clamp((int)Math.Round((float)finalAmmo / maxAmmo * 100f), 0, 100);
                battery.SetBatteryLife(pct);
                DebugLog($"[HuntsmanLoot] BatteryLife: {finalAmmo}/{maxAmmo} -> {pct}%");
            }

            Log.LogInfo($"[HuntsmanLoot] Ammo final: {finalAmmo}/{maxAmmo}");
        }

        static int GetAmmoCapacity(int current, ItemBattery battery)
        {
            if (battery != null && battery.batteryBars > 0)
                return battery.batteryBars;

            if (current > 0)
                return current;

            return 4;
        }

        static int RollWeightedAmmo(int maxAmmo)
        {
            int roll = UnityEngine.Random.Range(1, 101);
            return RollWeightedAmmoFromRoll(maxAmmo, roll);
        }

        internal static int RollWeightedAmmoFromRoll(int maxAmmo, int roll)
        {
            maxAmmo = Mathf.Max(1, maxAmmo);
            if (maxAmmo == 1) return 1;

            int[] weights = GetAmmoWeights(maxAmmo);
            roll = Mathf.Clamp(roll, 1, 100);

            int accumulated = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                accumulated += weights[i];
                if (roll <= accumulated)
                    return Mathf.Clamp(i + 1, 1, maxAmmo);
            }

            return maxAmmo;
        }

        internal static int[] GetAmmoWeights(int maxAmmo)
        {
            maxAmmo = Mathf.Max(1, maxAmmo);
            switch (maxAmmo)
            {
                case 1:
                    return new[] { 100 };
                case 2:
                    return new[] { 75, 25 };
                case 3:
                    return new[] { 45, 40, 15 };
                case 4:
                    return new[] { 32, 30, 26, 12 };
                case 5:
                    return new[] { 25, 25, 22, 18, 10 };
                case 6:
                    return new[] { 22, 22, 20, 16, 10, 10 };
                default:
                    return BuildAmmoWeights(maxAmmo);
            }
        }

        static int[] BuildAmmoWeights(int maxAmmo)
        {
            var weights = new int[maxAmmo];
            weights[maxAmmo - 1] = 10;

            int remaining = 90;
            int nonFullSlots = maxAmmo - 1;
            int baseWeight = remaining / nonFullSlots;
            int remainder = remaining - (baseWeight * nonFullSlots);

            for (int i = 0; i < nonFullSlots; i++)
                weights[i] = baseWeight + (i < remainder ? 1 : 0);

            return weights;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  HuntsmanPatches — patches Harmony
    // ──────────────────────────────────────────────────────────────────────────
    [HarmonyPatch]
    internal static class HuntsmanPatches
    {
        private const string SHOTGUN_PATH = "Items/Item Gun Shotgun";

        // Runtime capture encerrado. A rota atual usa Mesh/Material nativos do jogo.
        static void ClearPendingVisual()
        {
        }

        // ── 1. POST-DESPAWN: detecta morte e dropa a arma ─────────────────────
        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnemyParent), "Despawn")]
        static void OnHuntsmanDespawnPost(EnemyParent __instance)
        {
            if (__instance.enemyName != "Huntsman") return;

            HuntsmanLootPlugin.DebugLog("[HuntsmanLoot] Huntsman detectado no Despawn.");

            if (HuntsmanLootPlugin.MasterClientOnly.Value && !SemiFunc.IsMasterClientOrSingleplayer())
            {
                ClearPendingVisual();
                HuntsmanLootPlugin.DebugLog("[HuntsmanLoot] Nao eh master client — drop ignorado.");
                return;
            }

            if (HuntsmanLootPlugin._enemyParentField == null)
            {
                ClearPendingVisual();
                HuntsmanLootPlugin.Log.LogError("[HuntsmanLoot] _enemyParentField null — drop abortado.");
                return;
            }

            var enemy = (Enemy)HuntsmanLootPlugin._enemyParentField.GetValue(__instance);
            if (enemy == null)
            {
                ClearPendingVisual();
                HuntsmanLootPlugin.Log.LogWarning("[HuntsmanLoot] enemy null — drop abortado.");
                return;
            }

            if (HuntsmanLootPlugin._hasHealthField == null ||
                HuntsmanLootPlugin._healthField    == null ||
                HuntsmanLootPlugin._hpCurrentField == null)
            {
                ClearPendingVisual();
                HuntsmanLootPlugin.Log.LogError("[HuntsmanLoot] Campos de saude inacessiveis — drop abortado.");
                return;
            }

            bool hasHealth = (bool)HuntsmanLootPlugin._hasHealthField.GetValue(enemy);
            if (!hasHealth)
            {
                HuntsmanLootPlugin.DebugLog("[HuntsmanLoot] Enemy sem health component — ignorando.");
                return;
            }

            var health = (EnemyHealth)HuntsmanLootPlugin._healthField.GetValue(enemy);
            if (health == null)
            {
                ClearPendingVisual();
                HuntsmanLootPlugin.Log.LogWarning("[HuntsmanLoot] EnemyHealth null — drop abortado.");
                return;
            }

            int hp = (int)HuntsmanLootPlugin._hpCurrentField.GetValue(health);
            HuntsmanLootPlugin.DebugLog($"[HuntsmanLoot] HP ao despawnar: {hp}");

            if (hp > 0)
            {
                // Prefix nao teria setado _pendingWeaponVisual para hp>0, mas limpar por seguranca
                HuntsmanLootPlugin.DebugLog("[HuntsmanLoot] HP > 0 — despawn normal. Sem drop.");
                return;
            }

            int roll = UnityEngine.Random.Range(1, 101);
            if (roll > HuntsmanLootPlugin.DropChance.Value)
            {
                ClearPendingVisual();
                HuntsmanLootPlugin.DebugLog(
                    $"[HuntsmanLoot] Chance falhou (roll={roll} > {HuntsmanLootPlugin.DropChance.Value}%) — sem drop.");
                return;
            }

            HuntsmanLootPlugin.Log.LogInfo(
                $"[HuntsmanLoot] Morte confirmada! roll={roll} — iniciando drop.");

            if (HuntsmanLootPlugin.BerserkerOnly.Value)
                AttemptBerserkerDrop(enemy);
            else
                DropRifle(enemy);
        }

        static void AttemptBerserkerDrop(Enemy enemy)
        {
            var berserkAsm = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "BerserkerEnemies");

            if (berserkAsm == null)
            {
                HuntsmanLootPlugin.Log.LogWarning(
                    "[HuntsmanLoot] BerserkerOnly=true mas BerserkerEnemies nao detectado — dropando mesmo assim.");
                DropRifle(enemy);
                return;
            }

            var ctrlType = berserkAsm.GetType("BerserkerController");
            if (ctrlType == null)
            {
                HuntsmanLootPlugin.Log.LogWarning("[HuntsmanLoot] BerserkerController nao encontrado.");
                ClearPendingVisual();
                return;
            }

            var ctrl = ((UnityEngine.Component)enemy).GetComponentInParent(ctrlType);
            if (ctrl == null) { ClearPendingVisual(); return; }

            var flag = ctrlType.GetField("isBerserkerFlag");
            if (flag?.GetValue(ctrl) is bool isBerserker && isBerserker)
                DropRifle(enemy);
            else
                ClearPendingVisual();
        }

        static void DropRifle(Enemy enemy)
        {
            Transform origin = enemy.CustomValuableSpawnTransform ?? enemy.CenterTransform;
            if (origin == null)
            {
                ClearPendingVisual();
                HuntsmanLootPlugin.Log.LogError("[HuntsmanLoot] Posicao de spawn invalida — drop abortado.");
                return;
            }

            Vector3 pos = origin.position + Vector3.up * 0.3f;
            GameObject spawned;

            // ── Spawn da shotgun nativa (base funcional) ──────────────────────
            if (!SemiFunc.IsMultiplayer())
            {
                var prefab = Resources.Load<GameObject>(SHOTGUN_PATH);
                if (prefab == null)
                {
                    ClearPendingVisual();
                    HuntsmanLootPlugin.Log.LogError(
                        $"[HuntsmanLoot] Resources.Load falhou para '{SHOTGUN_PATH}'.");
                    return;
                }
                spawned = UnityEngine.Object.Instantiate(prefab, pos, Quaternion.identity);
                HuntsmanLootPlugin.DebugLog("[HuntsmanLoot] Arma spawnada (singleplayer).");
            }
            else
            {
                try
                {
                    spawned = Photon.Pun.PhotonNetwork.InstantiateRoomObject(
                        SHOTGUN_PATH, pos, Quaternion.identity, 0, null);
                }
                catch (Exception ex)
                {
                    ClearPendingVisual();
                    HuntsmanLootPlugin.Log.LogError(
                        $"[HuntsmanLoot] InstantiateRoomObject excecao: {ex.Message}");
                    return;
                }

                if (spawned == null)
                {
                    ClearPendingVisual();
                    HuntsmanLootPlugin.Log.LogError("[HuntsmanLoot] InstantiateRoomObject retornou null.");
                    return;
                }
                HuntsmanLootPlugin.DebugLog("[HuntsmanLoot] Arma spawnada (multiplayer).");
            }

            // Runtime capture/hierarquia encerrado: visual agora usa Mesh/Material nativos do jogo.
            ClearPendingVisual();

            // ── Customizacao visual ───────────────────────────────────────────
            HuntsmanLootPlugin.DebugLog("[HuntsmanLoot] Customizer START");
            try
            {
                HuntsmanRifleCustomizer.Apply(spawned, ((Component)enemy).gameObject, HuntsmanLootPlugin.Log);
                HuntsmanLootPlugin.DebugLog("[HuntsmanLoot] Customizer DONE");
            }
            catch (Exception exVis)
            {
                HuntsmanLootPlugin.Log.LogError($"[HuntsmanLoot] Customizer ERROR: {exVis.Message}");
            }

            // ── Municao (sincrono) ────────────────────────────────────────────
            try { HuntsmanLootPlugin.ApplyAmmoSync(spawned); }
            catch (Exception exAmmo) { HuntsmanLootPlugin.Log.LogError($"[HuntsmanLoot] ApplyAmmo ERROR: {exAmmo.Message}"); }
        }

        // ── 2. Suprime warning "Gun Hunter not found in itemDictionary" ───────
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UnityEngine.Debug), "LogWarning", new[] { typeof(object) })]
        static bool SuppressGunHunterWarning(object message)
        {
            if (message is string s &&
                s.Contains("Gun Hunter") &&
                s.Contains("not found in the itemDictionary"))
                return false;
            return true;
        }
    }
}
