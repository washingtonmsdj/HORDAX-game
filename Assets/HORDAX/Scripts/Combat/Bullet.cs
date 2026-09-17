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
        private Action<Bullet> recycle;

        public void Initialize(ShootableTarget newTarget, float newDamage, float newSpeed, Action<Bullet> recycleAction)
        {
            target = newTarget;
            damage = newDamage;
            speed = newSpeed;
            recycle = recycleAction;
            age = 0f;
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

            Vector3 toTarget = target.TargetPoint - transform.position;
            float step = speed * Time.deltaTime;

            if (toTarget.sqrMagnitude <= step * step || toTarget.sqrMagnitude < 0.20f)
            {
                target.TakeDamage(damage);
                Recycle();
                return;
            }

            transform.position += toTarget.normalized * step;
            transform.forward = toTarget.normalized;
        }

        private void Recycle()
        {
            target = null;
            recycle?.Invoke(this);
        }
    }
}
