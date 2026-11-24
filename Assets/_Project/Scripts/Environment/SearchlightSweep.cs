using UnityEngine;

namespace AirDefense.Environment
{
    /// <summary>
    /// Sweeps a searchlight head back and forth across the sky and draws its beam in the haze
    /// (VS-4, docs/VFX_AND_ANIMATION_SPEC.md §8):
    /// - a double-sided cone drawn with the AirDefense/LightBeam shader (soft edges, falloff, drifting dust);
    /// - a glow at the lamp head that flares when the beam points toward the camera, so every beam has a
    ///   visible source;
    /// - a real spot light (set up by the scene builder) for what the beam actually lights.
    /// </summary>
    public sealed class SearchlightSweep : MonoBehaviour
    {
        public Transform head;
        public Light spot;
        public Material beamMaterial;
        [Tooltip("Soft round additive sprite for the lamp glow (optional).")]
        public Material glowMaterial;
        public float beamLength = 260f;
        public float beamRadius = 7f;
        public float centerYaw = 0f;
        public float yawRange = 50f;
        public float pitch = 32f;
        public float pitchWobble = 8f;
        public float period = 14f;
        public float phase;

        private Transform _beam;
        private Transform _glow;
        private Material _glowInstance;
        private static Mesh _cone, _quad;

        private void Start()
        {
            var go = new GameObject("Beam");
            go.transform.SetParent(head, false);
            go.AddComponent<MeshFilter>().sharedMesh = _cone ??= BuildCone(beamLength, beamRadius, 32);
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = beamMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            _beam = go.transform;

            if (glowMaterial != null)
            {
                var g = new GameObject("LampGlow");
                g.transform.SetParent(head, false);
                g.transform.localPosition = Vector3.forward * 0.6f;
                g.AddComponent<MeshFilter>().sharedMesh = _quad ??= BuildQuad();
                var gr = g.AddComponent<MeshRenderer>();
                _glowInstance = new Material(glowMaterial);
                gr.sharedMaterial = _glowInstance;
                gr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                gr.receiveShadows = false;
                _glow = g.transform;
            }
        }

        private void Update()
        {
            float t = (Time.time + phase) * 2f * Mathf.PI / period;
            float yaw = centerYaw + Mathf.Sin(t) * yawRange;
            float p = pitch + Mathf.Sin(t * 0.37f + 1.3f) * pitchWobble;
            head.rotation = Quaternion.Euler(-p, yaw, 0f);
        }

        private void LateUpdate()
        {
            if (_glow == null) return;
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 toCam = cam.transform.position - _glow.position;
            float dist = toCam.magnitude;
            // How directly the lamp faces the camera: the lens flares when we look into the beam.
            float facing = Mathf.Max(0f, Vector3.Dot(head.forward, toCam / Mathf.Max(0.01f, dist)));
            float flare = Mathf.Pow(facing, 6f);
            float size = Mathf.Max(1.6f, dist * 0.004f) * (1f + flare * 5f);
            _glow.rotation = cam.transform.rotation;
            _glow.localScale = Vector3.one * size;
            _glowInstance.SetColor("_BaseColor", new Color(2.2f, 2.3f, 2.5f, 1f) * (0.35f + 1.8f * flare));
        }

        /// <summary>Open cone along +Z with outward normals and UV.y = 0 at the lamp .. 1 at the far end.</summary>
        private static Mesh BuildCone(float length, float radius, int sides)
        {
            const float lampRadius = 0.35f;
            var verts = new Vector3[(sides + 1) * 2];
            var normals = new Vector3[verts.Length];
            var uvs = new Vector2[verts.Length];
            var tris = new int[sides * 6];
            float slope = (radius - lampRadius) / length;
            for (int i = 0; i <= sides; i++)
            {
                float a = i / (float)sides * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                var n = (dir - Vector3.forward * slope).normalized;
                verts[i] = dir * lampRadius;
                verts[i + sides + 1] = dir * radius + Vector3.forward * length;
                normals[i] = normals[i + sides + 1] = n;
                uvs[i] = new Vector2(i / (float)sides, 0f);
                uvs[i + sides + 1] = new Vector2(i / (float)sides, 1f);
            }
            for (int i = 0; i < sides; i++)
            {
                int k = i * 6, b = i, c = i + sides + 1;
                tris[k] = b; tris[k + 1] = c; tris[k + 2] = b + 1;
                tris[k + 3] = b + 1; tris[k + 4] = c; tris[k + 5] = c + 1;
            }
            var m = new Mesh { name = "SearchlightBeam", vertices = verts, normals = normals, uv = uvs, triangles = tris };
            m.RecalculateBounds();
            return m;
        }

        private static Mesh BuildQuad()
        {
            var m = new Mesh { name = "GlowQuad" };
            m.vertices = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(-0.5f, 0.5f, 0) };
            m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            m.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.RecalculateBounds();
            return m;
        }
    }
}
