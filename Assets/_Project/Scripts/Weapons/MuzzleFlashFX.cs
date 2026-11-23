using UnityEngine;

namespace AirDefense.Weapons
{
    /// <summary>
    /// Muzzle flash for each barrel: a camera-facing star burst, a forward flame
    /// tongue (two crossed quads along the bore), a short light flash that lights
    /// up the gun and surroundings at night, and a smoke puff. Each shot varies size
    /// and rotation so the flash never looks repeated. Mobile-friendly: a few quads
    /// per barrel, one light, small particle counts.
    /// </summary>
    public sealed class MuzzleFlashFX : MonoBehaviour
    {
        public GunMount mount;
        public Camera viewCamera;
        public Material burstMaterial;   // additive, star-burst texture (HDR colour)
        public Material tongueMaterial;  // additive, flame-tongue texture (HDR colour)
        public Material smokeMaterial;   // alpha-blended soft dot

        [Header("Look")]
        public float burstSize = 0.95f;
        public float tongueLength = 1.5f;
        public float tongueWidth = 0.55f;
        public float flashTime = 0.045f;      // ~3 frames at 60 fps
        public float lightIntensity = 9f;
        public float lightRange = 14f;

        private sealed class Barrel
        {
            public Transform burst;
            public Transform tongue;
            public float t = 999f;
            public float scale = 1f;
            public ParticleSystem smoke;
        }

        private Barrel[] _b;
        private Light _light;
        private float _lightT = 999f;
        private static Mesh _quad;
        private readonly System.Random _rng = new System.Random(77);

        private void Start()
        {
            _quad ??= BuildQuad();
            var muzzles = mount.muzzles;
            _b = new Barrel[muzzles.Length];
            for (int i = 0; i < muzzles.Length; i++)
            {
                var b = new Barrel();
                b.burst = Part("FlashBurst", muzzles[i], burstMaterial).transform;
                var tongueRoot = new GameObject("FlashTongue").transform;
                tongueRoot.SetParent(muzzles[i], false);
                // Two quads crossed along the bore (local +Z of the muzzle's parent frame).
                for (int k = 0; k < 2; k++)
                {
                    var q = Part("Tongue" + k, tongueRoot, tongueMaterial).transform;
                    // Quad +Y -> root +Z (the bore); second quad turned 90° about the bore. Starts at the muzzle.
                    q.localRotation = Quaternion.Euler(0f, 0f, k * 90f) * Quaternion.Euler(90f, 0f, 0f);
                    q.localPosition = new Vector3(0f, 0f, 0.5f);
                }
                b.tongue = tongueRoot;
                b.burst.gameObject.SetActive(false);
                b.tongue.gameObject.SetActive(false);
                b.smoke = MakeSmoke(muzzles[i]);
                _b[i] = b;
            }

            _light = new GameObject("MuzzleLight").AddComponent<Light>();
            _light.transform.SetParent(mount.pitchPivot, false);
            _light.transform.position = (muzzles[0].position + muzzles[muzzles.Length - 1].position) * 0.5f;
            _light.type = LightType.Point;
            _light.color = new Color(1f, 0.72f, 0.38f);
            _light.range = lightRange;
            _light.intensity = 0f;
            _light.shadows = LightShadows.None;
        }

        public void Flash(int barrel)
        {
            if (_b == null) return;
            var b = _b[barrel % _b.Length];
            b.t = 0f;
            b.scale = 0.75f + (float)_rng.NextDouble() * 0.5f;
            float roll = (float)_rng.NextDouble() * 360f;
            b.burst.gameObject.SetActive(true);
            b.tongue.gameObject.SetActive(true);
            b.burst.localScale = Vector3.one * burstSize * b.scale;
            b.tongue.rotation = Quaternion.LookRotation(mount.BoreDirection) * Quaternion.Euler(0f, 0f, roll);
            b.tongue.localScale = new Vector3(tongueWidth * b.scale, tongueWidth * b.scale, tongueLength * (0.7f + (float)_rng.NextDouble() * 0.6f));
            b.burst.localRotation = Quaternion.identity;
            _lightT = 0f;

            var ep = new ParticleSystem.EmitParams
            {
                velocity = mount.BoreDirection * (2f + (float)_rng.NextDouble() * 2f) + Vector3.up * 0.4f,
                startSize = 0.6f + (float)_rng.NextDouble() * 0.5f,
                startLifetime = 1.4f + (float)_rng.NextDouble() * 0.8f,
                rotation = (float)_rng.NextDouble() * 360f,
            };
            b.smoke.Emit(ep, 1);
        }

        private void LateUpdate()
        {
            if (_b == null) return;
            float dt = Time.deltaTime;
            Vector3 camPos = viewCamera ? viewCamera.transform.position : Vector3.zero;
            foreach (var b in _b)
            {
                if (b.t > flashTime) continue;
                b.t += dt;
                if (b.t > flashTime)
                {
                    b.burst.gameObject.SetActive(false);
                    b.tongue.gameObject.SetActive(false);
                    continue;
                }
                // Burst faces the camera; it pops big then shrinks over its short life.
                float k = b.t / flashTime;
                b.burst.rotation = Quaternion.LookRotation(b.burst.position - camPos) * Quaternion.Euler(0f, 0f, b.scale * 997f);
                b.burst.localScale = Vector3.one * burstSize * b.scale * Mathf.Lerp(1.15f, 0.6f, k);
            }
            _lightT += dt;
            _light.intensity = _lightT < flashTime * 1.4f ? lightIntensity * (1f - _lightT / (flashTime * 1.4f)) : 0f;
        }

        private GameObject Part(string n, Transform parent, Material m)
        {
            var go = new GameObject(n);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = _quad;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go;
        }

        private ParticleSystem MakeSmoke(Transform muzzle)
        {
            var go = new GameObject("MuzzleSmoke");
            go.transform.SetParent(muzzle, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.maxParticles = 50;
            main.startColor = new Color(0.55f, 0.55f, 0.55f, 0.35f);
            var em = ps.emission; em.enabled = false;
            var shape = ps.shape; shape.enabled = false;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(new Color(0.6f, 0.6f, 0.6f), 0f), new GradientColorKey(new Color(0.35f, 0.35f, 0.37f), 1f) },
                      new[] { new GradientAlphaKey(0.4f, 0f), new GradientAlphaKey(0.18f, 0.4f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var size = ps.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 3.2f));
            var limit = ps.limitVelocityOverLifetime; limit.enabled = true; limit.limit = 0.6f; limit.dampen = 0.25f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = smokeMaterial;
            ps.Play();
            return ps;
        }

        /// <summary>Unit quad in the XY plane, centred horizontally, from z=0 forward for tongues.</summary>
        private static Mesh BuildQuad()
        {
            // Burst uses it centred; tongue quads are rotated so the quad's +Y runs along the bore.
            var m = new Mesh { name = "FlashQuad" };
            m.vertices = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(-0.5f, 0.5f, 0) };
            m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        // ------------------------------------------------------------ textures

        /// <summary>Star-burst flash texture: hot core plus 6 irregular spikes.</summary>
        public static Texture2D MakeBurstTexture(int size = 128, int seed = 3)
        {
            var rnd = new System.Random(seed);
            var spikes = new float[6];
            var widths = new float[6];
            for (int i = 0; i < 6; i++) { spikes[i] = (float)(i * 60 + rnd.Next(-14, 14)); widths[i] = 0.10f + (float)rnd.NextDouble() * 0.08f; }
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, name = "FlashBurst" };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                float core = Mathf.Clamp01(1f - r / 0.38f);
                float spike = 0f;
                for (int i = 0; i < 6; i++)
                {
                    float d = Mathf.Abs(Mathf.DeltaAngle(ang, spikes[i])) / 60f;
                    float w = widths[i] * (1f - r);
                    spike = Mathf.Max(spike, Mathf.Clamp01(1f - d / Mathf.Max(0.01f, w)) * Mathf.Clamp01(1f - r));
                }
                float a = Mathf.Clamp01(core * core * 1.2f + spike * 0.9f);
                float heat = Mathf.Clamp01(core * 1.4f); // white-hot centre, orange edges
                var c = Color.Lerp(new Color(1f, 0.55f, 0.18f), new Color(1f, 0.95f, 0.8f), heat);
                px[y * size + x] = new Color(c.r, c.g, c.b, a);
            }
            tex.SetPixels32(px);
            tex.Apply(true);
            return tex;
        }

        /// <summary>Flame-tongue texture: bright at the muzzle (bottom), tapering and fading forward (top).</summary>
        public static Texture2D MakeTongueTexture(int w = 64, int h = 128)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, name = "FlashTongue" };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float v = (y + 0.5f) / h;                 // 0 at muzzle, 1 at tip
                float u = Mathf.Abs((x + 0.5f) / w * 2f - 1f);
                float width = Mathf.Lerp(0.55f, 0.08f, v) + 0.12f * Mathf.Sin(v * 9f) * (1f - v);
                float a = Mathf.Clamp01(1f - u / Mathf.Max(0.02f, width)) * Mathf.Pow(1f - v, 1.4f);
                var c = Color.Lerp(new Color(1f, 0.9f, 0.7f), new Color(1f, 0.45f, 0.12f), v);
                px[y * w + x] = new Color(c.r, c.g, c.b, a);
            }
            tex.SetPixels32(px);
            tex.Apply(true);
            return tex;
        }
    }
}
