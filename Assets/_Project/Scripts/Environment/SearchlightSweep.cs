using UnityEngine;

namespace AirDefense.Environment
{
    /// <summary>
    /// Sweeps a searchlight head back and forth across the sky and draws a
    /// fake volumetric beam (additive cone, bright at the lamp, fading out).
    /// No real volumetrics: cheap on mobile (CINEMATIC_OPENING.md §4 "light cones faked with meshes").
    /// </summary>
    public sealed class SearchlightSweep : MonoBehaviour
    {
        public Transform head;
        public Light spot;
        public Material beamMaterial;
        public float beamLength = 220f;
        public float beamRadius = 9f;
        public float centerYaw = 0f;
        public float yawRange = 50f;
        public float pitch = 32f;
        public float pitchWobble = 8f;
        public float period = 14f;
        public float phase;

        private Transform _beam;

        private void Start()
        {
            var go = new GameObject("Beam");
            go.transform.SetParent(head, false);
            go.AddComponent<MeshFilter>().sharedMesh = BuildCone(beamLength, beamRadius, 18);
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = beamMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            _beam = go.transform;
        }

        private void Update()
        {
            float t = (Time.time + phase) * 2f * Mathf.PI / period;
            float yaw = centerYaw + Mathf.Sin(t) * yawRange;
            float p = pitch + Mathf.Sin(t * 0.37f + 1.3f) * pitchWobble;
            head.rotation = Quaternion.Euler(-p, yaw, 0f);
        }

        /// <summary>Open cone along +Z; vertex alpha fades from the lamp to the tip.</summary>
        private static Mesh BuildCone(float length, float radius, int sides)
        {
            var verts = new Vector3[sides * 2];
            var cols = new Color[sides * 2];
            var tris = new int[sides * 6];
            for (int i = 0; i < sides; i++)
            {
                float a = i / (float)sides * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                verts[i] = dir * 0.35f;                                   // at the lamp
                verts[i + sides] = dir * radius + Vector3.forward * length; // far end
                cols[i] = new Color(1f, 1f, 1f, 0.22f);
                cols[i + sides] = new Color(1f, 1f, 1f, 0f);
                int n = (i + 1) % sides;
                int k = i * 6;
                tris[k] = i; tris[k + 1] = i + sides; tris[k + 2] = n;
                tris[k + 3] = n; tris[k + 4] = i + sides; tris[k + 5] = n + sides;
            }
            var m = new Mesh { name = "SearchlightBeam", vertices = verts, colors = cols, triangles = tris };
            m.RecalculateBounds();
            return m;
        }
    }
}
