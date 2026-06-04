using System;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HuntsmanLoot
{
    // Aplicado sobre a shotgun nativa JA spawnada.
    // Oculta renderers originais e anexa o visual real da arma do Huntsman.
    // Nao toca em PhysGrabObject, RoomVolumeCheck, Rigidbody, ItemGun, PhotonView.
    internal static class HuntsmanRifleCustomizer
    {
        private static readonly string[] _preserveKeywords =
            { "battery", "ammo", "bar", "ui", "icon", "screen", "text" };

        internal static void Apply(GameObject spawned, GameObject weaponVisual, ManualLogSource log)
        {
            if (spawned == null) return;

            // Verificar se o visual capturado tem renderer real
            bool hasValidVisual = weaponVisual != null && HasValidRenderer(weaponVisual);

            if (!hasValidVisual)
            {
                // Sem visual valido: manter shotgun intacta, apenas luz e identidade
                log.LogWarning("[HuntsmanLoot] Huntsman weapon visual not found — keeping native shotgun visible.");

                try { ApplyLight(spawned); }
                catch (Exception ex) { log.LogError($"[HuntsmanLoot] Customizer/Light: {ex.Message}"); }

                try { ApplyIdentity(spawned, log); }
                catch (Exception ex) { log.LogError($"[HuntsmanLoot] Customizer/Identity: {ex.Message}"); }

                return;
            }

            // Com visual valido: ocultar shotgun → anexar visual real → luz → identidade
            // 1. Ocultar renderers visuais originais da shotgun (so quando ha visual alternativo)
            try { HideOriginalRenderers(spawned, log); }
            catch (Exception ex) { log.LogError($"[HuntsmanLoot] Customizer/Hide: {ex.Message}"); }

            // 2. Anexar visual real da arma do Huntsman
            try { AttachWeaponVisual(spawned, weaponVisual, log); }
            catch (Exception ex) { log.LogError($"[HuntsmanLoot] Customizer/Attach: {ex.Message}"); }

            // 3. Luz teal sutil (glow identificador)
            try { ApplyLight(spawned); }
            catch (Exception ex) { log.LogError($"[HuntsmanLoot] Customizer/Light: {ex.Message}"); }

            // 4. Identidade
            try { ApplyIdentity(spawned, log); }
            catch (Exception ex) { log.LogError($"[HuntsmanLoot] Customizer/Identity: {ex.Message}"); }
        }

        // Verifica se o GameObject (ou qualquer filho) tem renderer real com material/mesh
        static bool HasValidRenderer(GameObject go)
        {
            foreach (var mr in go.GetComponentsInChildren<MeshRenderer>(true))
                if (mr.sharedMaterial != null) return true;
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (smr.sharedMesh != null) return true;
            return false;
        }

        // ── Ocultar renderers originais ────────────────────────────────────────
        // Desativa MeshRenderer e SkinnedMeshRenderer da shotgun base.
        // NAO destroi GameObjects, NAO remove componentes funcionais.
        // Preserva qualquer renderer cujo caminho contenha palavras-chave de UI/barra.

        static void HideOriginalRenderers(GameObject spawned, ManualLogSource log)
        {
            int hidden = 0, preserved = 0;

            foreach (var mr in spawned.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (ShouldPreserve(mr.transform)) { preserved++; }
                else { mr.enabled = false; hidden++; }
            }

            foreach (var smr in spawned.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (ShouldPreserve(smr.transform)) { preserved++; }
                else { smr.enabled = false; hidden++; }
            }

            log.LogInfo($"[HuntsmanLoot] Original shotgun renderers hidden: {hidden} | Preserved: {preserved}");
        }

        static bool ShouldPreserve(Transform t)
        {
            var cur = t;
            while (cur != null)
            {
                string n = cur.name.ToLowerInvariant();
                foreach (var kw in _preserveKeywords)
                    if (n.Contains(kw)) return true;
                cur = cur.parent;
            }
            return false;
        }

        // ── Anexar visual real da arma do Huntsman ─────────────────────────────

        static void AttachWeaponVisual(GameObject spawned, GameObject weaponVisual, ManualLogSource log)
        {
            // Root visual ancorado no item dropado
            var root = new GameObject("HuntsmanGunVisualRoot");
            root.transform.SetParent(spawned.transform, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale    = Vector3.one;

            // Remover physics e scripts do clone antes de parentear
            // (mantem apenas Transform, MeshRenderer, SkinnedMeshRenderer, MeshFilter)
            foreach (var col  in weaponVisual.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(col);
            foreach (var rb   in weaponVisual.GetComponentsInChildren<Rigidbody>(true))
                Object.DestroyImmediate(rb);
            foreach (var mb   in weaponVisual.GetComponentsInChildren<MonoBehaviour>(true))
                Object.DestroyImmediate(mb);

            // Parentear ao root com transform limpo
            weaponVisual.transform.SetParent(root.transform, false);
            weaponVisual.transform.localPosition = Vector3.zero;
            weaponVisual.transform.localRotation = Quaternion.identity;
            weaponVisual.transform.localScale    = Vector3.one;
            weaponVisual.SetActive(true);

            log.LogInfo("[HuntsmanLoot] Huntsman weapon visual attached: success");
        }

        // ── Luz pontual teal (complemento visual) ─────────────────────────────

        static void ApplyLight(GameObject spawned)
        {
            var go = new GameObject("HuntsmanGlow");
            go.transform.SetParent(spawned.transform, false);
            go.transform.localPosition = new Vector3(0f, 0.10f, 0f);
            var l = go.AddComponent<Light>();
            l.type      = LightType.Point;
            l.color     = new Color(0.00f, 0.88f, 0.65f);
            l.range     = 0.60f;
            l.intensity = 0.65f;
            l.shadows   = LightShadows.None;
        }

        // ── Identidade ─────────────────────────────────────────────────────────

        static void ApplyIdentity(GameObject spawned, ManualLogSource log)
        {
            var attr = spawned.GetComponentInChildren<ItemAttributes>();
            if (attr == null) return;

            var fName   = AccessTools.Field(typeof(ItemAttributes), "itemName");
            var fPrompt = AccessTools.Field(typeof(ItemAttributes), "promptName");
            if (fName   != null) try { fName.SetValue(attr,   "Huntsman Rifle"); } catch { }
            if (fPrompt != null) try { fPrompt.SetValue(attr, "Huntsman Rifle"); } catch { }

            try
            {
                var itemField = AccessTools.Field(typeof(ItemAttributes), "item");
                var origItem  = itemField?.GetValue(attr) as Item;
                if (origItem != null)
                {
                    var custom    = Object.Instantiate(origItem);
                    var nameField = AccessTools.Field(typeof(Item), "itemName")
                                 ?? AccessTools.Field(typeof(Item), "<itemName>k__BackingField");
                    if (nameField != null) nameField.SetValue(custom, "Huntsman Rifle");
                    itemField.SetValue(attr, custom);
                }
            }
            catch { }

            log.LogInfo("[HuntsmanLoot] Identity: nome aplicado.");
        }
    }
}
