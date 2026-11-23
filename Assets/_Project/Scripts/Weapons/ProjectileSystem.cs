using System.Collections.Generic;
using AirDefense.Core;
using AirDefense.Threats;
using UnityEngine;

namespace AirDefense.Weapons
{
    /// <summary>
    /// Pooled gun rounds with realistic flight and appearance (research notes in
    /// docs/WEAPON_SYSTEMS.md "Tracer behaviour"):
    /// - Physics: gravity plus quadratic air drag, so rounds slow (970 -> ~610 m/s by 1 km) and arc.
    /// - Hits: segment-vs-sphere tests against live threats each frame (no physics colliders).
    /// - Tracer: a burning point of light. What the eye/camera sees is the light smeared over a short
    ///   exposure, so the visible trail = the distance flown in ~1/60 s (shrinks as the round slows),
    ///   with a bright head and fading tail, slight flicker, and a minimum on-screen size so distant
    ///   tracers stay visible as points (bright point sources don't shrink to nothing).
    /// - The tracer burns out after its burn time; the round flies on unseen until it self-destructs
    ///   with a small flash and smoke puff (the twinkling seen in night footage of 23 mm fire).
    /// </summary>
    public sealed class ProjectileSystem : MonoBehaviour
    {
        public PrototypeVisuals visuals;
        public ThreatManager threats;
        [Tooltip("Camera the streaks face and size against. Defaults to the main camera.")]
        public Camera viewCamera;

        [Header("Tracer look")]
        [Tooltip("Exposure time that sets the trail length (distance flown in this time).")]
        public float exposure = 1f / 60f; // typical camera shutter: at 970 m/s gives ~16 m dashes with gaps between rounds
        [Tooltip("Physical glow width, metres.")]
        public float tracerWidth = 0.35f;
        [Tooltip("Minimum width on screen, in pixels, so far tracers remain visible points of light.")]
        public float minPixelWidth = 8f;
        [Tooltip("Tracers appear once the round is this far from the muzzle (they ignite almost immediately).")]
        public float tracerHiddenDistance = 3f;

        [Header("Self-destruct burst (optional)")]
        public Material burstFireMaterial;   // additive glow, soft round texture (no fog)
        public Material burstSmokeMaterial;  // alpha soft dot, no fog (dark puff readable against night sky)

        private struct Round
        {
            public Vector3 pos;
            public Vector3 vel;
            public float age;
            public float life;          // self-destruct (or expiry) time
            public float tracerBurn;    // tracer visible until this age
            public float drag;
            public float damage;
            public float travelled;
            public bool selfDestruct;
            public int tracerIndex;     // -1 = no tracer
            public float flickerSeed;
            public float burstScale;    // self-destruct flash/puff size (1 = 23 mm)
            public bool burstSmoke;     // this round's self-destruct leaves a puff and a pop
            public bool counted;        // counts toward the player's accuracy (manual fire)
        }

        private readonly List<Round> _rounds = new List<Round>(512);
        private readonly Stack<int> _freeTracers = new Stack<int>();
        private readonly List<Transform> _tracerObjects = new List<Transform>();
        private Transform _tracerRoot;
        private static Mesh _ribbon;
        private ParticleSystem _burstFire;
        private ParticleSystem _burstSmoke;
        private readonly System.Random _rng = new System.Random(91);

        public int RoundsHit { get; private set; }
        public int LiveRounds => _rounds.Count;

        private void Awake()
        {
            _tracerRoot = new GameObject("Tracers").transform;
            _tracerRoot.SetParent(transform, false);
            _ribbon ??= BuildRibbon();
            if (burstFireMaterial != null) _burstFire = MakeBurstSystem("SelfDestructFlash", burstFireMaterial, true);
            if (burstSmokeMaterial != null) _burstSmoke = MakeBurstSystem("SelfDestructSmoke", burstSmokeMaterial, false);
        }

        /// <summary>Simple round (no drag, fixed lifetime). Kept for tests and older callers.</summary>
        public void Fire(Vector3 origin, Vector3 direction, float speed, float lifetime, float damage, bool tracer) =>
            Fire(origin, direction, speed, 0f, lifetime, tracer ? lifetime : 0f, false, damage);

        /// <summary>Realistic round: drag, tracer burn time, and optional self-destruct at <paramref name="life"/>.</summary>
        /// <param name="burstScale">Self-destruct flash/puff size (1 = 23 mm HE).</param>
        /// <param name="burstSmoke">Whether the self-destruct leaves a smoke puff and a pop.</param>
        /// <param name="counted">Whether a hit counts toward the player's accuracy (false for autonomous fire).</param>
        public void Fire(Vector3 origin, Vector3 direction, float speed, float dragK, float life, float tracerBurn, bool selfDestruct, float damage,
                         float burstScale = 1f, bool burstSmoke = true, bool counted = true)
        {
            _rounds.Add(new Round
            {
                pos = origin,
                vel = direction.normalized * speed,
                life = life,
                tracerBurn = tracerBurn,
                drag = dragK,
                damage = damage,
                selfDestruct = selfDestruct,
                tracerIndex = tracerBurn > 0f ? AcquireTracer() : -1,
                flickerSeed = (float)_rng.NextDouble() * 100f,
                burstScale = burstScale,
                burstSmoke = burstSmoke,
                counted = counted,
            });
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            var live = threats.Active;
            Camera cam = viewCamera != null ? viewCamera : Camera.main;
            Vector3 camPos = cam != null ? cam.transform.position : Vector3.zero;
            // Metres per pixel at 1 m distance, for the minimum on-screen width.
            float pxAngle = cam != null ? 2f * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) / Mathf.Max(1, Screen.height) : 0f;

            for (int i = _rounds.Count - 1; i >= 0; i--)
            {
                Round r = _rounds[i];
                Vector3 p0 = r.pos;
                r.vel = Ballistics.Step(r.vel, r.drag, dt);
                r.pos += r.vel * dt;
                r.travelled += (r.pos - p0).magnitude;
                r.age += dt;

                bool done = r.pos.y < 0f;
                if (!done && r.age >= r.life)
                {
                    done = true;
                    if (r.selfDestruct) SelfDestructBurst(r.pos, r.vel, Vector3.Distance(camPos, r.pos) * pxAngle, r.burstScale, r.burstSmoke);
                }

                if (!done)
                {
                    for (int t = 0; t < live.Count; t++)
                    {
                        ThreatAgent threat = live[t];
                        if (!threat.Alive) continue;
                        if (Ballistics.SegmentHitsSphere(p0, r.pos, threat.transform.position, threat.Definition.hitRadius))
                        {
                            threat.ApplyDamage(r.damage);
                            if (r.counted) RoundsHit++;
                            done = true;
                            break;
                        }
                    }
                }

                if (r.tracerIndex >= 0)
                {
                    Transform tr = _tracerObjects[r.tracerIndex];
                    bool burning = r.age < r.tracerBurn;
                    if (done || !burning)
                    {
                        tr.gameObject.SetActive(false);
                        _freeTracers.Push(r.tracerIndex);
                        r.tracerIndex = -1;
                    }
                    else if (r.travelled >= tracerHiddenDistance)
                    {
                        if (!tr.gameObject.activeSelf) tr.gameObject.SetActive(true);
                        float speed = r.vel.magnitude;
                        Vector3 dir = r.vel / Mathf.Max(1e-3f, speed);
                        float dist = Vector3.Distance(camPos, r.pos);
                        // Trail = distance flown during the exposure; never longer than the flight so far.
                        float len = Mathf.Min(speed * exposure, r.travelled);
                        // Keep at least minPixelWidth on screen; trail at least ~3x that so it reads as a streak.
                        float minW = dist * pxAngle * minPixelWidth;
                        float width = Mathf.Max(tracerWidth, minW);
                        len = Mathf.Max(len, width * 5f);
                        // Burning composition flickers slightly; fades in the last 0.3 s before burnout.
                        float flicker = 0.85f + 0.3f * Mathf.PerlinNoise(r.flickerSeed, r.age * 25f);
                        float fade = Mathf.Clamp01((r.tracerBurn - r.age) / 0.3f);
                        width *= flicker * Mathf.Lerp(0.4f, 1f, fade);

                        Vector3 tail = r.pos - dir * len;
                        Vector3 toCam = camPos - tail;
                        Vector3 side = Vector3.Cross(toCam, dir);
                        if (side.sqrMagnitude < 1e-6f) side = Vector3.Cross(Vector3.up, dir);
                        Vector3 normal = Vector3.Cross(dir, side.normalized);
                        tr.SetPositionAndRotation(tail, Quaternion.LookRotation(dir, normal));
                        tr.localScale = new Vector3(width, 1f, len);
                    }
                }

                if (done && r.tracerIndex >= 0)
                {
                    _tracerObjects[r.tracerIndex].gameObject.SetActive(false);
                    _freeTracers.Push(r.tracerIndex);
                }
                if (done) _rounds.RemoveAt(i);
                else _rounds[i] = r;
            }
        }

        /// <summary>
        /// A real shell airburst (research: flight-sim/veteran accounts, see WEAPON_SYSTEMS.md section 6):
        /// one sharp round flash lasting about a tenth of a second (lighting the haze around it), then a
        /// dark grey-black smoke puff that hangs for several seconds and slowly spreads, sometimes in a
        /// "dumb-bell" of two lobes along the shell path. No falling sparks: that reads as a firework.
        /// </summary>
        /// <param name="metresPerPixel">World size of one screen pixel at the burst distance.</param>
        /// <summary>An airburst at a position (used by the opening cinematic for distant flak).</summary>
        public void SpawnAirburst(Vector3 pos, float distanceFromCamera)
        {
            Camera cam = viewCamera != null ? viewCamera : Camera.main;
            float px = cam != null ? 2f * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) / Mathf.Max(1, Screen.height) : 0.001f;
            SelfDestructBurst(pos, Vector3.zero, distanceFromCamera * px);
        }

        private void SelfDestructBurst(Vector3 pos, Vector3 shellVelocity, float metresPerPixel, float scale = 1f, bool smoke = true)
        {
            if (smoke) GameEvents.RaiseRoundSelfDestructed(pos);
            float R() => (float)_rng.NextDouble();

            if (_burstFire != null)
            {
                // Core: compact white-hot flash, kept readable at range (>= ~10 px).
                float core = Mathf.Max((2.5f + R() * 1.5f) * scale, metresPerPixel * (10f + R() * 4f) * Mathf.Sqrt(scale));
                _burstFire.Emit(new ParticleSystem.EmitParams
                {
                    position = pos, applyShapeToPosition = false,
                    startSize = core,
                    startLifetime = 0.08f + R() * 0.05f,
                    startColor = new Color(1f, 0.95f, 0.85f, 1f),
                }, 1);
                // Halo: the flash lighting the surrounding haze for the same instant (dim, wide).
                _burstFire.Emit(new ParticleSystem.EmitParams
                {
                    position = pos, applyShapeToPosition = false,
                    startSize = core * (3.2f + R()),
                    startLifetime = 0.1f + R() * 0.04f,
                    startColor = new Color(1f, 0.7f, 0.4f, 0.22f),
                }, 1);
            }
            if (_burstSmoke != null && smoke)
            {
                Vector3 along = shellVelocity.sqrMagnitude > 1f ? shellVelocity.normalized : Vector3.forward;
                float puff = Mathf.Max((3.5f + R() * 1.5f) * scale, metresPerPixel * (9f + R() * 4f) * Mathf.Sqrt(scale));
                bool dumbbell = R() < 0.45f;
                int lobes = dumbbell ? 2 : 1;
                for (int k = 0; k < lobes; k++)
                {
                    float offset = dumbbell ? (k == 0 ? -1f : 1f) * puff * 0.28f : 0f;
                    _burstSmoke.Emit(new ParticleSystem.EmitParams
                    {
                        position = pos + along * offset, applyShapeToPosition = false,
                        startSize = puff * (dumbbell ? 0.8f : 1f),
                        startLifetime = 5f + R() * 2f,
                        startColor = new Color(1f, 1f, 1f, 0.8f),
                        rotation = R() * 360f,
                        velocity = along * 2f + Vector3.up * 0.3f, // carries on briefly with the shell momentum
                    }, 1);
                }
            }
        }

        private ParticleSystem MakeBurstSystem(string n, Material mat, bool fire)
        {
            var go = new GameObject(n);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.maxParticles = fire ? 120 : 400;
            var em = ps.emission; em.enabled = false;
            var shape = ps.shape; shape.enabled = false;
            var col = ps.colorOverLifetime; col.enabled = true;
            var size = ps.sizeOverLifetime; size.enabled = true;
            var g = new Gradient();
            if (fire)
            {
                // White-hot to orange, gone almost at once.
                g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.55f, 0.25f), 1f) },
                          new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.6f, 0.4f), new GradientAlphaKey(0f, 1f) });
                size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.75f, 1f, 1.15f));
            }
            else
            {
                // Lit warm by the flash for an instant, then dark grey-black; hangs and slowly spreads.
                g.SetKeys(new[]
                          {
                              new GradientColorKey(new Color(0.9f, 0.62f, 0.4f), 0f),
                              new GradientColorKey(new Color(0.2f, 0.19f, 0.19f), 0.04f),
                              new GradientColorKey(new Color(0.16f, 0.16f, 0.17f), 1f)
                          },
                          new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.85f, 0.02f), new GradientAlphaKey(0.55f, 0.4f), new GradientAlphaKey(0f, 1f) });
                var curve = new AnimationCurve(new Keyframe(0f, 0.45f), new Keyframe(0.06f, 1f), new Keyframe(1f, 2.1f));
                size.size = new ParticleSystem.MinMaxCurve(1f, curve);
                var vel = ps.limitVelocityOverLifetime; vel.enabled = true; vel.limit = 0.5f; vel.dampen = 0.35f;
            }
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            ps.Play();
            return ps;
        }

        /// <summary>Unit ribbon along +Z (0..1), width along X, facing +Y; white vertex colours.</summary>
        private static Mesh BuildRibbon()
        {
            var m = new Mesh { name = "TracerRibbon" };
            m.vertices = new[] { new Vector3(-0.5f, 0, 0), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 0, 1), new Vector3(-0.5f, 0, 1) };
            m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            m.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.RecalculateNormals();
            m.bounds = new Bounds(new Vector3(0, 0, 0.5f), new Vector3(1, 1, 1));
            return m;
        }

        private int AcquireTracer()
        {
            if (_freeTracers.Count > 0) return _freeTracers.Pop(); // stays hidden until it clears the muzzle
            var go = new GameObject("Tracer");
            go.transform.SetParent(_tracerRoot, false);
            go.AddComponent<MeshFilter>().sharedMesh = _ribbon;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = visuals.tracer;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            go.SetActive(false);
            _tracerObjects.Add(go.transform);
            return _tracerObjects.Count - 1;
        }
    }
}
