using System.Collections.Generic;
using UnityEngine;
using HORDAX.Core;
using HORDAX.Prototype;

namespace HORDAX.Combat
{
    public sealed class WeaponController : MonoBehaviour
    {
        [Header("Weapon")]
        [SerializeField] private float damage = 5f;
        [SerializeField] private float fireRate = 12f;
        [SerializeField] private float range = 34f;
        [SerializeField] private float bulletSpeed = 45f;
        [SerializeField] private Transform muzzle;
        [SerializeField] private GameObject bulletPrefab;

        private readonly Stack<Bullet> bulletPool = new Stack<Bullet>();
        private float shotTimer;

        public float Damage => damage;
        public float FireRate => fireRate;

        public void SetMuzzle(Transform value) => muzzle = value;

        public void ApplyUpgrade(float damageAdd, float fireRateMultiplier)
        {
            damage += damageAdd;
            fireRate = Mathf.Clamp(fireRate * fireRateMultiplier, 1f, 30f);
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

            shotTimer -= Time.deltaTime;
            if (shotTimer > 0f) return;

            ShootableTarget target = SelectTarget();
            if (target == null) return;

            Fire(target);
            shotTimer = 1f / Mathf.Max(0.01f, fireRate);
        }

        private ShootableTarget SelectTarget()
        {
            ShootableTarget best = null;
            float bestScore = float.MaxValue;
            IReadOnlyList<ShootableTarget> targets = ShootableTarget.ActiveTargets;

            for (int i = targets.Count - 1; i >= 0; i--)
            {
                ShootableTarget candidate = targets[i];
                if (candidate == null || !candidate.CanBeTargeted) continue;

                Vector3 offset = candidate.TargetPoint - transform.position;
                if (offset.z < -0.5f || offset.z > range) continue;
                if (Mathf.Abs(offset.x) > 8f) continue;

                float score = offset.z * offset.z + offset.x * offset.x * 1.75f;
                if (score >= bestScore) continue;

                best = candidate;
                bestScore = score;
            }

            return best;
        }

        private void Fire(ShootableTarget target)
        {
            Bullet bullet = AcquireBullet();
            Transform origin = muzzle != null ? muzzle : transform;
            bullet.transform.position = origin.position;
            bullet.transform.rotation = origin.rotation;
            bullet.Initialize(target, damage, bulletSpeed, RecycleBullet);
        }

        private Bullet AcquireBullet()
        {
            if (bulletPool.Count > 0) return bulletPool.Pop();

            GameObject instance;
            if (bulletPrefab != null)
            {
                instance = Instantiate(bulletPrefab);
            }
            else
            {
                instance = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                instance.name = "Prototype Bullet";
                instance.transform.localScale = Vector3.one * 0.18f;
                Collider collider = instance.GetComponent<Collider>();
                if (collider != null) Destroy(collider);
                Renderer renderer = instance.GetComponent<Renderer>();
                if (renderer != null) renderer.sharedMaterial = PrototypeMaterials.Bullet;
            }

            Bullet bullet = instance.GetComponent<Bullet>();
            if (bullet == null) bullet = instance.AddComponent<Bullet>();
            return bullet;
        }

        private void RecycleBullet(Bullet bullet)
        {
            if (bullet == null) return;
            bullet.gameObject.SetActive(false);
            bulletPool.Push(bullet);
        }
    }
}
