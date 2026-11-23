using System.Collections.Generic;
using AirDefense.Core;
using AirDefense.Threats;
using UnityEngine;

namespace AirDefense.Weapons
{
    /// <summary>
    /// Pooled gun rounds. Each round is a point integrated with gravity; hits are
    /// segment-vs-sphere tests against live threats (no physics colliders), which
    /// is cheap and reliable at high projectile speed. Every Nth round is a tracer:
    /// a glowing streak (bright head, fading tail) drawn as a camera-facing ribbon
    /// along the round's velocity, like a tracer seen with motion blur.
    /// </summary>
    public sealed class ProjectileSystem : MonoBehaviour
    {
        public PrototypeVisuals visuals;
        public ThreatManager threats;
        [Tooltip("Streak length, metres (about the distance a 900 m/s round covers in one 60 fps frame).")]
        public float tracerLength = 15f;
        [Tooltip("Streak width including its glow, metres.")]
        public float tracerWidth = 0.38f;
        [Tooltip("Tracers appear once they have left the muzzle by this distance.")]
        public float tracerHiddenDistance = 4f;
        [Tooltip("Camera the streaks face. Defaults to the main camera.")]
        public Camera viewCamera;

        private static Mesh _ribbon;

        private struct Round
        {
            public Vector3 pos;
            public Vector3 vel;
            public float life;
            public float damage;
            public float travelled;
            public int tracerIndex; // -1 = no tracer
        }

        private readonly List<Round> _rounds = new List<Round>(512);
        private readonly Stack<int> _freeTracers = new Stack<int>();
        private readonly List<Transform> _tracerObjects = new List<Transform>();
        private Transform _tracerRoot;

        public int RoundsHit { get; private set; }
        public int LiveRounds => _rounds.Count;

        private void Awake()
        {
            _tracerRoot = new GameObject("Tracers").transform;
            _tracerRoot.SetParent(transform, false);
            _ribbon ??= BuildRibbon();
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

        public void Fire(Vector3 origin, Vector3 direction, float speed, float lifetime, float damage, bool tracer)
        {
            var r = new Round
            {
                pos = origin,
                vel = direction.normalized * speed,
                life = lifetime,
                damage = damage,
                tracerIndex = tracer ? AcquireTracer() : -1
            };
            _rounds.Add(r);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            var live = threats.Active;
            Camera cam = viewCamera != null ? viewCamera : Camera.main;

            for (int i = _rounds.Count - 1; i >= 0; i--)
            {
                Round r = _rounds[i];
                Vector3 p0 = r.pos;
                r.vel += Ballistics.Gravity * dt;
                r.pos += r.vel * dt;
                r.travelled += r.vel.magnitude * dt;
                r.life -= dt;

                bool done = r.life <= 0f || r.pos.y < 0f;

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
                    if (done)
                    {
                        tr.gameObject.SetActive(false);
                        _freeTracers.Push(r.tracerIndex);
                    }
                    else if (r.travelled >= tracerHiddenDistance)
                    {
                        if (!tr.gameObject.activeSelf) tr.gameObject.SetActive(true);
                        Vector3 dir = r.vel.normalized;
                        // Streak ends at the round (head) and trails behind it; shorter just after the muzzle.
                        float len = Mathf.Min(tracerLength, r.travelled);
                        Vector3 tail = r.pos - dir * len;
                        // Cylindrical billboard: the ribbon turns about its own axis to face the camera.
                        Vector3 toCam = (cam != null ? cam.transform.position : tail - dir) - tail;
                        Vector3 side = Vector3.Cross(toCam, dir);
                        if (side.sqrMagnitude < 1e-6f) side = Vector3.Cross(Vector3.up, dir);
                        Vector3 normal = Vector3.Cross(dir, side.normalized);
                        tr.SetPositionAndRotation(tail, Quaternion.LookRotation(dir, normal));
                        tr.localScale = new Vector3(tracerWidth, 1f, len);
                    }
                }

                if (done) _rounds.RemoveAt(i);
                else _rounds[i] = r;
            }
        }

        private int AcquireTracer()
        {
            if (_freeTracers.Count > 0)
            {
                int free = _freeTracers.Pop();
                return free; // stays hidden until it has travelled tracerHiddenDistance
            }
            else
            {
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
}
