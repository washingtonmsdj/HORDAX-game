using System.Collections.Generic;
using UnityEngine;

namespace HORDAX.Combat
{
    public abstract class ShootableTarget : MonoBehaviour
    {
        private static readonly List<ShootableTarget> Active = new List<ShootableTarget>();

        public static IReadOnlyList<ShootableTarget> ActiveTargets => Active;
        public virtual Vector3 TargetPoint => transform.position + Vector3.up * 0.5f;
        public virtual bool CanBeTargeted => isActiveAndEnabled;

        protected virtual void OnEnable()
        {
            if (!Active.Contains(this)) Active.Add(this);
        }

        protected virtual void OnDisable()
        {
            Active.Remove(this);
        }

        public abstract void TakeDamage(float amount);
    }
}
