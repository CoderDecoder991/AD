using UnityEngine;

namespace AirDefense.Core
{
    /// <summary>
    /// Materials for PLACEHOLDER greybox visuals (C-14). Referencing them from a
    /// scene asset guarantees their shaders are included in builds.
    /// Created by AirDefense ▸ Setup ▸ Build M1 Greybox Scene.
    /// </summary>
    [CreateAssetMenu(menuName = "AirDefense/Prototype Visuals", fileName = "PrototypeVisuals")]
    public sealed class PrototypeVisuals : ScriptableObject
    {
        public Material threatBody;
        public Material threatAccent;
        public Material tracer;
        public Material explosion;
        public Material impactFlash;
        [Tooltip("Unlit emissive red, for blinking threat navigation lights.")]
        public Material navLight;

        // Runtime code must not use GameObject.CreatePrimitive: it adds colliders whose
        // classes are stripped from builds when no scene uses physics.
        [Header("Primitive meshes")]
        public Mesh cube;
        public Mesh sphere;
        public Mesh cylinder;
        public Mesh capsule;

        /// <summary>Creates a mesh-only child object (no collider).</summary>
        public GameObject Part(string name, Mesh mesh, Material mat, Transform parent,
                               Vector3 localPos, Vector3 localScale, Quaternion? localRot = null, bool shadows = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot ?? Quaternion.identity;
            go.transform.localScale = localScale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go;
        }
    }
}
