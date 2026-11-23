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
        public float minPixelWidth = 3.5f;
        [Tooltip("Tracers appear once the round is this far from the muzzle (they ignite almost immediately).")]
        public float tracerHiddenDistance = 3f;

        [Header("Self-destruct burst (optional)")]
        public Material burstFireMaterial;   // additive fireball sheet
        public Material burstSmokeMaterial;  // alpha soft dot

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
        public void Fire(Vector3 origin, Vector3 direction, float speed, float dragK, float life, float tracerBurn, bool selfDestruct, float damage)
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
                    if (r.selfDestruct) SelfDestructBurst(r.pos, Vector3.Distance(camPos, r.pos) * pxAngle);
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
                            RoundsHit++;
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
                        len = Mathf.Max(len, width * 3f);
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

        /// <param name="metresPerPixel">World size of one screen pixel at the burst's distance.</param>
        private void SelfDestructBurst(Vector3 pos, float metresPerPixel)
        {
            if (_burstFire != null)
            {
                // A bright flash in a dark sky reads as a spark even when tiny: keep >= ~12 px on screen.
                float size = Mathf.Max(2.2f + (float)_rng.NextDouble() * 1.5f, metresPerPixel * (10f + (float)_rng.NextDouble() * 6f));
                _burstFire.Emit(new ParticleSystem.EmitParams
                {
                    position = pos, applyShapeToPosition = false,
                    startSize = size,
                    startLifetime = 0.1f + (float)_rng.NextDouble() * 0.08f,
                    startColor = new Color(1f, 0.75f, 0.45f, 1f),
                    rotation = (float)_rng.NextDouble() * 360f,
                }, 1);
            }
            if (_burstSmoke != null)
            {
                _burstSmoke.Emit(new ParticleSystem.EmitParams
                {
                    position = pos, applyShapeToPosition = false,
                    startSize = Mathf.Max(3f + (float)_rng.NextDouble() * 2f, metresPerPixel * 14f),
                    startLifetime = 2.5f + (float)_rng.NextDouble() * 1.5f,
                    startColor = new Color(0.45f, 0.45f, 0.45f, 0.45f),
                    rotation = (float)_rng.NextDouble() * 360f,
                    velocity = Vector3.up * 0.5f,
                }, 1);
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
            main.maxParticles = fire ? 80 : 160;
            var em = ps.emission; em.enabled = false;
            var shape = ps.shape; shape.enabled = false;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            if (fire)
            {
                var sheet = ps.textureSheetAnimation;
                sheet.enabled = true; sheet.numTilesX = 2; sheet.numTilesY = 2;
                sheet.frameOverTime = new ParticleSystem.MinMaxCurve(0f);
                sheet.startFrame = new ParticleSystem.MinMaxCurve(0f, 3.999f);
                g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.5f, 0.2f), 1f) },
                          new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            }
            else
            {
                g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                          new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0f, 1f) });
                var size = ps.sizeOverLifetime; size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 1.6f));
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
