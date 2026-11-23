using System.Collections.Generic;
using AirDefense.Core;
using AirDefense.Data;
using UnityEngine;

namespace AirDefense.Threats
{
    /// <summary>
    /// Spawns, pools and tracks threats; owns their destruction/impact effects.
    /// Threat bodies are PLACEHOLDER greybox shapes (C-14) built from primitives.
    /// </summary>
    public sealed class ThreatManager : MonoBehaviour
    {
        public PrototypeVisuals visuals;
        [Tooltip("Centre of the defended installation.")]
        public Transform installationCenter;
        [Tooltip("Points threats aim for (buildings). Falls back to the centre.")]
        public Transform[] targetPoints;

        public IReadOnlyList<ThreatAgent> Active => _active;
        public int SpawnedCount { get; private set; }

        private readonly List<ThreatAgent> _active = new List<ThreatAgent>(64);
        private readonly Dictionary<ThreatDefinition, Stack<ThreatAgent>> _pools = new Dictionary<ThreatDefinition, Stack<ThreatAgent>>();
        private readonly List<Effect> _effects = new List<Effect>(32);
        private readonly Stack<Transform> _freeEffects = new Stack<Transform>();
        private System.Random _rng = new System.Random(1);
        private int _nextId;

        private struct Effect
        {
            public Transform t;
            public float age;
            public float duration;
            public float maxScale;
        }

        public void SetSeed(int seed) => _rng = new System.Random(seed);

        public ThreatAgent Spawn(SpawnGroup group)
        {
            ThreatDefinition def = group.threat;
            float bearing = Lerp(group.bearingRange.x, group.bearingRange.y);
            float dist = Lerp(group.distanceRange.x, group.distanceRange.y);
            Vector3 c = installationCenter.position;
            Vector3 dir = Quaternion.Euler(0f, bearing, 0f) * Vector3.forward;
            Vector3 spawn = c + dir * dist + Vector3.up * def.cruiseAltitude;

            Vector3 target = c;
            if (targetPoints != null && targetPoints.Length > 0)
                target = targetPoints[_rng.Next(targetPoints.Length)].position;

            ThreatAgent agent = Acquire(def);
            agent.Launch(this, _nextId++, def, spawn, target, (float)(_rng.NextDouble() * Mathf.PI * 2.0));
            _active.Add(agent);
            SpawnedCount++;
            GameEvents.RaiseThreatSpawned(agent);
            return agent;
        }

        internal void OnThreatDestroyed(ThreatAgent t)
        {
            SpawnEffect(t.transform.position, 0.7f, 9f * t.Definition.visualScale);
            Retire(t);
            GameEvents.RaiseThreatDestroyed(t);
        }

        internal void OnThreatImpacted(ThreatAgent t)
        {
            SpawnEffect(t.transform.position, 1.2f, 22f);
            Retire(t);
            GameEvents.RaiseThreatImpacted(t);
        }

        /// <summary>Removes every live threat without effects (mission reset).</summary>
        public void ClearAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--) Retire(_active[i]);
        }

        private void Retire(ThreatAgent t)
        {
            _active.Remove(t);
            t.gameObject.SetActive(false);
            _pools[t.Definition].Push(t);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = _effects.Count - 1; i >= 0; i--)
            {
                Effect e = _effects[i];
                e.age += dt;
                float k = e.age / e.duration;
                if (k >= 1f)
                {
                    e.t.gameObject.SetActive(false);
                    _freeEffects.Push(e.t);
                    _effects.RemoveAt(i);
                    continue;
                }
                // Quick swell, slow fade-shrink.
                float s = k < 0.2f ? Mathf.Lerp(0.2f, 1f, k / 0.2f) : Mathf.Lerp(1f, 0.05f, (k - 0.2f) / 0.8f);
                e.t.localScale = Vector3.one * (e.maxScale * s);
                _effects[i] = e;
            }
        }

        private void SpawnEffect(Vector3 pos, float duration, float maxScale)
        {
            Transform t;
            if (_freeEffects.Count > 0) t = _freeEffects.Pop();
            else
            {
                t = visuals.Part("Explosion (placeholder)", visuals.sphere, visuals.explosion, transform,
                    Vector3.zero, Vector3.one).transform;
            }
            t.position = pos;
            t.localScale = Vector3.one * 0.1f;
            t.gameObject.SetActive(true);
            _effects.Add(new Effect { t = t, age = 0f, duration = duration, maxScale = maxScale });
        }

        private ThreatAgent Acquire(ThreatDefinition def)
        {
            if (!_pools.TryGetValue(def, out var pool))
            {
                pool = new Stack<ThreatAgent>();
                _pools[def] = pool;
            }
            if (pool.Count > 0) return pool.Pop();

            var root = new GameObject($"Threat_{def.threatId}");
            root.transform.SetParent(transform, false);
            BuildPlaceholderBody(root.transform, def);
            return root.AddComponent<ThreatAgent>();
        }

        private void BuildPlaceholderBody(Transform root, ThreatDefinition def)
        {
            float s = def.visualScale;
            if (visuals.navLight != null)
            {
                // Blinking red navigation light: keeps threats readable against the night sky.
                var nav = visuals.Part("NavLight", visuals.sphere, visuals.navLight, root, new Vector3(0f, 0.35f * s, 0f), Vector3.one * 0.55f * s);
                nav.AddComponent<Environment.NavLightBlink>();
            }
            switch (def.threatClass)
            {
                case ThreatClass.SmallDrone:
                    visuals.Part("Body", visuals.cube, visuals.threatBody, root, Vector3.zero, new Vector3(0.9f, 0.3f, 0.9f) * s);
                    for (int i = 0; i < 4; i++)
                    {
                        float x = (i % 2 == 0 ? -0.75f : 0.75f) * s;
                        float z = (i < 2 ? -0.75f : 0.75f) * s;
                        visuals.Part("Rotor", visuals.cylinder, visuals.threatAccent, root, new Vector3(x, 0.15f * s, z), new Vector3(0.7f, 0.03f, 0.7f) * s);
                    }
                    break;

                default: // fixed-wing style body
                    visuals.Part("Fuselage", visuals.capsule, visuals.threatBody, root, Vector3.zero, new Vector3(0.6f, 2.2f, 0.6f) * s, Quaternion.Euler(90f, 0f, 0f));
                    visuals.Part("Wing", visuals.cube, visuals.threatAccent, root, new Vector3(0f, 0f, 0.3f * s), new Vector3(5.5f, 0.08f, 0.9f) * s);
                    visuals.Part("Tail", visuals.cube, visuals.threatAccent, root, new Vector3(0f, 0.35f * s, -1.9f * s), new Vector3(1.6f, 0.06f, 0.5f) * s);
                    break;
            }
        }

        private float Lerp(float a, float b) => Mathf.Lerp(a, b, (float)_rng.NextDouble());
    }
}
