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
    [BepInPlugin("com.hiarlyscripter.huntsmanloot", "Huntsman Loot", "1.1.2")]
    public sealed class HuntsmanLootPlugin : BaseUnityPlugin
    {
        internal static HuntsmanLootPlugin Instance { get; private set; }
        internal static ManualLogSource    Log      { get; private set; }

        // ── Configs ───────────────────────────────────────────────────────────
        internal static ConfigEntry<int>  DropChance;
        internal static ConfigEntry<bool> BerserkerOnly;
        internal static ConfigEntry<bool> MasterClientOnly;
        internal static ConfigEntry<bool> RandomizeAmmo;

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

            LogReflectionStatus();

            new Harmony("com.hiarlyscripter.huntsmanloot").PatchAll(typeof(HuntsmanPatches));
            Log.LogInfo("[HuntsmanLoot] v1.1.2 carregado. Patch Harmony aplicado.");
        }

        private void LogReflectionStatus()
        {
            Log.LogInfo($"[HuntsmanLoot] Reflexao: " +
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
            int finalAmmo;

            if (current <= 0)
            {
                finalAmmo = RandomizeAmmo.Value ? UnityEngine.Random.Range(1, 5) : 4;
                _numberOfBulletsField.SetValue(gun, finalAmmo);
                Log.LogInfo($"[HuntsmanLoot] Ammo zerada → forcada para {finalAmmo}");
            }
            else if (RandomizeAmmo.Value && current > 1)
            {
                finalAmmo = UnityEngine.Random.Range(1, current + 1);
                _numberOfBulletsField.SetValue(gun, finalAmmo);
                Log.LogInfo($"[HuntsmanLoot] Ammo randomizada: {current} → {finalAmmo}");
            }
            else
            {
                finalAmmo = current;
                Log.LogInfo($"[HuntsmanLoot] Ammo mantida: {finalAmmo}");
            }

            var battery = spawned.GetComponentInChildren<ItemBattery>();
            if (battery != null)
            {
                int bars = battery.batteryBars > 0 ? battery.batteryBars : 4;
                int pct  = Mathf.Clamp((int)Math.Round((float)finalAmmo / bars * 100f), 0, 100);
                battery.SetBatteryLife(pct);
                Log.LogInfo($"[HuntsmanLoot] BatteryLife: {finalAmmo}/{bars} → {pct}%");
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  HuntsmanPatches — patches Harmony
    // ──────────────────────────────────────────────────────────────────────────
    [HarmonyPatch]
    internal static class HuntsmanPatches
    {
        private const string SHOTGUN_PATH = "Items/Item Gun Shotgun";

        // Visual clonado antes do Despawn — consumido em DropRifle
        private static GameObject _pendingWeaponVisual;

        // ── 0. PRE-DESPAWN: captura visual da arma ANTES de o inimigo ser desativado ──
        [HarmonyPrefix]
        [HarmonyPatch(typeof(EnemyParent), "Despawn")]
        static void OnHuntsmanDespawnPre(EnemyParent __instance)
        {
            if (__instance.enemyName != "Huntsman") return;
            if (HuntsmanLootPlugin._enemyParentField == null) return;

            // Verificar se e morte (HP = 0)
            var enemy = (Enemy)HuntsmanLootPlugin._enemyParentField.GetValue(__instance);
            if (enemy == null) return;
            if (HuntsmanLootPlugin._healthField == null || HuntsmanLootPlugin._hpCurrentField == null) return;
            var health = (EnemyHealth)HuntsmanLootPlugin._healthField.GetValue(enemy);
            if (health == null) return;
            int hp = (int)HuntsmanLootPlugin._hpCurrentField.GetValue(health);
            if (hp > 0) return; // despawn normal, nao morte

            // Limpar pendente anterior (seguranca)
            ClearPendingVisual();

            // Buscar visual da arma na hierarquia do Huntsman
            var weaponGo = FindWeaponVisual(__instance.transform);
            if (weaponGo != null)
            {
                // Clonar AGORA, antes do Despawn destruir/desativar o inimigo
                var clone = UnityEngine.Object.Instantiate(weaponGo);
                clone.name = "HuntsmanWeaponVisual_Pending";
                clone.SetActive(false);
                UnityEngine.Object.DontDestroyOnLoad(clone);
                _pendingWeaponVisual = clone;
                HuntsmanLootPlugin.Log.LogInfo(
                    $"[HuntsmanLoot] Cached Huntsman weapon visual: {GetTransformPath(weaponGo.transform)}");
            }
            else
            {
                HuntsmanLootPlugin.Log.LogError(
                    "[HuntsmanLoot] Huntsman weapon visual not found — falling back to native shotgun.");
            }
        }

        // ── Seletor por score: percorre TODA a hierarquia sem pular subarvores ──
        // Arm/hand penalizam o NO como candidato mas os FILHOS sao sempre explorados.
        // So pode vencer um no com renderer real e score > 0.
        static GameObject FindWeaponVisual(Transform root)
        {
            GameObject bestCandidate = null;
            int        bestScore     = int.MinValue;

            var queue = new Queue<Transform>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var    t = queue.Dequeue();
                string n = t.name.ToLowerInvariant();

                // ── Score por palavra-chave ──────────────────────────────────
                int score = 0;

                // Positivo forte: nome de arma
                if (n.Contains("gun") || n.Contains("rifle") ||
                    n.Contains("weapon") || n.Contains("firearm"))
                    score += 10;
                else if (n.Contains("shotgun")) score += 8;
                else if (n.Contains("barrel"))  score += 5;
                else if (n.Contains("muzzle"))  score += 2; // geralmente sem mesh

                // Negativo: partes de corpo — penaliza o NO, mas NUNCA pula filhos
                if      (n.Contains("body")   || n.Contains("torso") || n.Contains("skin")) score -= 20;
                else if (n.Contains("head"))                                                 score -= 15;
                else if (n.Contains("arm")    || n.Contains("hand"))                        score -= 15;
                else if (n.Contains("leg")    || n.Contains("foot"))                        score -= 10;

                // ── Contar renderers reais na subarvore deste no ─────────────
                int rendererCount = 0, meshCount = 0;

                foreach (var mr in t.GetComponentsInChildren<MeshRenderer>(true))
                    if (mr.sharedMaterial != null) rendererCount++;
                foreach (var smr in t.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    if (smr.sharedMesh != null) { rendererCount++; meshCount++; }
                foreach (var mf in t.GetComponentsInChildren<MeshFilter>(true))
                    if (mf.sharedMesh != null) meshCount++;

                bool hasRenderer = rendererCount > 0;
                if (hasRenderer)        score += 5;
                if (rendererCount > 2)  score += 2;

                // ── Candidato valido: tem renderer E score > 0 ───────────────
                bool isCandidate = hasRenderer && score > 0;

                // ── Log de diagnostico (todos os nos com renderer ou score relevante) ──
                if (hasRenderer || score > 3)
                {
                    string status = isCandidate ? "accepted" : "rejected";
                    string reason = !hasRenderer ? "no-renderer"
                                  : score <= 0   ? "score<=0"
                                  : "ok";
                    HuntsmanLootPlugin.Log.LogInfo(
                        $"[HuntsmanLoot] Candidate: path={GetTransformPath(t)}" +
                        $" score={score} renderers={rendererCount} meshes={meshCount}" +
                        $" {status} reason={reason}");
                }

                if (isCandidate && score > bestScore)
                {
                    bestScore     = score;
                    bestCandidate = t.gameObject;
                }

                // Sempre descer nos filhos — NUNCA pular subarvore
                for (int i = 0; i < t.childCount; i++)
                    queue.Enqueue(t.GetChild(i));
            }

            if (bestCandidate != null)
                HuntsmanLootPlugin.Log.LogInfo(
                    $"[HuntsmanLoot] Selected Huntsman weapon visual:" +
                    $" {GetTransformPath(bestCandidate.transform)} (score={bestScore})");
            else
                HuntsmanLootPlugin.Log.LogInfo("[HuntsmanLoot] No valid Huntsman weapon visual found");

            return bestCandidate;
        }

        static string GetTransformPath(Transform t)
        {
            var parts = new List<string>();
            var cur = t;
            while (cur != null) { parts.Insert(0, cur.name); cur = cur.parent; }
            return string.Join("/", parts);
        }

        static void ClearPendingVisual()
        {
            if (_pendingWeaponVisual != null)
            {
                UnityEngine.Object.Destroy(_pendingWeaponVisual);
                _pendingWeaponVisual = null;
            }
        }

        // ── 1. POST-DESPAWN: detecta morte e dropa a arma ─────────────────────
        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnemyParent), "Despawn")]
        static void OnHuntsmanDespawnPost(EnemyParent __instance)
        {
            if (__instance.enemyName != "Huntsman") return;

            HuntsmanLootPlugin.Log.LogInfo("[HuntsmanLoot] Huntsman detectado no Despawn.");

            if (HuntsmanLootPlugin.MasterClientOnly.Value && !SemiFunc.IsMasterClientOrSingleplayer())
            {
                ClearPendingVisual();
                HuntsmanLootPlugin.Log.LogInfo("[HuntsmanLoot] Nao eh master client — drop ignorado.");
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
                HuntsmanLootPlugin.Log.LogInfo("[HuntsmanLoot] Enemy sem health component — ignorando.");
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
            HuntsmanLootPlugin.Log.LogInfo($"[HuntsmanLoot] HP ao despawnar: {hp}");

            if (hp > 0)
            {
                // Prefix nao teria setado _pendingWeaponVisual para hp>0, mas limpar por seguranca
                HuntsmanLootPlugin.Log.LogInfo("[HuntsmanLoot] HP > 0 — despawn normal. Sem drop.");
                return;
            }

            int roll = UnityEngine.Random.Range(1, 101);
            if (roll > HuntsmanLootPlugin.DropChance.Value)
            {
                ClearPendingVisual();
                HuntsmanLootPlugin.Log.LogInfo(
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
                HuntsmanLootPlugin.Log.LogInfo("[HuntsmanLoot] Arma spawnada (singleplayer).");
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
                HuntsmanLootPlugin.Log.LogInfo("[HuntsmanLoot] Arma spawnada (multiplayer).");
            }

            // ── Consumir visual pendente ──────────────────────────────────────
            var pendingVisual = _pendingWeaponVisual;
            _pendingWeaponVisual = null;

            // ── Customizacao visual ───────────────────────────────────────────
            HuntsmanLootPlugin.Log.LogInfo("[HuntsmanLoot] Customizer START");
            try
            {
                HuntsmanRifleCustomizer.Apply(spawned, pendingVisual, HuntsmanLootPlugin.Log);
                HuntsmanLootPlugin.Log.LogInfo("[HuntsmanLoot] Customizer DONE");
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
