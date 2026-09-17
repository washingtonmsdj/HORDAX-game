using System;
using UnityEngine;

namespace HORDAX.Combat
{
    public sealed class Bullet : MonoBehaviour
    {
        private ShootableTarget target;
        private float damage;
        private float speed;
        private float age;
        private float impactScale;
        private Vector3 aimOffset;
        private Action<Bullet> recycle;
        private Vector3 baseScale;
        private bool hasBaseScale;

        public void Initialize(ShootableTarget newTarget, float newDamage, float newSpeed, Vector3 newAimOffset, float scaleMultiplier, Action<Bullet> recycleAction)
        {
            target = newTarget;
            damage = newDamage;
            speed = newSpeed;
            aimOffset = newAimOffset;
            recycle = recycleAction;
            age = 0f;

            if (!hasBaseScale)
            {
                baseScale = transform.localScale;
                hasBaseScale = true;
            }

            float safeScale = Mathf.Max(0.1f, scaleMultiplier);
            transform.localScale = baseScale * safeScale;
            impactScale = 0.22f * safeScale;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            age += Time.deltaTime;

            if (target == null || !target.CanBeTargeted || age > 4f)
            {
                Recycle();
                return;
            }

            Vector3 targetPoint = target.TargetPoint + aimOffset;
            Vector3 toTarget = targetPoint - transform.position;
            float step = speed * Time.deltaTime;

            if (toTarget.sqrMagnitude <= step * step || toTarget.sqrMagnitude < 0.20f)
            {
                CombatFxPool.Instance?.PlayImpact(targetPoint, impactScale);
                target.TakeDamage(damage);
                Recycle();
                return;
            }

            Vector3 direction = toTarget.normalized;
            transform.position += direction * step;
            if (direction.sqrMagnitude > 0.001f) transform.forward = direction;
        }

        private void Recycle()
        {
            target = null;
            aimOffset = Vector3.zero;
            recycle?.Invoke(this);
        }
    }
}
