using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HuntsmanLoot
{
    // Aplicado sobre a shotgun nativa JA spawnada.
    // Oculta renderers originais e anexa Mesh/Material nativos da arma do Huntsman.
    // Nao toca em PhysGrabObject, RoomVolumeCheck, Rigidbody, ItemGun, PhotonView.
    internal static class HuntsmanRifleCustomizer
    {
        private static readonly string[] _preserveKeywords =
            { "battery", "ammo", "bar", "ui", "icon", "screen", "text" };

        private static readonly Vector3 NativeGunLocalPosition = new Vector3(0f, 0f, 0f);
        private static readonly Vector3 NativeGunLocalEuler    = new Vector3(-8f, 180f, 90f);
        private static readonly Vector3 NativeGunLocalScale    = new Vector3(0.75f, 0.75f, 0.75f);
        private const string NativeCollisionEnvelopeName = "HuntsmanNativeGunCollisionEnvelope";
        private const float CollisionLengthScale = 1.08f;
        private const float CollisionWidthScale = 1.05f;
        private const float CollisionThicknessScale = 1.10f;
        private const float CollisionMinThickness = 0.04f;
        private const string DisplayName = "Huntsman Rifle";
        private const float IconFramingPadding = 1.16f;
        private const float IconCameraDistanceMultiplier = 2.5f;
        private const float IconCameraMinDistance = 1.25f;
        private static bool _generatedIconCacheCleared;

        internal static void Apply(GameObject spawned, GameObject sourceEnemy, ManualLogSource log)
        {
            if (spawned == null) return;

            NativeHunterGunVisualResolver.VisualAttachResult attachResult = null;
            try { attachResult = AttachNativeWeaponVisual(spawned, sourceEnemy, log); }
            catch (Exception ex)
            {
                log.LogError($"[HuntsmanLoot] Customizer/NativeMesh: {ex.Message}");
                log.LogInfo("[HuntsmanLoot] Native Hunter Gun visual created: FAIL reason=exception");
                attachResult = NativeHunterGunVisualResolver.VisualAttachResult.Fail("exception");
            }

            if (attachResult == null || !attachResult.Pass)
            {
                if (attachResult == null || attachResult.Reason == "mesh-not-found")
                    log.LogWarning("[HuntsmanLoot] Native Hunter Gun mesh not found — keeping native shotgun visible");
                else
                    log.LogWarning($"[HuntsmanLoot] Keeping native shotgun visible because native visual failed: {attachResult.Reason}");

                HuntsmanLootPlugin.DebugLog("[HuntsmanLoot] Original shotgun renderers hidden: 0");
            }
            else
            {
                try { HideOriginalRenderers(spawned, attachResult.Root, log); }
                catch (Exception ex) { log.LogError($"[HuntsmanLoot] Customizer/Hide: {ex.Message}"); }
            }

            try { ApplyIdentity(spawned, log); }
            catch (Exception ex) { log.LogError($"[HuntsmanLoot] Customizer/Identity: {ex.Message}"); }
        }

        static NativeHunterGunVisualResolver.VisualAttachResult AttachNativeWeaponVisual(
            GameObject spawned,
            GameObject sourceEnemy,
            ManualLogSource log)
        {
            var resolved = NativeHunterGunVisualResolver.Resolve(sourceEnemy, log);
            if (resolved.Mesh == null)
            {
                log.LogInfo("[HuntsmanLoot] Native Hunter Gun visual created: FAIL reason=mesh-not-found");
                return NativeHunterGunVisualResolver.VisualAttachResult.Fail("mesh-not-found");
            }

            var materials = NativeHunterGunVisualResolver.CreateNativeCloneMaterials(resolved.Mesh, log);
            if (!HasAnyMaterial(materials))
            {
                log.LogInfo("[HuntsmanLoot] Native Hunter Gun visual created: FAIL reason=material-not-found");
                return NativeHunterGunVisualResolver.VisualAttachResult.Fail("material-not-found");
            }

            var root = new GameObject("HuntsmanNativeGunVisualRoot");
            root.transform.SetParent(spawned.transform, false);
            root.transform.localPosition = NativeGunLocalPosition;
            root.transform.localRotation = Quaternion.Euler(NativeGunLocalEuler.x, NativeGunLocalEuler.y, NativeGunLocalEuler.z);
            root.transform.localScale = NativeGunLocalScale;

            var meshFilter = root.AddComponent<MeshFilter>();
            var meshRenderer = root.AddComponent<MeshRenderer>();

            meshFilter.sharedMesh = resolved.Mesh;
            meshRenderer.sharedMaterials = materials;
            meshRenderer.enabled = true;
            root.SetActive(true);

            log.LogInfo(
                "[HuntsmanLoot] Native visual transform: " +
                $"pos={FormatVector(root.transform.localPosition)} " +
                $"euler={FormatVector(NativeGunLocalEuler)} " +
                $"scale={FormatVector(root.transform.localScale)}");

            var validation = HasRenderableVisual(root);
            log.LogInfo(
                $"[HuntsmanLoot] Native Hunter Gun visual created: " +
                $"{(validation.Pass ? "PASS" : "FAIL")} reason={validation.Reason}");

            if (!validation.Pass)
            {
                Object.Destroy(root);
                return NativeHunterGunVisualResolver.VisualAttachResult.Fail(validation.Reason);
            }

            var collisionResult = CreateNativeCollisionEnvelope(root, spawned, resolved.Mesh, log);
            if (collisionResult.Created)
                log.LogInfo(
                    $"[HuntsmanLoot] Native collision envelope created: " +
                    $"attachedRigidbody={collisionResult.AttachedRigidbody.ToString().ToLowerInvariant()} " +
                    $"size={FormatVector(collisionResult.Size)}");
            else
                log.LogWarning($"[HuntsmanLoot] Native collision envelope skipped: reason={collisionResult.Reason}");

            ConfigureNativeIconCamera(spawned, root, log);

            return NativeHunterGunVisualResolver.VisualAttachResult.Success(root);
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

        static void HideOriginalRenderers(GameObject spawned, GameObject protectedVisualRoot, ManualLogSource log)
        {
            int hidden = 0, preserved = 0;

            foreach (var mr in spawned.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (IsProtectedVisual(mr.transform, protectedVisualRoot) || ShouldPreserve(mr.transform)) { preserved++; }
                else { mr.enabled = false; hidden++; }
            }

            foreach (var smr in spawned.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (IsProtectedVisual(smr.transform, protectedVisualRoot) || ShouldPreserve(smr.transform)) { preserved++; }
                else { smr.enabled = false; hidden++; }
            }

            HuntsmanLootPlugin.DebugLog($"[HuntsmanLoot] Original shotgun renderers hidden: {hidden}");
            HuntsmanLootPlugin.DebugLog($"[HuntsmanLoot] Original shotgun renderers preserved: {preserved}");
        }

        static bool IsProtectedVisual(Transform t, GameObject protectedVisualRoot)
        {
            return protectedVisualRoot != null && t != null && t.IsChildOf(protectedVisualRoot.transform);
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

        static void ConfigureNativeIconCamera(GameObject spawned, GameObject visualRoot, ManualLogSource log)
        {
            try
            {
                var iconMaker = spawned.GetComponentInChildren<SemiIconMaker>(true);
                if (iconMaker == null)
                {
                    HuntsmanLootPlugin.DebugLog("[HuntsmanLoot] Inventory icon framing skipped: reason=SemiIconMaker not found");
                    return;
                }

                var camera = iconMaker.iconCamera ?? iconMaker.GetComponentInChildren<Camera>(true);
                if (camera == null)
                {
                    HuntsmanLootPlugin.DebugLog("[HuntsmanLoot] Inventory icon framing skipped: reason=icon camera not found");
                    return;
                }

                if (!TryGetLocalRendererBounds(spawned.transform, visualRoot, out Bounds bounds, out Vector3[] corners))
                {
                    HuntsmanLootPlugin.DebugLog("[HuntsmanLoot] Inventory icon framing skipped: reason=visual bounds unavailable");
                    return;
                }

                int lengthAxis = LongestAxis(bounds.size);
                int viewAxis = SmallestAxis(bounds.size, lengthAxis);
                Vector3 longLocal = AxisVector(lengthAxis);
                Vector3 forwardLocal = AxisVector(viewAxis);
                Vector3 upLocal = Vector3.Cross(forwardLocal, longLocal).normalized;
                if (upLocal.sqrMagnitude < 0.0001f)
                    upLocal = AxisVector(NextAxis(lengthAxis, viewAxis));

                Vector3 rightLocal = Vector3.Cross(upLocal, forwardLocal).normalized;
                if (Vector3.Dot(rightLocal, longLocal) < 0f)
                {
                    rightLocal = -rightLocal;
                    upLocal = -upLocal;
                }

                float width = ProjectSpan(corners, bounds.center, rightLocal);
                float height = ProjectSpan(corners, bounds.center, upLocal);
                float aspect = GetIconCameraAspect(iconMaker, camera);
                float orthographicSize = Mathf.Max(
                    Mathf.Max(height * 0.5f, width / (2f * aspect)) * IconFramingPadding,
                    0.05f);

                Vector3 centerWorld = spawned.transform.TransformPoint(bounds.center);
                Vector3 forwardWorld = spawned.transform.TransformDirection(forwardLocal).normalized;
                Vector3 upWorld = -spawned.transform.TransformDirection(upLocal).normalized;
                float distance = Mathf.Max(IconCameraMinDistance, bounds.size.magnitude * IconCameraDistanceMultiplier);

                camera.orthographic = true;
                camera.orthographicSize = orthographicSize;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = Mathf.Max(camera.farClipPlane, distance + bounds.size.magnitude * 3f);
                camera.transform.position = centerWorld - forwardWorld * distance;
                camera.transform.rotation = Quaternion.LookRotation(forwardWorld, upWorld);

                iconMaker.iconCamera = camera;
                iconMaker.iconCameraPlacementDone = true;

                HuntsmanLootPlugin.DebugLog(
                    "[HuntsmanLoot] Inventory icon framing adjusted: " +
                    $"lengthAxis={lengthAxis} viewAxis={viewAxis} ortho={orthographicSize.ToString("0.###", CultureInfo.InvariantCulture)}");
            }
            catch (Exception ex)
            {
                HuntsmanLootPlugin.DebugWarning($"[HuntsmanLoot] Inventory icon framing failed: {ex.Message}");
            }
        }

        static bool TryGetLocalRendererBounds(
            Transform itemRoot,
            GameObject visualRoot,
            out Bounds bounds,
            out Vector3[] corners)
        {
            bounds = default;
            corners = null;
            if (itemRoot == null || visualRoot == null) return false;

            var points = new List<Vector3>();
            foreach (var renderer in visualRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || !renderer.enabled) continue;
                AddLocalBoundsCorners(itemRoot, renderer.bounds, points);
            }

            if (points.Count == 0) return false;

            bounds = new Bounds(points[0], Vector3.zero);
            for (int i = 1; i < points.Count; i++)
                bounds.Encapsulate(points[i]);

            corners = BoundsCorners(bounds);
            return bounds.size.x > 0f && bounds.size.y > 0f && bounds.size.z > 0f;
        }

        static void AddLocalBoundsCorners(Transform itemRoot, Bounds worldBounds, List<Vector3> points)
        {
            Vector3 min = worldBounds.min;
            Vector3 max = worldBounds.max;
            points.Add(itemRoot.InverseTransformPoint(new Vector3(min.x, min.y, min.z)));
            points.Add(itemRoot.InverseTransformPoint(new Vector3(min.x, min.y, max.z)));
            points.Add(itemRoot.InverseTransformPoint(new Vector3(min.x, max.y, min.z)));
            points.Add(itemRoot.InverseTransformPoint(new Vector3(min.x, max.y, max.z)));
            points.Add(itemRoot.InverseTransformPoint(new Vector3(max.x, min.y, min.z)));
            points.Add(itemRoot.InverseTransformPoint(new Vector3(max.x, min.y, max.z)));
            points.Add(itemRoot.InverseTransformPoint(new Vector3(max.x, max.y, min.z)));
            points.Add(itemRoot.InverseTransformPoint(new Vector3(max.x, max.y, max.z)));
        }

        static Vector3[] BoundsCorners(Bounds bounds)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            return new[]
            {
                new Vector3(min.x, min.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(max.x, max.y, max.z)
            };
        }

        static float ProjectSpan(Vector3[] points, Vector3 center, Vector3 axis)
        {
            float min = float.MaxValue;
            float max = float.MinValue;
            for (int i = 0; i < points.Length; i++)
            {
                float value = Vector3.Dot(points[i] - center, axis);
                if (value < min) min = value;
                if (value > max) max = value;
            }

            return Mathf.Max(0.01f, max - min);
        }

        static float GetIconCameraAspect(SemiIconMaker iconMaker, Camera camera)
        {
            if (iconMaker != null && iconMaker.renderTexture != null && iconMaker.renderTexture.height > 0)
                return Mathf.Max(0.01f, (float)iconMaker.renderTexture.width / iconMaker.renderTexture.height);

            return camera != null && camera.aspect > 0f ? camera.aspect : 1f;
        }

        static Vector3 AxisVector(int axis)
        {
            switch (axis)
            {
                case 0: return Vector3.right;
                case 1: return Vector3.up;
                default: return Vector3.forward;
            }
        }

        static int NextAxis(int first, int second)
        {
            for (int axis = 0; axis < 3; axis++)
                if (axis != first && axis != second)
                    return axis;

            return 1;
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

        static CollisionEnvelopeResult CreateNativeCollisionEnvelope(
            GameObject visualRoot,
            GameObject spawned,
            Mesh mesh,
            ManualLogSource log)
        {
            if (visualRoot == null) return CollisionEnvelopeResult.Skip("visual-root-null");
            if (spawned == null) return CollisionEnvelopeResult.Skip("spawned-null");
            if (mesh == null) return CollisionEnvelopeResult.Skip("mesh-null");

            var bounds = mesh.bounds;
            if (bounds.size.x <= 0f || bounds.size.y <= 0f || bounds.size.z <= 0f)
                return CollisionEnvelopeResult.Skip("invalid-mesh-bounds");

            try
            {
                var sourceCollider = FindNativeSourceCollider(spawned, visualRoot);
                var envelope = new GameObject(NativeCollisionEnvelopeName);
                envelope.transform.SetParent(visualRoot.transform, false);
                envelope.transform.localPosition = Vector3.zero;
                envelope.transform.localRotation = Quaternion.identity;
                envelope.transform.localScale = Vector3.one;

                CopyCollisionIdentity(envelope, sourceCollider, spawned);

                var box = envelope.AddComponent<BoxCollider>();
                box.isTrigger = false;
                if (sourceCollider != null && sourceCollider.sharedMaterial != null)
                    box.sharedMaterial = sourceCollider.sharedMaterial;

                int lengthAxis = LongestAxis(bounds.size);
                int thicknessAxis = SmallestAxis(bounds.size, lengthAxis);
                var size = ExpandEnvelopeSize(bounds.size, lengthAxis, thicknessAxis);

                box.center = bounds.center;
                box.size = size;

                bool attached = box.attachedRigidbody != null;
                if (!attached)
                {
                    Object.Destroy(envelope);
                    return CollisionEnvelopeResult.Skip("attached-rigidbody-null");
                }

                HuntsmanLootPlugin.DebugLog(
                    $"[HuntsmanLoot] Native collision envelope center={FormatVector(box.center)} " +
                    $"size={FormatVector(box.size)} lengthAxis={lengthAxis} thicknessAxis={thicknessAxis}");

                return CollisionEnvelopeResult.Success(attached, box.center, box.size);
            }
            catch (Exception ex)
            {
                return CollisionEnvelopeResult.Skip(ex.Message);
            }
        }

        static int LongestAxis(Vector3 size)
        {
            if (size.x >= size.y && size.x >= size.z) return 0;
            if (size.y >= size.x && size.y >= size.z) return 1;
            return 2;
        }

        static int SmallestAxis(Vector3 size, int excludedAxis)
        {
            int smallest = -1;
            for (int i = 0; i < 3; i++)
            {
                if (i == excludedAxis) continue;
                if (smallest < 0 || size[i] < size[smallest])
                    smallest = i;
            }
            return smallest < 0 ? 0 : smallest;
        }

        static Vector3 ExpandEnvelopeSize(Vector3 sourceSize, int lengthAxis, int thicknessAxis)
        {
            var size = sourceSize;
            for (int i = 0; i < 3; i++)
            {
                float scale = CollisionWidthScale;
                if (i == lengthAxis)
                    scale = CollisionLengthScale;
                else if (i == thicknessAxis)
                    scale = CollisionThicknessScale;

                size[i] = Mathf.Max(sourceSize[i] * scale, CollisionMinThickness);
            }
            return size;
        }

        static Collider FindNativeSourceCollider(GameObject spawned, GameObject visualRoot)
        {
            if (spawned == null) return null;

            foreach (var collider in spawned.GetComponentsInChildren<Collider>(true))
            {
                if (collider == null) continue;
                if (visualRoot != null && collider.transform.IsChildOf(visualRoot.transform)) continue;
                return collider;
            }

            return null;
        }

        static void CopyCollisionIdentity(GameObject envelope, Collider sourceCollider, GameObject spawned)
        {
            var source = sourceCollider != null ? sourceCollider.gameObject : spawned;
            envelope.layer = source.layer;
            envelope.tag = source.tag;
        }

        static bool HasAnyMaterial(Material[] materials)
        {
            if (materials == null || materials.Length == 0) return false;
            foreach (var material in materials)
                if (material != null) return true;
            return false;
        }

        struct CollisionEnvelopeResult
        {
            internal bool Created;
            internal bool AttachedRigidbody;
            internal Vector3 Center;
            internal Vector3 Size;
            internal string Reason;

            internal static CollisionEnvelopeResult Success(bool attachedRigidbody, Vector3 center, Vector3 size)
            {
                return new CollisionEnvelopeResult
                {
                    Created = true,
                    AttachedRigidbody = attachedRigidbody,
                    Center = center,
                    Size = size,
                    Reason = "created"
                };
            }

            internal static CollisionEnvelopeResult Skip(string reason)
            {
                return new CollisionEnvelopeResult
                {
                    Created = false,
                    AttachedRigidbody = false,
                    Center = Vector3.zero,
                    Size = Vector3.zero,
                    Reason = reason
                };
            }
        }

        static string FormatVector(Vector3 value)
        {
            return "(" +
                   value.x.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                   value.y.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                   value.z.ToString("0.###", CultureInfo.InvariantCulture) + ")";
        }

        static string FormatQuaternion(Quaternion value)
        {
            return $"({value.x:0.###},{value.y:0.###},{value.z:0.###},{value.w:0.###})";
        }

        private static class NativeHunterGunVisualResolver
        {
            private const string NativeMeshName = "Hunter Gun";
            private const string NativeMaterialName = "Enemy Hunter";

            internal static ResolveResult Resolve(GameObject sourceEnemy, ManualLogSource log)
            {
                var result = new ResolveResult();
                TryResolveFromGameObject(sourceEnemy, result);

                if (result.Mesh == null)
                    result.Mesh = FindNativeMesh();

                if (result.Mesh == null)
                    return result;

                log.LogInfo(
                    $"[HuntsmanLoot] Native Hunter Gun mesh found: name={result.Mesh.name} vertices={result.Mesh.vertexCount}");

                return result;
            }

            internal static Material[] CreateNativeCloneMaterials(Mesh mesh, ManualLogSource log)
            {
                var nativeMaterials = FindNativeMaterials();
                if (HasAnyMaterial(nativeMaterials))
                {
                    var cloned = CloneMaterialsForMesh(mesh, nativeMaterials);
                    NeutralizeProblematicTint(cloned);
                    log.LogInfo("[HuntsmanLoot] Visual material mode: native-clone");
                    return cloned;
                }

                log.LogWarning("[HuntsmanLoot] Native material unavailable — using fallback-neutral.");
                var fallback = CreateFallbackNeutralMaterials(mesh, log);
                log.LogInfo("[HuntsmanLoot] Visual material mode: fallback-neutral");
                return fallback;
            }

            static Material[] CloneMaterialsForMesh(Mesh mesh, Material[] nativeMaterials)
            {
                int count = Mathf.Max(1, mesh != null ? mesh.subMeshCount : 1);
                var materials = new Material[count];
                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = nativeMaterials[Mathf.Min(i, nativeMaterials.Length - 1)];
                    materials[i] = new Material(source)
                    {
                        name = $"HuntsmanLoot_NativeHunterGunClone_{i}"
                    };
                }
                return materials;
            }

            static void NeutralizeProblematicTint(Material[] materials)
            {
                if (materials == null) return;

                foreach (var material in materials)
                {
                    if (material == null) continue;

                    if (material.HasProperty("_EmissionColor"))
                        material.SetColor("_EmissionColor", Color.black);

                    if (material.HasProperty("_Color"))
                        NeutralizeRedColor(material, "_Color");
                    if (material.HasProperty("_BaseColor"))
                        NeutralizeRedColor(material, "_BaseColor");

                    material.DisableKeyword("_EMISSION");
                    material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                }
            }

            static void NeutralizeRedColor(Material material, string property)
            {
                Color color = material.GetColor(property);
                if (!IsStrongRedTint(color)) return;

                material.SetColor(property, new Color(0.62f, 0.58f, 0.49f, color.a));
            }

            static bool IsStrongRedTint(Color color)
            {
                return color.r > 0.65f &&
                       color.r > color.g * 1.6f &&
                       color.r > color.b * 1.6f;
            }

            static Material[] CreateFallbackNeutralMaterials(Mesh mesh, ManualLogSource log)
            {
                Shader shader = Shader.Find("Standard")
                             ?? Shader.Find("Universal Render Pipeline/Lit")
                             ?? Shader.Find("HDRP/Lit")
                             ?? Shader.Find("Diffuse");

                if (shader == null)
                {
                    log.LogError("[HuntsmanLoot] Runtime neutral material failed: no compatible shader found");
                    return null;
                }

                int count = Mathf.Max(1, mesh != null ? mesh.subMeshCount : 1);
                var materials = new Material[count];
                for (int i = 0; i < materials.Length; i++)
                {
                    var material = new Material(shader)
                    {
                        name = $"HuntsmanLoot_FallbackHunterGunNeutral_{i}"
                    };

                    Color baseColor = i == 1
                        ? new Color(0.42f, 0.34f, 0.23f, 1f)
                        : new Color(0.40f, 0.39f, 0.34f, 1f);

                    if (material.HasProperty("_Color"))
                        material.SetColor("_Color", baseColor);
                    if (material.HasProperty("_BaseColor"))
                        material.SetColor("_BaseColor", baseColor);
                    if (material.HasProperty("_EmissionColor"))
                        material.SetColor("_EmissionColor", Color.black);
                    if (material.HasProperty("_Metallic"))
                        material.SetFloat("_Metallic", 0.15f);
                    if (material.HasProperty("_Glossiness"))
                        material.SetFloat("_Glossiness", 0.28f);

                    material.DisableKeyword("_EMISSION");
                    material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                    materials[i] = material;
                }

                return materials;
            }

            internal static Material CreateFallbackMaterial(ManualLogSource log)
            {
                log.LogWarning("[HuntsmanLoot] Native material not found — using runtime fallback material.");

                Shader shader = Shader.Find("Standard")
                             ?? Shader.Find("Diffuse")
                             ?? Shader.Find("Universal Render Pipeline/Lit")
                             ?? Shader.Find("HDRP/Lit");

                if (shader == null)
                {
                    log.LogError("[HuntsmanLoot] Runtime fallback material failed: no compatible shader found");
                    return null;
                }

                return new Material(shader)
                {
                    name = "HuntsmanLoot_RuntimeHunterGunFallback",
                    color = new Color(0.015f, 0.013f, 0.011f, 1f)
                };
            }

            static Mesh FindNativeMesh()
            {
                foreach (var mesh in Resources.FindObjectsOfTypeAll<Mesh>())
                    if (IsHunterGunMesh(mesh))
                        return mesh;

                foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
                {
                    if (go == null || !NameSuggestsHunterGunContainer(go.name)) continue;

                    var mesh = FindMeshInGameObject(go);
                    if (mesh != null) return mesh;
                }

                return null;
            }

            static Material[] FindNativeMaterials()
            {
                var exactBase = new List<Material>();
                var exactInstance = new List<Material>();
                var related = new List<Material>();

                foreach (var material in Resources.FindObjectsOfTypeAll<Material>())
                {
                    if (material == null) continue;

                    string name = CleanName(material.name);
                    if (name == NativeMaterialName)
                    {
                        if (IsInstanceName(material.name))
                            exactInstance.Add(material);
                        else
                            exactBase.Add(material);
                        continue;
                    }

                    if (name.IndexOf(NativeMaterialName, StringComparison.OrdinalIgnoreCase) >= 0 &&
                        name.IndexOf("Shoot", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        related.Add(material);
                    }
                }

                if (exactBase.Count > 0) return exactBase.ToArray();
                if (exactInstance.Count > 0) return exactInstance.ToArray();
                if (related.Count > 0) return related.ToArray();
                return null;
            }

            static void TryResolveFromGameObject(GameObject source, ResolveResult result)
            {
                if (source == null) return;

                foreach (var meshFilter in source.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (meshFilter == null || !IsHunterGunMesh(meshFilter.sharedMesh)) continue;

                    result.Mesh = meshFilter.sharedMesh;
                    var renderer = meshFilter.GetComponent<MeshRenderer>();
                    if (renderer != null && HasAnyMaterial(renderer.sharedMaterials))
                        result.Materials = renderer.sharedMaterials;
                    return;
                }
            }

            static Mesh FindMeshInGameObject(GameObject go)
            {
                foreach (var meshFilter in go.GetComponentsInChildren<MeshFilter>(true))
                    if (meshFilter != null && IsHunterGunMesh(meshFilter.sharedMesh))
                        return meshFilter.sharedMesh;

                return null;
            }

            static bool IsHunterGunMesh(Mesh mesh)
            {
                return mesh != null && CleanName(mesh.name) == NativeMeshName && mesh.vertexCount > 0;
            }

            static bool NameSuggestsHunterGunContainer(string name)
            {
                if (string.IsNullOrEmpty(name)) return false;

                return name.IndexOf("Enemy - Hunter", StringComparison.OrdinalIgnoreCase) >= 0 ||
                       name.IndexOf("ANIM GUN", StringComparison.OrdinalIgnoreCase) >= 0 ||
                       name.IndexOf("mesh gun", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            static string CleanName(string name)
            {
                if (string.IsNullOrEmpty(name)) return string.Empty;
                const string instanceSuffix = " (Instance)";
                return name.EndsWith(instanceSuffix, StringComparison.Ordinal)
                    ? name.Substring(0, name.Length - instanceSuffix.Length)
                    : name;
            }

            static bool IsInstanceName(string name)
            {
                return !string.IsNullOrEmpty(name) &&
                       name.EndsWith(" (Instance)", StringComparison.Ordinal);
            }

            static string FormatMaterialNames(Material[] materials)
            {
                var names = new List<string>();
                foreach (var material in materials)
                {
                    if (material == null) continue;
                    names.Add(material.name);
                }
                return names.Count > 0 ? string.Join(",", names.ToArray()) : "none";
            }

            internal sealed class ResolveResult
            {
                internal Mesh Mesh;
                internal Material[] Materials;
            }

            internal sealed class VisualAttachResult
            {
                internal bool Pass;
                internal string Reason;
                internal GameObject Root;

                internal static VisualAttachResult Success(GameObject root)
                {
                    return new VisualAttachResult { Pass = true, Reason = "mesh-renderer", Root = root };
                }

                internal static VisualAttachResult Fail(string reason)
                {
                    return new VisualAttachResult { Pass = false, Reason = reason };
                }
            }
        }

        // ── Identidade ─────────────────────────────────────────────────────────

        static void ApplyIdentity(GameObject spawned, ManualLogSource log)
        {
            var attr = spawned.GetComponentInChildren<ItemAttributes>();
            if (attr == null)
            {
                HuntsmanLootPlugin.DebugLog("[HuntsmanLoot] Inventory identity fields found: none");
                HuntsmanLootPlugin.DebugLog("[HuntsmanLoot] Inventory display name updated: fail field=ItemAttributes");
                HuntsmanLootPlugin.DebugLog("[HuntsmanLoot] Inventory icon update: fail reason=ItemAttributes not found");
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

            if (TrySetStringField(attr, typeof(ItemAttributes), "itemName", DisplayName))
                updatedFields.Add("ItemAttributes.itemName");
            if (TrySetStringField(attr, typeof(ItemAttributes), "promptName", DisplayName))
                updatedFields.Add("ItemAttributes.promptName");
            if (TrySetStringField(attr, typeof(ItemAttributes), "instanceName", DisplayName))
                updatedFields.Add("ItemAttributes.instanceName");

            attr.gameObject.name = DisplayName;

            var equippable = GetItemEquippable(attr, spawned);
            if (equippable != null)
            {
                TryRecordField(typeof(ItemEquippable), "ItemIcon", foundFields);
                TryRecordField(typeof(ItemEquippable), "itemEmojiIcon", foundFields);
                TryRecordField(typeof(ItemEquippable), "itemEmoji", foundFields);
            }

            bool iconUpdated = ApplyInventoryIconOverride(attr, equippable, log);

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
                        nameField.SetValue(custom, DisplayName);
                        updatedFields.Add("Item.itemName");
                    }

                    var localizedField = AccessTools.Field(typeof(Item), "itemNameLocalized");
                    if (localizedField != null)
                    {
                        foundFields.Add("Item.itemNameLocalized");
                        localizedField.SetValue(custom, null);
                        updatedFields.Add("Item.itemNameLocalized");
                    }

                    itemField.SetValue(attr, custom);
                }
            }
            catch (Exception ex)
            {
                HuntsmanLootPlugin.DebugWarning($"[HuntsmanLoot] Inventory item metadata clone failed: {ex.Message}");
            }

            HuntsmanLootPlugin.DebugLog(
                $"[HuntsmanLoot] Inventory identity fields found: " +
                $"{(foundFields.Count > 0 ? string.Join(",", foundFields.ToArray()) : "none")}");

            if (updatedFields.Count > 0)
                HuntsmanLootPlugin.DebugLog(
                    $"[HuntsmanLoot] Inventory display name updated: success field=" +
                    $"{string.Join(",", updatedFields.ToArray())}");
            else
                HuntsmanLootPlugin.DebugLog("[HuntsmanLoot] Inventory display name updated: fail field=none");

            HuntsmanLootPlugin.DebugLog(
                "[HuntsmanLoot] Inventory icon update: " +
                (iconUpdated ? "success" : "fail"));
        }

        static bool ApplyInventoryIconOverride(ItemAttributes attr, ItemEquippable equippable, ManualLogSource log)
        {
            try
            {
                attr.icon = null;
                var hasIconField = AccessTools.Field(typeof(ItemAttributes), "hasIcon");
                hasIconField?.SetValue(attr, false);

                if (equippable != null)
                    equippable.ItemIcon = null;

                ClearGeneratedIconCacheOnce();
                HuntsmanLootPlugin.DebugLog("[HuntsmanLoot] Inventory icon override disabled; native item icon generation enabled.");
                return true;
            }
            catch (Exception ex)
            {
                HuntsmanLootPlugin.DebugWarning($"[HuntsmanLoot] Inventory icon override failed: {ex.Message}");
                return false;
            }

        }

        static void ClearGeneratedIconCacheOnce()
        {
            if (_generatedIconCacheCleared) return;
            _generatedIconCacheCleared = true;

            try
            {
                string path = Path.Combine(
                    Application.persistentDataPath,
                    "Cache",
                    "Icons",
                    "Items",
                    DisplayName.ToLowerInvariant() + ".png");

                if (File.Exists(path))
                {
                    File.Delete(path);
                    HuntsmanLootPlugin.DebugLog($"[HuntsmanLoot] Inventory icon cache cleared: {path}");
                }
            }
            catch (Exception ex)
            {
                HuntsmanLootPlugin.DebugWarning($"[HuntsmanLoot] Inventory icon cache clear failed: {ex.Message}");
            }
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
