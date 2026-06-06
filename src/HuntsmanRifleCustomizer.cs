using System;
using System.Collections.Generic;
using System.Reflection;
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

            if (weaponVisual == null)
            {
                // Sem visual valido: manter shotgun intacta e nao mascarar com visual fake.
                log.LogWarning("[HuntsmanLoot] Huntsman weapon visual not found — keeping native shotgun visible.");
                log.LogInfo("[HuntsmanLoot] Original shotgun renderers hidden: 0");
                log.LogInfo("[HuntsmanLoot] Huntsman weapon visual attached: fail");

                try { ApplyIdentity(spawned, log); }
                catch (Exception ex) { log.LogError($"[HuntsmanLoot] Customizer/Identity: {ex.Message}"); }

                return;
            }

            // Com visual valido: anexar visual real; so entao ocultar a shotgun base.
            bool attached = false;
            try { attached = AttachWeaponVisual(spawned, weaponVisual, log); }
            catch (Exception ex)
            {
                log.LogError($"[HuntsmanLoot] Customizer/Attach: {ex.Message}");
                log.LogInfo("[HuntsmanLoot] Huntsman weapon visual attached: fail");
            }

            if (!attached)
            {
                log.LogInfo("[HuntsmanLoot] Original shotgun renderers hidden: 0");
                DestroyDetachedVisual(weaponVisual);
            }
            else
            {
                try { HideOriginalRenderers(spawned, log); }
                catch (Exception ex) { log.LogError($"[HuntsmanLoot] Customizer/Hide: {ex.Message}"); }
            }

            try { ApplyIdentity(spawned, log); }
            catch (Exception ex) { log.LogError($"[HuntsmanLoot] Customizer/Identity: {ex.Message}"); }
        }

        // Verifica se o GameObject (ou qualquer filho) tem mesh renderizavel real.
        static bool HasValidRenderer(GameObject go)
        {
            foreach (var mr in go.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mf = mr.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null) return true;
            }

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

        static bool AttachWeaponVisual(GameObject spawned, GameObject weaponVisual, ManualLogSource log)
        {
            if (spawned == null || weaponVisual == null)
            {
                log.LogInfo("[HuntsmanLoot] Huntsman weapon visual attached: fail");
                return false;
            }

            // Root visual ancorado no item dropado
            var root = new GameObject("HuntsmanGunVisualRoot");
            root.transform.SetParent(spawned.transform, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale    = Vector3.one;

            // Parentear ao root com transform limpo antes de validar renderizacao.
            weaponVisual.transform.SetParent(root.transform, false);
            weaponVisual.transform.localPosition = Vector3.zero;
            weaponVisual.transform.localRotation = Quaternion.identity;
            weaponVisual.transform.localScale    = Vector3.one;
            weaponVisual.SetActive(true);

            var before = CountVisualRenderers(weaponVisual);
            log.LogInfo(
                $"[HuntsmanLoot] Visual clone renderers before cleanup: MR={before.MeshRenderers} " +
                $"SMR={before.SkinnedMeshRenderers} MF={before.MeshFilters}");

            StripNonVisualComponents(weaponVisual, log);
            PrepareVisualRenderers(weaponVisual);

            var after = CountVisualRenderers(weaponVisual);
            log.LogInfo(
                $"[HuntsmanLoot] Visual clone renderers after cleanup: MR={after.MeshRenderers} " +
                $"SMR={after.SkinnedMeshRenderers} MF={after.MeshFilters}");

            var validation = HasRenderableVisual(weaponVisual);
            if (!validation.Pass && after.SkinnedMeshRenderers > 0)
            {
                int baked = BakeSkinnedMeshRenderers(weaponVisual, log);
                if (baked > 0)
                {
                    PrepareVisualRenderers(weaponVisual);
                    validation = HasRenderableVisual(weaponVisual);
                }
            }

            log.LogInfo(
                $"[HuntsmanLoot] Visual clone validation: " +
                $"{(validation.Pass ? "PASS" : "FAIL")} reason={validation.Reason}");

            if (!validation.Pass)
            {
                Object.Destroy(root);
                log.LogWarning("[HuntsmanLoot] Keeping native shotgun visible because visual clone is invalid");
                log.LogInfo("[HuntsmanLoot] Huntsman weapon visual attached: fail");
                return false;
            }

            log.LogInfo("[HuntsmanLoot] Huntsman weapon visual attached: success");
            return true;
        }

        static VisualRendererCounts CountVisualRenderers(GameObject root)
        {
            return new VisualRendererCounts
            {
                MeshRenderers = root.GetComponentsInChildren<MeshRenderer>(true).Length,
                SkinnedMeshRenderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length,
                MeshFilters = root.GetComponentsInChildren<MeshFilter>(true).Length
            };
        }

        static void PrepareVisualRenderers(GameObject root)
        {
            foreach (var tr in root.GetComponentsInChildren<Transform>(true))
                tr.gameObject.SetActive(true);

            foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
                mr.enabled = true;

            foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                smr.enabled = true;
        }

        static VisualValidationResult HasRenderableVisual(GameObject root)
        {
            foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mf = mr.GetComponent<MeshFilter>();
                if (mr.enabled &&
                    mr.gameObject.activeInHierarchy &&
                    mf != null &&
                    mf.sharedMesh != null &&
                    mf.sharedMesh.vertexCount > 0 &&
                    HasAnyMaterial(mr.sharedMaterials))
                {
                    return VisualValidationResult.Success("mesh-renderer");
                }
            }

            foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.enabled &&
                    smr.gameObject.activeInHierarchy &&
                    smr.sharedMesh != null &&
                    smr.sharedMesh.vertexCount > 0 &&
                    HasAnyMaterial(smr.sharedMaterials))
                {
                    return VisualValidationResult.Success("skinned-mesh-renderer");
                }
            }

            return VisualValidationResult.Fail("no-enabled-renderer-with-mesh-and-material");
        }

        static bool HasAnyMaterial(Material[] materials)
        {
            if (materials == null || materials.Length == 0) return false;
            foreach (var material in materials)
                if (material != null) return true;
            return false;
        }

        static int BakeSkinnedMeshRenderers(GameObject root, ManualLogSource log)
        {
            int bakedCount = 0;
            var skinnedRenderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            foreach (var smr in skinnedRenderers)
            {
                if (smr == null || smr.sharedMesh == null || !HasAnyMaterial(smr.sharedMaterials))
                    continue;

                try
                {
                    var bakedMesh = new Mesh();
                    smr.BakeMesh(bakedMesh);

                    if (bakedMesh.vertexCount <= 0)
                    {
                        Object.Destroy(bakedMesh);
                        continue;
                    }

                    var bakedGo = new GameObject($"{smr.gameObject.name}_BakedVisual");
                    bakedGo.transform.SetParent(smr.transform.parent, false);
                    bakedGo.transform.localPosition = smr.transform.localPosition;
                    bakedGo.transform.localRotation = smr.transform.localRotation;
                    bakedGo.transform.localScale = smr.transform.localScale;

                    var mf = bakedGo.AddComponent<MeshFilter>();
                    mf.sharedMesh = bakedMesh;

                    var mr = bakedGo.AddComponent<MeshRenderer>();
                    mr.sharedMaterials = smr.sharedMaterials;
                    mr.enabled = true;

                    bakedGo.SetActive(true);
                    smr.enabled = false;
                    bakedCount++;
                }
                catch (Exception ex)
                {
                    log.LogWarning($"[HuntsmanLoot] SkinnedMeshRenderer bake failed: {ex.Message}");
                }
            }

            log.LogInfo($"[HuntsmanLoot] SkinnedMeshRenderer bake: baked={bakedCount}");
            return bakedCount;
        }

        static void StripNonVisualComponents(GameObject weaponVisual, ManualLogSource log)
        {
            int removed = 0;
            int disabled = 0;

            foreach (var component in weaponVisual.GetComponentsInChildren<Component>(true))
            {
                if (component == null || IsAllowedVisualComponent(component))
                    continue;

                try
                {
                    if (component is Behaviour behaviour)
                    {
                        behaviour.enabled = false;
                        disabled++;
                    }
                    else if (component is Collider collider)
                    {
                        collider.enabled = false;
                        disabled++;
                    }
                    else if (component is Rigidbody rigidbody)
                    {
                        rigidbody.detectCollisions = false;
                        rigidbody.isKinematic = true;
                        disabled++;
                    }

                    Object.DestroyImmediate(component);
                    removed++;
                }
                catch (Exception ex)
                {
                    log.LogWarning(
                        $"[HuntsmanLoot] Visual cleanup skipped component {component.GetType().Name}: {ex.Message}");
                }
            }

            log.LogInfo($"[HuntsmanLoot] Huntsman weapon visual cleanup: removed={removed} disabled={disabled}");
        }

        static bool IsAllowedVisualComponent(Component component)
        {
            return component is Transform ||
                   component is MeshRenderer ||
                   component is SkinnedMeshRenderer ||
                   component is MeshFilter;
        }

        static void DestroyDetachedVisual(GameObject weaponVisual)
        {
            if (weaponVisual != null)
                Object.Destroy(weaponVisual);
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
            if (attr == null)
            {
                log.LogInfo("[HuntsmanLoot] Inventory identity fields found: none");
                log.LogInfo("[HuntsmanLoot] Inventory display name updated: fail field=ItemAttributes");
                log.LogInfo("[HuntsmanLoot] Inventory icon update: fail reason=ItemAttributes not found");
                return;
            }

            var foundFields = new List<string>();
            var updatedFields = new List<string>();

            TryRecordField(typeof(ItemAttributes), "itemName", foundFields);
            TryRecordField(typeof(ItemAttributes), "promptName", foundFields);
            TryRecordField(typeof(ItemAttributes), "instanceName", foundFields);
            TryRecordField(typeof(ItemAttributes), "icon", foundFields);
            TryRecordField(typeof(ItemAttributes), "hasIcon", foundFields);
            TryRecordField(typeof(ItemAttributes), "item", foundFields);

            if (TrySetStringField(attr, typeof(ItemAttributes), "itemName", "Huntsman Rifle"))
                updatedFields.Add("ItemAttributes.itemName");
            if (TrySetStringField(attr, typeof(ItemAttributes), "promptName", "Huntsman Rifle"))
                updatedFields.Add("ItemAttributes.promptName");
            if (TrySetStringField(attr, typeof(ItemAttributes), "instanceName", "Huntsman Rifle"))
                updatedFields.Add("ItemAttributes.instanceName");

            attr.gameObject.name = "Huntsman Rifle";

            var equippable = GetItemEquippable(attr, spawned);
            if (equippable != null)
            {
                TryRecordField(typeof(ItemEquippable), "ItemIcon", foundFields);
                TryRecordField(typeof(ItemEquippable), "itemEmojiIcon", foundFields);
                TryRecordField(typeof(ItemEquippable), "itemEmoji", foundFields);
            }

            try
            {
                var itemField = AccessTools.Field(typeof(ItemAttributes), "item");
                var origItem  = itemField?.GetValue(attr) as Item;
                if (origItem != null)
                {
                    var custom    = Object.Instantiate(origItem);
                    var nameField = AccessTools.Field(typeof(Item), "itemName")
                                 ?? AccessTools.Field(typeof(Item), "<itemName>k__BackingField");
                    if (nameField != null)
                    {
                        foundFields.Add("Item.itemName");
                        nameField.SetValue(custom, "Huntsman Rifle");
                        updatedFields.Add("Item.itemName");
                    }

                    if (AccessTools.Field(typeof(Item), "itemNameLocalized") != null)
                        foundFields.Add("Item.itemNameLocalized");

                    itemField.SetValue(attr, custom);
                }
            }
            catch (Exception ex)
            {
                log.LogWarning($"[HuntsmanLoot] Inventory item metadata clone failed: {ex.Message}");
            }

            log.LogInfo(
                $"[HuntsmanLoot] Inventory identity fields found: " +
                $"{(foundFields.Count > 0 ? string.Join(",", foundFields.ToArray()) : "none")}");

            if (updatedFields.Count > 0)
                log.LogInfo(
                    $"[HuntsmanLoot] Inventory display name updated: success field=" +
                    $"{string.Join(",", updatedFields.ToArray())}");
            else
                log.LogInfo("[HuntsmanLoot] Inventory display name updated: fail field=none");

            log.LogInfo(
                "[HuntsmanLoot] Inventory icon update: unsupported " +
                "reason=no safe custom sprite available for native shotgun base");
            log.LogWarning(
                "[HuntsmanLoot] Inventory icon/name still uses native shotgun metadata — known limitation of native shotgun base.");
        }

        static void TryRecordField(Type type, string fieldName, List<string> fields)
        {
            if (AccessTools.Field(type, fieldName) != null)
                fields.Add($"{type.Name}.{fieldName}");
        }

        static bool TrySetStringField(object target, Type type, string fieldName, string value)
        {
            FieldInfo field = AccessTools.Field(type, fieldName);
            if (field == null || field.FieldType != typeof(string)) return false;

            try
            {
                field.SetValue(target, value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        static ItemEquippable GetItemEquippable(ItemAttributes attr, GameObject spawned)
        {
            try
            {
                var field = AccessTools.Field(typeof(ItemAttributes), "itemEquippable");
                if (field?.GetValue(attr) is ItemEquippable reflected)
                    return reflected;
            }
            catch { }

            return spawned.GetComponentInChildren<ItemEquippable>();
        }

        private struct VisualRendererCounts
        {
            internal int MeshRenderers;
            internal int SkinnedMeshRenderers;
            internal int MeshFilters;
        }

        private sealed class VisualValidationResult
        {
            internal bool Pass;
            internal string Reason;

            internal static VisualValidationResult Success(string reason)
            {
                return new VisualValidationResult { Pass = true, Reason = reason };
            }

            internal static VisualValidationResult Fail(string reason)
            {
                return new VisualValidationResult { Pass = false, Reason = reason };
            }
        }
    }
}
