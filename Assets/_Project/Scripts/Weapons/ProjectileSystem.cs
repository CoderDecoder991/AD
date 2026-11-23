using System.Collections.Generic;
using AirDefense.Core;
using AirDefense.Threats;
using UnityEngine;

namespace AirDefense.Weapons
{
    /// <summary>
    /// Pooled gun rounds. Each round is a point integrated with gravity; hits are
    /// segment-vs-sphere tests against live threats (no physics colliders), which
    /// is cheap and reliable at high projectile speed. Tracers are drawn for every
    /// Nth round with pooled stretched quads.
    /// </summary>
    public sealed class ProjectileSystem : MonoBehaviour
    {
        public PrototypeVisuals visuals;
        public ThreatManager threats;
        [Tooltip("Visual tracer length, metres.")]
        public float tracerLength = 14f;
        public float tracerWidth = 0.22f;
        [Tooltip("Tracers stay hidden until this far from the muzzle. The camera sits right behind the gun, so a nearby tracer would fill the screen.")]
        public float tracerHiddenDistance = 40f;

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
                        tr.SetPositionAndRotation(r.pos - dir * (tracerLength * 0.5f), Quaternion.LookRotation(dir));
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
                var go = visuals.Part("Tracer", visuals.cube, visuals.tracer, _tracerRoot,
                    Vector3.zero, new Vector3(tracerWidth, tracerWidth, tracerLength));
                go.SetActive(false);
                _tracerObjects.Add(go.transform);
                return _tracerObjects.Count - 1;
            }
        }
    }
}
