using AirDefense.Core;
using AirDefense.Data;
using UnityEngine;

namespace AirDefense.Threats
{
    /// <summary>
    /// One incoming threat. Flies a simple scripted path (no flight physics,
    /// TECHNICAL_ARCHITECTURE.md §4) toward a target point on the installation.
    /// </summary>
    public sealed class ThreatAgent : MonoBehaviour
    {
        public ThreatDefinition Definition { get; private set; }
        public bool Alive { get; private set; }
        public Vector3 Velocity { get; private set; }
        public float Health { get; private set; }
        public int Id { get; private set; }
        /// <summary>In its terminal dive onto the target (radar shows it as critical).</summary>
        public bool Diving => _diving;

        private Vector3 _target;
        private float _age;
        private float _weavePhase;
        private bool _diving;
        private ThreatManager _owner;

        private const float ImpactDistance = 6f;

        public void Launch(ThreatManager owner, int id, ThreatDefinition def, Vector3 spawn, Vector3 target, float weavePhase)
        {
            _owner = owner;
            Id = id;
            Definition = def;
            _target = target;
            _weavePhase = weavePhase;
            _age = 0f;
            _diving = false;
            Health = def.health;
            Alive = true;
            transform.position = spawn;
            Vector3 toTarget = Flat(target - spawn).normalized;
            Velocity = toTarget * def.speed;
            transform.rotation = Quaternion.LookRotation(toTarget);
            gameObject.SetActive(true);
        }

        public void ApplyDamage(float amount)
        {
            if (!Alive) return;
            Health -= amount;
            if (Health <= 0f)
            {
                Alive = false;
                _owner.OnThreatDestroyed(this);
            }
        }

        private void Update()
        {
            if (!Alive) return;
            float dt = Time.deltaTime;
            _age += dt;

            Vector3 pos = transform.position;
            Vector3 flatToTarget = Flat(_target - pos);
            float flatDist = flatToTarget.magnitude;
            Vector3 desiredDir;

            if (!_diving && flatDist <= Definition.diveDistance) _diving = true;

            if (_diving)
            {
                desiredDir = (_target - pos).normalized;
            }
            else
            {
                Vector3 fwd = flatToTarget / Mathf.Max(0.001f, flatDist);
                Vector3 side = Vector3.Cross(Vector3.up, fwd);
                float altErr = Definition.cruiseAltitude - pos.y;
                Vector3 climb = Vector3.up * Mathf.Clamp(altErr * 0.05f, -0.5f, 0.5f);
                float weave = 0f;
                if (Definition.movement == ThreatMovement.WeaveAndDive)
                {
                    // Derivative of a sine path gives the sideways heading component.
                    float w = 2f * Mathf.PI * Definition.weaveFrequency;
                    weave = Mathf.Cos(_age * w + _weavePhase) * Definition.weaveAmplitude * w / Mathf.Max(1f, Definition.speed);
                }
                desiredDir = (fwd + side * weave + climb).normalized;
            }

            float turnRate = _diving ? 4f : 2f;
            Vector3 dir = Vector3.Slerp(Velocity.normalized, desiredDir, 1f - Mathf.Exp(-turnRate * dt));
            float speed = Definition.speed * (_diving ? 1.3f : 1f);
            Velocity = dir * speed;
            pos += Velocity * dt;
            transform.SetPositionAndRotation(pos, Quaternion.LookRotation(Velocity));

            if ((pos - _target).sqrMagnitude <= ImpactDistance * ImpactDistance || pos.y <= 0.5f)
            {
                Alive = false;
                _owner.OnThreatImpacted(this);
            }
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
