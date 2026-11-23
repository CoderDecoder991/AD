using System.Collections.Generic;
using AirDefense.Core;
using AirDefense.Threats;
using UnityEngine;

namespace AirDefense.Environment
{
    /// <summary>
    /// Shows installation damage (GDD §5): every threat that gets through leaves a
    /// fire with a rising smoke column at the impact point (oldest is reused past
    /// the cap). Particle systems are built in code with a generated soft sprite.
    /// Particle counts are small for mobile.
    /// </summary>
    public sealed class DamageVisuals : MonoBehaviour
    {
        public Material particleMaterial; // URP Particles/Unlit, additive-friendly, soft texture
        public int maxFires = 8;

        private readonly Queue<GameObject> _fires = new Queue<GameObject>();

        private void OnEnable() => GameEvents.ThreatImpacted += OnImpact;
        private void OnDisable() => GameEvents.ThreatImpacted -= OnImpact;

        private void OnImpact(ThreatAgent t)
        {
            Vector3 p = t.transform.position;
            if (_fires.Count >= maxFires) Destroy(_fires.Dequeue());
            var root = new GameObject("DamageFire");
            root.transform.SetParent(transform, false);
            root.transform.position = p;
            MakeFire(root.transform);
            MakeSmoke(root.transform);
            _fires.Enqueue(root);
        }

        private void MakeFire(Transform parent)
        {
            var ps = Create(parent, "Fire", out var main, out var r);
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(1.5f, 3.2f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.55f, 0.15f, 0.9f), new Color(1f, 0.3f, 0.05f, 0.8f));
            main.maxParticles = 30;
            var em = ps.emission; em.rateOverTime = 22f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 1.2f; sh.rotation = new Vector3(-90f, 0f, 0f);
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = Fade(new Color(1f, 0.7f, 0.3f), new Color(0.6f, 0.1f, 0.02f));
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0.3f));
            ps.Play();

            // A flickering light makes the fire light up its surroundings at night.
            var light = new GameObject("FireLight").AddComponent<Light>();
            light.transform.SetParent(parent, false);
            light.transform.localPosition = Vector3.up * 2f;
            light.type = LightType.Point;
            light.color = new Color(1f, 0.5f, 0.2f);
            light.range = 18f;
            light.intensity = 3f;
            light.shadows = LightShadows.None;
            light.gameObject.AddComponent<Flicker>();
        }

        private void MakeSmoke(Transform parent)
        {
            var ps = Create(parent, "Smoke", out var main, out var r);
            r.sortingFudge = 10f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 3.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(3f, 6f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.12f, 0.12f, 0.12f, 0.55f));
            main.maxParticles = 40;
            var em = ps.emission; em.rateOverTime = 6f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 1f; sh.rotation = new Vector3(-90f, 0f, 0f);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(0.8f, 1.6f); vel.y = new ParticleSystem.MinMaxCurve(0f); vel.z = new ParticleSystem.MinMaxCurve(0.2f);
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = Fade(new Color(0.15f, 0.15f, 0.15f), new Color(0.3f, 0.3f, 0.32f));
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.6f, 1, 2.5f));
            ps.Play();
        }

        private ParticleSystem Create(Transform parent, string n, out ParticleSystem.MainModule main, out ParticleSystemRenderer r)
        {
            var go = new GameObject(n);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = true;
            r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = particleMaterial;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            return ps;
        }

        private static ParticleSystem.MinMaxGradient Fade(Color start, Color end)
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(start, 0f), new GradientColorKey(end, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
            return new ParticleSystem.MinMaxGradient(g);
        }

        /// <summary>Generated soft round sprite for particles (no texture asset needed).</summary>
        public static Texture2D SoftDot(int size = 64)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, name = "SoftDot" };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * a * 255f));
            }
            tex.SetPixels32(px);
            tex.Apply(true);
            return tex;
        }
    }

    /// <summary>Random light flicker for fires.</summary>
    public sealed class Flicker : MonoBehaviour
    {
        private Light _l;
        private float _base;
        private float _seed;
        private void Awake() { _l = GetComponent<Light>(); _base = _l.intensity; _seed = Random.value * 100f; }
        private void Update() => _l.intensity = _base * (0.75f + 0.5f * Mathf.PerlinNoise(_seed, Time.time * 6f));
    }

    /// <summary>Blinks a navigation light on a threat (readability at night).</summary>
    public sealed class NavLightBlink : MonoBehaviour
    {
        public float period = 1.1f;
        private Renderer _r;
        private float _offset;
        private void Awake() { _r = GetComponent<Renderer>(); _offset = Random.value * period; }
        private void Update() => _r.enabled = Mathf.Repeat(Time.time + _offset, period) < period * 0.35f;
    }
}
