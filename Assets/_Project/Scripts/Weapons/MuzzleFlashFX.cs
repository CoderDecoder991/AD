using UnityEngine;

namespace AirDefense.Weapons
{
    /// <summary>
    /// Realistic muzzle flash per barrel (user feedback: the earlier star sprite looked "animated").
    /// A real autocannon flash is an irregular, turbulent fireball that differs every shot and
    /// lasts 2-3 frames: a white-hot core at the muzzle and a few orange flame lobes pushed
    /// forward by the propellant gas, then grey smoke. Built from particles using four
    /// noise-generated fireball images (random image, size and rotation per puff), plus a short
    /// light flash that lights the gun and surroundings at night.
    /// </summary>
    public sealed class MuzzleFlashFX : MonoBehaviour
    {
        public GunMount mount;
        public Camera viewCamera;
        [Tooltip("Additive material using the 2x2 fireball sheet (MakeFireballSheet).")]
        public Material fireMaterial;
        public Material smokeMaterial;   // alpha-blended soft dot

        [Header("Look")]
        public float coreSize = 0.75f;
        public float lobeSize = 0.6f;
        public float lobeReach = 1.1f;       // how far forward the flame lobes reach, metres
        public float flashLife = 0.04f;      // seconds (2-3 frames at 60 fps)
        public float lightIntensity = 10f;
        public float lightRange = 16f;
        [Tooltip("Smoke puff on every Nth flash (high-rate guns would otherwise overflow the smoke pool).")]
        public int smokeEvery = 1;
        private int _flashCount;

        private ParticleSystem[] _fire;
        private ParticleSystem[] _smoke;
        private Light _light;
        private float _lightT = 999f;
        private readonly System.Random _rng = new System.Random(77);

        // Kept so the scene builder's older references still compile.
        [HideInInspector] public Material burstMaterial;
        [HideInInspector] public Material tongueMaterial;

        private void Start()
        {
            var muzzles = mount.muzzles;
            _fire = new ParticleSystem[muzzles.Length];
            _smoke = new ParticleSystem[muzzles.Length];
            for (int i = 0; i < muzzles.Length; i++)
            {
                _fire[i] = MakeFire(muzzles[i]);
                _smoke[i] = MakeSmoke(muzzles[i]);
            }
            _light = new GameObject("MuzzleLight").AddComponent<Light>();
            _light.transform.SetParent(mount.pitchPivot, false);
            _light.transform.position = (muzzles[0].position + muzzles[muzzles.Length - 1].position) * 0.5f + mount.BoreDirection * 0.5f;
            _light.type = LightType.Point;
            _light.color = new Color(1f, 0.7f, 0.35f);
            _light.range = lightRange;
            _light.intensity = 0f;
            _light.shadows = LightShadows.None;
        }

        private float R => (float)_rng.NextDouble();

        public void Flash(int barrel)
        {
            if (_fire == null) return;
            int i = barrel % _fire.Length;
            Vector3 origin = mount.muzzles[i].position;
            Vector3 fwd = mount.BoreDirection;
            Vector3 side = Vector3.Cross(fwd, Vector3.up).normalized;
            if (side.sqrMagnitude < 0.01f) side = Vector3.right;
            Vector3 up = Vector3.Cross(side, fwd);
            ParticleSystem ps = _fire[i];

            // White-hot core right at the muzzle.
            Emit(ps, origin + fwd * 0.08f, coreSize * (0.8f + R * 0.5f), flashLife * (0.8f + R * 0.4f), new Color(1f, 0.93f, 0.75f, 1f), fwd * 2f);

            // 2-4 orange lobes, pushed forward and slightly off-axis, each a different shape.
            int lobes = 2 + _rng.Next(3);
            for (int k = 0; k < lobes; k++)
            {
                float along = lobeReach * (0.25f + R * 0.75f);
                Vector3 off = side * (R - 0.5f) * 0.35f + up * (R - 0.5f) * 0.35f;
                float size = lobeSize * (0.6f + R * 0.8f) * (1f - along / (lobeReach * 2.2f));
                var col = Color.Lerp(new Color(1f, 0.62f, 0.25f, 0.95f), new Color(1f, 0.42f, 0.12f, 0.8f), along / lobeReach);
                Emit(ps, origin + fwd * along + off, size, flashLife * (0.7f + R * 0.6f), col, fwd * (6f + R * 6f));
            }

            // Brief side flare from the flash hider (smaller, sometimes absent).
            if (R < 0.6f)
            {
                float s = R < 0.5f ? 1f : -1f;
                Emit(ps, origin + fwd * 0.12f + side * s * 0.18f, lobeSize * 0.45f, flashLife * 0.6f,
                     new Color(1f, 0.55f, 0.2f, 0.7f), side * s * 4f);
            }

            _lightT = 0f;
            _light.transform.position = origin + fwd * 0.6f;

            // Smoke puff that lingers and drifts.
            if (smokeEvery > 1 && (_flashCount++ % smokeEvery) != 0) return;
            var ep = new ParticleSystem.EmitParams
            {
                position = origin + fwd * 0.3f,
                applyShapeToPosition = false,
                velocity = fwd * (2f + R * 2.5f) + Vector3.up * 0.4f,
                startSize = 0.5f + R * 0.5f,
                startLifetime = 1.4f + R * 1.0f,
                rotation = R * 360f,
            };
            _smoke[i].Emit(ep, 1);
        }

        private void Emit(ParticleSystem ps, Vector3 pos, float size, float life, Color c, Vector3 vel)
        {
            var ep = new ParticleSystem.EmitParams
            {
                position = pos,
                applyShapeToPosition = false,
                velocity = vel,
                startSize = size,
                startLifetime = life,
                startColor = c,
                rotation = R * 360f,
            };
            ps.Emit(ep, 1);
        }

        private void LateUpdate()
        {
            _lightT += Time.deltaTime;
            float lt = flashLife * 1.3f;
            if (_light != null) _light.intensity = _lightT < lt ? lightIntensity * (1f - _lightT / lt) * (0.85f + R * 0.3f) : 0f;
        }

        private ParticleSystem MakeFire(Transform muzzle)
        {
            var go = new GameObject("MuzzleFire");
            go.transform.SetParent(muzzle, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.maxParticles = 120; // C-RAM: up to 75 flashes a second
            var em = ps.emission; em.enabled = false;
            var shape = ps.shape; shape.enabled = false;
            // Pick one of four fireball shapes at random (no animation: real flash is a single exposure).
            var sheet = ps.textureSheetAnimation;
            sheet.enabled = true;
            sheet.numTilesX = 2;
            sheet.numTilesY = 2;
            sheet.animation = ParticleSystemAnimationType.WholeSheet;
            sheet.frameOverTime = new ParticleSystem.MinMaxCurve(0f);
            sheet.startFrame = new ParticleSystem.MinMaxCurve(0f, 3.999f);
            sheet.cycleCount = 1;
            // Swell slightly and fade fast.
            var size = ps.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.85f, 1f, 1.25f));
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.6f, 0.3f), 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.7f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = fireMaterial;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            ps.Play();
            return ps;
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
            main.maxParticles = 80;
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

        // ------------------------------------------------------------ textures

        /// <summary>
        /// 2x2 sheet of turbulent fireballs: fractal value noise warps a radial falloff so every
        /// tile is an irregular blob with a hot centre and ragged edges (not a geometric star).
        /// </summary>
        public static Texture2D MakeFireballSheet(int tile = 128)
        {
            int size = tile * 2;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, name = "FireballSheet" };
            var px = new Color[size * size];
            for (int ty = 0; ty < 2; ty++)
            for (int tx = 0; tx < 2; tx++)
            {
                float seed = 13.7f + (ty * 2 + tx) * 41.3f;
                for (int y = 0; y < tile; y++)
                for (int x = 0; x < tile; x++)
                {
                    float u = (x + 0.5f) / tile * 2f - 1f, v = (y + 0.5f) / tile * 2f - 1f;
                    // Domain warp: push the sample point around with 2D noise so the blob billows
                    // in lumps (no radial streaks, which read as a cartoon star).
                    float wu = u + 0.45f * (Fbm(u * 1.6f + seed, v * 1.6f + 3.1f, 4) - 0.5f);
                    float wv = v + 0.45f * (Fbm(u * 1.6f - 7.7f, v * 1.6f + seed, 4) - 0.5f);
                    float r = Mathf.Sqrt(wu * wu + wv * wv);
                    float edge = 0.62f + 0.3f * (Fbm(wu * 2.2f + seed, wv * 2.2f - seed, 4) - 0.5f) * 2f;
                    float turb = Fbm(wu * 4.5f + seed, wv * 4.5f - seed, 5);
                    float m = Mathf.Clamp01(1f - r / Mathf.Max(0.2f, edge));
                    float a = Mathf.SmoothStep(0f, 1f, m) * Mathf.Lerp(0.45f, 1.2f, turb);
                    a = Mathf.Clamp01(a);
                    float heat = Mathf.Clamp01(m * 1.5f * turb + 0.15f);
                    Color c = Color.Lerp(new Color(1f, 0.45f, 0.12f), new Color(1f, 0.97f, 0.85f), heat * heat);
                    px[(ty * tile + y) * size + tx * tile + x] = new Color(c.r, c.g, c.b, a);
                }
            }
            tex.SetPixels(px);
            tex.Apply(true);
            return tex;
        }

        private static float Fbm(float x, float y, int octaves)
        {
            float sum = 0f, amp = 0.5f, freq = 1f, norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += amp * Mathf.PerlinNoise(x * freq, y * freq);
                norm += amp;
                amp *= 0.5f;
                freq *= 2.03f;
            }
            return sum / norm;
        }

        /// <summary>
        /// Tracer streak: bright glowing head at the front (v=1), fading tail behind (v=0),
        /// soft glowing edges across (u). Rendered stretched along the round's velocity.
        /// </summary>
        /// <summary>
        /// Tracer as a camera records it: rows 0-0.25 and 0.75-1 are round end caps, 0.25-0.75 the evenly lit
        /// body (stretched along the streak). Across: a saturated yellow-white core with a fairly sharp edge,
        /// then a red-orange halo (strontium flame and lens glow) fading out.
        /// </summary>
        public static Texture2D MakeTracerTexture(int w = 64, int h = 128)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, name = "TracerCapsule" };
            var px = new Color[w * h];
            var hot = new Color(1f, 0.95f, 0.82f);
            var red = new Color(1f, 0.32f, 0.12f);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float v = (y + 0.5f) / h;
                float u = Mathf.Abs((x + 0.5f) / w * 2f - 1f);
                float dy = v < 0.25f ? (0.25f - v) / 0.25f : v > 0.75f ? (v - 0.75f) / 0.25f : 0f;
                float d = Mathf.Sqrt(u * u + dy * dy);                    // 0 at the centre line, 1 at the rim
                float core = 1f - Mathf.SmoothStep(0.1f, 0.28f, d);         // clipped (saturated) centre, thin
                float halo = Mathf.Exp(-d * d * 5f) * (1f - Mathf.SmoothStep(0.8f, 1f, d));
                float a = Mathf.Clamp01(core + halo * 0.4f);
                Color c = Color.Lerp(red, hot, core);
                px[y * w + x] = new Color(c.r, c.g, c.b, a);
            }
            tex.SetPixels(px);
            tex.Apply(true);
            return tex;
        }
    }
}
