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

        private static readonly string[] StrongBodyKeywords =
        {
            "body", "torso", "skin", "head", "neck", "chest", "spine", "pelvis",
            "hip", "leg", "foot", "thigh", "calf", "knee", "face", "eye", "jaw",
            "mouth", "teeth", "hair"
        };

        private static readonly string[] WeakBodyKeywords =
        {
            "arm", "hand", "finger", "thumb", "wrist", "elbow", "shoulder",
            "clavicle"
        };

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
                HuntsmanLootPlugin.Log.LogWarning(
                    "[HuntsmanLoot] Huntsman weapon visual not found — keeping native shotgun visible.");
            }
        }

        // ── Seletor por score: percorre TODA a hierarquia sem pular subarvores ──
        // Arm/hand penalizam o NO como candidato mas os FILHOS sao sempre explorados.
        // So pode vencer um no com sinal de arma, mesh renderizavel e score > 0.
        static GameObject FindWeaponVisual(Transform root)
        {
            WeaponVisualCandidate bestCandidate = null;

            var queue = new Queue<Transform>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var t = queue.Dequeue();
                var candidate = EvaluateWeaponCandidate(t, root);
                LogWeaponCandidate(candidate);

                if (candidate.Accepted && IsBetterWeaponCandidate(candidate, bestCandidate))
                    bestCandidate = candidate;

                // Sempre descer nos filhos — NUNCA pular subarvore
                for (int i = 0; i < t.childCount; i++)
                    queue.Enqueue(t.GetChild(i));
            }

            if (bestCandidate != null)
                HuntsmanLootPlugin.Log.LogInfo(
                    $"[HuntsmanLoot] Selected Huntsman weapon visual:" +
                    $" {bestCandidate.Path} (score={bestCandidate.Score})");
            else
                HuntsmanLootPlugin.Log.LogInfo("[HuntsmanLoot] No valid Huntsman weapon visual found");

            return bestCandidate?.Transform.gameObject;
        }

        static WeaponVisualCandidate EvaluateWeaponCandidate(Transform t, Transform root)
        {
            string name = t.name.ToLowerInvariant();
            string path = GetTransformPath(t);
            string pathLower = path.ToLowerInvariant();

            var stats = GetVisualStats(t);
            int score = 0;

            bool directWeaponSignal = AddWeaponScore(name, true, ref score);
            bool pathWeaponSignal = AddWeaponScore(pathLower, false, ref score);
            bool hasWeaponSignal = directWeaponSignal || pathWeaponSignal;

            bool strongBody = ContainsAny(name, StrongBodyKeywords);
            bool weakBody = ContainsAny(name, WeakBodyKeywords);

            if (strongBody) score -= 45;
            if (weakBody) score -= 18;

            if (stats.RenderableMeshCount > 0) score += 8;
            if (stats.RenderableMeshCount > 1) score += 4;
            if (stats.RendererCount > 3) score -= 4;

            bool accepted = false;
            string reason;

            if (t == root)
                reason = "root-not-weapon";
            else if (!stats.HasValidVisual)
                reason = "no-valid-mesh-or-renderer";
            else if (!hasWeaponSignal)
                reason = "no-weapon-signal";
            else if (strongBody && !directWeaponSignal)
                reason = "body-part";
            else if (score <= 0)
                reason = "score<=0";
            else
            {
                accepted = true;
                reason = "ok";
            }

            return new WeaponVisualCandidate
            {
                Transform = t,
                Path = path,
                Score = score,
                RendererCount = stats.RendererCount,
                MeshCount = stats.MeshCount,
                Accepted = accepted,
                Reason = reason,
                DirectWeaponSignal = directWeaponSignal,
                Depth = GetDepth(t, root)
            };
        }

        static bool AddWeaponScore(string text, bool directName, ref int score)
        {
            if (text.Contains("shotgun"))
            {
                score += directName ? 34 : 11;
                return true;
            }

            if (text.Contains("gun") || text.Contains("rifle") ||
                text.Contains("weapon") || text.Contains("firearm"))
            {
                score += directName ? 30 : 10;
                return true;
            }

            if (text.Contains("barrel") || text.Contains("stock") ||
                text.Contains("trigger") || text.Contains("pump") ||
                text.Contains("receiver") || text.Contains("magazine"))
            {
                score += directName ? 14 : 5;
                return true;
            }

            if (text.Contains("muzzle") || text.Contains("sight") ||
                text.Contains("scope") || text.Contains("ammo") ||
                text.Contains("cannon"))
            {
                score += directName ? 10 : 3;
                return true;
            }

            return false;
        }

        static VisualStats GetVisualStats(Transform t)
        {
            var stats = new VisualStats();

            foreach (var mr in t.GetComponentsInChildren<MeshRenderer>(true))
            {
                stats.RendererCount++;
                var mf = mr.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                    stats.RenderableMeshCount++;
            }

            foreach (var smr in t.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                stats.RendererCount++;
                if (smr.sharedMesh != null)
                {
                    stats.MeshCount++;
                    stats.RenderableMeshCount++;
                }
            }

            foreach (var mf in t.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh != null)
                    stats.MeshCount++;
            }

            return stats;
        }

        static void LogWeaponCandidate(WeaponVisualCandidate candidate)
        {
            if (candidate.RendererCount == 0 &&
                candidate.MeshCount == 0 &&
                candidate.Score <= 0 &&
                !candidate.DirectWeaponSignal)
                return;

            string status = candidate.Accepted ? "accepted" : "rejected";
            HuntsmanLootPlugin.Log.LogInfo(
                $"[HuntsmanLoot] Candidate: path={candidate.Path}" +
                $" score={candidate.Score} renderers={candidate.RendererCount}" +
                $" meshes={candidate.MeshCount} {status} reason={candidate.Reason}");
        }

        static bool IsBetterWeaponCandidate(
            WeaponVisualCandidate candidate,
            WeaponVisualCandidate bestCandidate)
        {
            if (bestCandidate == null) return true;
            if (candidate.Score != bestCandidate.Score)
                return candidate.Score > bestCandidate.Score;
            if (candidate.DirectWeaponSignal != bestCandidate.DirectWeaponSignal)
                return candidate.DirectWeaponSignal;
            if (candidate.RendererCount != bestCandidate.RendererCount)
                return candidate.RendererCount > bestCandidate.RendererCount;
            return candidate.Depth > bestCandidate.Depth;
        }

        static bool ContainsAny(string text, string[] keywords)
        {
            foreach (var keyword in keywords)
                if (text.Contains(keyword))
                    return true;
            return false;
        }

        static int GetDepth(Transform t, Transform root)
        {
            int depth = 0;
            var cur = t;
            while (cur != null && cur != root)
            {
                depth++;
                cur = cur.parent;
            }
            return depth;
        }

        private sealed class WeaponVisualCandidate
        {
            internal Transform Transform;
            internal string Path;
            internal int Score;
            internal int RendererCount;
            internal int MeshCount;
            internal bool Accepted;
            internal string Reason;
            internal bool DirectWeaponSignal;
            internal int Depth;
        }

        private struct VisualStats
        {
            internal int RendererCount;
            internal int MeshCount;
            internal int RenderableMeshCount;
            internal bool HasValidVisual => RenderableMeshCount > 0;
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
