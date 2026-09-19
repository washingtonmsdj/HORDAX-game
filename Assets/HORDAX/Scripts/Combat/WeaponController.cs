using System;
using System.Collections.Generic;
using UnityEngine;
using HORDAX.Core;
using HORDAX.Data;
using HORDAX.Enemies;
using HORDAX.Prototype;
using HORDAX.World;

namespace HORDAX.Combat
{
    public sealed class WeaponController : MonoBehaviour
    {
        [Header("Weapon")]
        [SerializeField] private WeaponData weaponData;
        [SerializeField] private WeaponArchetype archetype = WeaponArchetype.Rifle;
        [SerializeField] private WeaponRarity rarity = WeaponRarity.Common;
        [SerializeField] private string displayName = "Rifle";
        [SerializeField] private float damage = 5f;
        [SerializeField] private float fireRate = 12f;
        [SerializeField] private float range = 34f;
        [SerializeField] private float bulletSpeed = 45f;
        [SerializeField] private int projectilesPerShot = 1;
        [SerializeField] private float spreadDegrees = 0.4f;
        [SerializeField] private float recoilKick = 0.05f;
        [SerializeField] private float projectileScale = 1f;
        [SerializeField] private Transform muzzle;
        [SerializeField] private GameObject bulletPrefab;

        private readonly Stack<Bullet> prototypeBulletPool = new Stack<Bullet>();
        private readonly Dictionary<GameObject, Stack<Bullet>> prefabBulletPools = new Dictionary<GameObject, Stack<Bullet>>();
        private readonly Dictionary<Bullet, GameObject> sourcePrefabByBullet = new Dictionary<Bullet, GameObject>();
        private readonly Dictionary<ShootableTarget, float> reservedDamageByTarget = new Dictionary<ShootableTarget, float>();
        private float shotTimer;
        private float flashTimer;
        private GameObject muzzleFlash;
        private int upgradeLevel = 1;
        private float permanentDamageMultiplier = 1f;
        private float permanentFireRateMultiplier = 1f;
        private ShootableTarget currentTarget;

        public event Action ShotFired;
        public event Action WeaponChanged;

        public string DisplayName => displayName;
        public WeaponArchetype Archetype => archetype;
        public WeaponRarity Rarity => rarity;
        public float Damage => damage;
        public float FireRate => fireRate;
        public float Range => range;
        public int ProjectilesPerShot => projectilesPerShot;
        public float SpreadDegrees => spreadDegrees;
        public float RecoilKick => recoilKick;
        public int UpgradeLevel => upgradeLevel;
        public int ReservedTargetCount => reservedDamageByTarget.Count;
        public float ReservedDamageTotal
        {
            get
            {
                float total = 0f;
                foreach (KeyValuePair<ShootableTarget, float> pair in reservedDamageByTarget)
                    total += Mathf.Max(0f, pair.Value);
                return total;
            }
        }
        public WeaponData Definition => weaponData;

        public bool TryGetAimPoint(out Vector3 point)
        {
            if (currentTarget != null && currentTarget.CanBeTargeted)
            {
                point = currentTarget.TargetPoint;
                return true;
            }

            point = Vector3.zero;
            return false;
        }

        public void SetMuzzle(Transform value) => muzzle = value;

        public void ApplyDefinition(WeaponData definition)
        {
            if (definition == null) return;

            weaponData = definition;
            archetype = definition.Archetype;
            rarity = definition.Rarity;
            displayName = string.IsNullOrWhiteSpace(definition.DisplayName) ? definition.Archetype.ToString() : definition.DisplayName;
            damage = definition.Damage * GetRarityDamageMultiplier(rarity);
            fireRate = definition.FireRate;
            range = definition.Range;
            bulletSpeed = definition.BulletSpeed;
            projectilesPerShot = Mathf.Max(1, definition.ProjectilesPerShot);
            spreadDegrees = Mathf.Max(0f, definition.SpreadDegrees);
            recoilKick = Mathf.Max(0f, definition.RecoilKick);
            projectileScale = Mathf.Max(0.1f, definition.ProjectileScale);
            bulletPrefab = definition.BulletPrefab;

            ApplyModifiers(definition.Modifiers);

            damage = Mathf.Max(0.1f, damage);
            fireRate = Mathf.Clamp(fireRate, 0.1f, 32f);
            range = Mathf.Max(1f, range);
            projectilesPerShot = Mathf.Clamp(projectilesPerShot, 1, 16);
            spreadDegrees = Mathf.Max(0f, spreadDegrees);
            ApplyPersistentBonusesToCurrentStats();

            upgradeLevel = 1;
            shotTimer = 0f;
            WeaponChanged?.Invoke();
        }

        public void ApplyPrototype(WeaponArchetype type)
        {
            weaponData = null;
            archetype = type;
            rarity = WeaponRarity.Common;
            bulletPrefab = null;
            upgradeLevel = 1;
            shotTimer = 0f;

            switch (type)
            {
                case WeaponArchetype.SMG:
                    displayName = "SMG";
                    damage = 4f;
                    fireRate = 18f;
                    range = 31f;
                    bulletSpeed = 52f;
                    projectilesPerShot = 1;
                    spreadDegrees = 1.2f;
                    recoilKick = 0.035f;
                    projectileScale = 0.85f;
                    break;
                case WeaponArchetype.Shotgun:
                    displayName = "SHOTGUN";
                    damage = 3.5f;
                    fireRate = 4.2f;
                    range = 24f;
                    bulletSpeed = 42f;
                    projectilesPerShot = 5;
                    spreadDegrees = 5.5f;
                    recoilKick = 0.13f;
                    projectileScale = 1.05f;
                    break;
                case WeaponArchetype.Minigun:
                    displayName = "MINIGUN";
                    damage = 4.5f;
                    fireRate = 24f;
                    range = 38f;
                    bulletSpeed = 58f;
                    projectilesPerShot = 1;
                    spreadDegrees = 1.5f;
                    recoilKick = 0.045f;
                    projectileScale = 0.8f;
                    break;
                default:
                    displayName = "RIFLE";
                    damage = 5f;
                    fireRate = 12f;
                    range = 34f;
                    bulletSpeed = 45f;
                    projectilesPerShot = 1;
                    spreadDegrees = 0.4f;
                    recoilKick = 0.055f;
                    projectileScale = 1f;
                    break;
            }

            ApplyPersistentBonusesToCurrentStats();
            WeaponChanged?.Invoke();
        }

        public void SetPermanentBonuses(float damageMultiplier, float fireRateMultiplier)
        {
            damage /= Mathf.Max(0.01f, permanentDamageMultiplier);
            fireRate /= Mathf.Max(0.01f, permanentFireRateMultiplier);

            permanentDamageMultiplier = Mathf.Max(0.01f, damageMultiplier);
            permanentFireRateMultiplier = Mathf.Max(0.01f, fireRateMultiplier);
            ApplyPersistentBonusesToCurrentStats();
            WeaponChanged?.Invoke();
        }

        private void ApplyPersistentBonusesToCurrentStats()
        {
            damage *= permanentDamageMultiplier;
            fireRate = Mathf.Clamp(fireRate * permanentFireRateMultiplier, 0.1f, 32f);
        }

        public void ApplyUpgrade(float damageAdd, float fireRateMultiplier)
        {
            damage += damageAdd;
            fireRate = Mathf.Clamp(fireRate * fireRateMultiplier, 1f, 32f);
            upgradeLevel++;
            WeaponChanged?.Invoke();
        }

        private void ApplyModifiers(IReadOnlyList<WeaponModifierData> modifiers)
        {
            if (modifiers == null) return;

            for (int i = 0; i < modifiers.Count; i++)
            {
                WeaponModifierData modifier = modifiers[i];
                if (modifier == null) continue;

                damage += modifier.DamageAdd;
                damage *= Mathf.Max(0.01f, modifier.DamageMultiplier);
                fireRate *= Mathf.Max(0.01f, modifier.FireRateMultiplier);
                range *= Mathf.Max(0.01f, modifier.RangeMultiplier);
                projectilesPerShot += modifier.BonusProjectiles;
                spreadDegrees *= Mathf.Max(0.01f, modifier.SpreadMultiplier);
            }
        }

        private static float GetRarityDamageMultiplier(WeaponRarity value)
        {
            switch (value)
            {
                case WeaponRarity.Uncommon: return 1.08f;
                case WeaponRarity.Rare: return 1.18f;
                case WeaponRarity.Epic: return 1.32f;
                case WeaponRarity.Legendary: return 1.50f;
                default: return 1f;
            }
        }

        private void Start()
        {
            if (weaponData != null) ApplyDefinition(weaponData);
            BuildPrototypeMuzzleFlash();
        }

        private void Update()
        {
            if (flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
                if (flashTimer <= 0f && muzzleFlash != null) muzzleFlash.SetActive(false);
            }

            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
            {
                currentTarget = null;
                return;
            }

            currentTarget = SelectTarget();

            shotTimer -= Time.deltaTime;
            if (currentTarget == null)
            {
                // Cooldown may recover while no target is available, but never build
                // an artificial backlog that would burst-fire when a target appears.
                shotTimer = Mathf.Max(0f, shotTimer);
                return;
            }

            if (shotTimer > 0f) return;

            float interval = 1f / Mathf.Max(0.01f, fireRate);
            int shotsThisFrame = 0;
            const int maxCatchUpShotsPerFrame = 4;

            // Preserve timer overshoot so low frame rates and accelerated automation
            // still deliver the configured rounds-per-second instead of silently
            // reducing weapon DPS.
            while (shotTimer <= 0f && shotsThisFrame < maxCatchUpShotsPerFrame)
            {
                Fire(currentTarget);
                shotTimer += interval;
                shotsThisFrame++;
            }

            if (shotsThisFrame == maxCatchUpShotsPerFrame && shotTimer < -interval)
                shotTimer = 0f;
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

                float estimatedHealth = GetEstimatedHealth(candidate);
                if (!float.IsPositiveInfinity(estimatedHealth))
                {
                    float reserved = GetReservedDamage(candidate);
                    if (reserved >= estimatedHealth - 0.001f)
                        continue;
                }

                // HORDAX uses adjacent arsenal/horde lanes, so lateral distance must not
                // overpower breach urgency. Forward distance remains the main threat score.
                float score = offset.z * offset.z + offset.x * offset.x * 0.35f;
                if (score >= bestScore) continue;

                best = candidate;
                bestScore = score;
            }

            return best;
        }

        private void Fire(ShootableTarget target)
        {
            Transform origin = muzzle != null ? muzzle : transform;
            int projectileCount = Mathf.Max(1, projectilesPerShot);

            for (int i = 0; i < projectileCount; i++)
            {
                Bullet bullet = AcquireBullet();
                Vector3 aimOffset = CalculateAimOffset(origin.position, target.TargetPoint, projectileCount);
                Vector3 direction = target.TargetPoint + aimOffset - origin.position;

                bullet.transform.position = origin.position;
                if (direction.sqrMagnitude > 0.001f)
                    bullet.transform.rotation = Quaternion.LookRotation(direction.normalized);

                ReserveDamage(target, damage);
                bullet.Initialize(target, damage, bulletSpeed, aimOffset, projectileScale, RecycleBullet);
            }

            if (muzzleFlash != null)
            {
                muzzleFlash.SetActive(false);
                float flashSize = Mathf.Lerp(0.18f, 0.42f, Mathf.Clamp01(recoilKick / 0.14f));
                muzzleFlash.transform.localScale = Vector3.one * UnityEngine.Random.Range(flashSize * 0.8f, flashSize * 1.15f);
                muzzleFlash.SetActive(true);
                flashTimer = 0.045f;
            }

            ShotFired?.Invoke();
        }

        private Vector3 CalculateAimOffset(Vector3 origin, Vector3 targetPoint, int projectileCount)
        {
            if (spreadDegrees <= 0f) return Vector3.zero;
            if (projectileCount == 1 && spreadDegrees < 0.75f) return Vector3.zero;

            float distance = Mathf.Max(1f, Vector3.Distance(origin, targetPoint));
            float radius = Mathf.Tan(spreadDegrees * Mathf.Deg2Rad) * distance;
            Vector2 random = UnityEngine.Random.insideUnitCircle * radius;
            return new Vector3(random.x, random.y * 0.35f, 0f);
        }

        private Bullet AcquireBullet()
        {
            GameObject requestedPrefab = bulletPrefab;
            Stack<Bullet> pool = GetBulletPool(requestedPrefab);
            if (pool.Count > 0) return pool.Pop();

            GameObject instance;
            if (requestedPrefab != null)
            {
                instance = Instantiate(requestedPrefab);
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

                TrailRenderer trail = instance.AddComponent<TrailRenderer>();
                trail.time = 0.09f;
                trail.startWidth = 0.10f;
                trail.endWidth = 0.015f;
                trail.minVertexDistance = 0.04f;
                trail.sharedMaterial = PrototypeMaterials.Bullet;
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                trail.receiveShadows = false;
            }

            Bullet bullet = instance.GetComponent<Bullet>();
            if (bullet == null) bullet = instance.AddComponent<Bullet>();
            sourcePrefabByBullet[bullet] = requestedPrefab;
            return bullet;
        }

        private Stack<Bullet> GetBulletPool(GameObject prefab)
        {
            if (prefab == null) return prototypeBulletPool;

            Stack<Bullet> pool;
            if (!prefabBulletPools.TryGetValue(prefab, out pool))
            {
                pool = new Stack<Bullet>();
                prefabBulletPools.Add(prefab, pool);
            }
            return pool;
        }

        private float GetEstimatedHealth(ShootableTarget target)
        {
            EnemyAgent enemy = target as EnemyAgent;
            if (enemy != null)
                return enemy.CurrentHealth;

            DamageGate gate = target as DamageGate;
            if (gate != null)
                return gate.CurrentHealth;

            return float.PositiveInfinity;
        }

        private float GetReservedDamage(ShootableTarget target)
        {
            if (ReferenceEquals(target, null))
                return 0f;

            return reservedDamageByTarget.TryGetValue(target, out float value)
                ? Mathf.Max(0f, value)
                : 0f;
        }

        private void ReserveDamage(ShootableTarget target, float amount)
        {
            if (ReferenceEquals(target, null) || amount <= 0f)
                return;

            float current = GetReservedDamage(target);
            reservedDamageByTarget[target] = current + amount;
        }

        private void ReleaseReservedDamage(ShootableTarget target, float amount)
        {
            if (ReferenceEquals(target, null) ||
                !reservedDamageByTarget.TryGetValue(target, out float current))
                return;

            float remaining = Mathf.Max(0f, current - Mathf.Max(0f, amount));
            if (remaining <= 0.001f)
                reservedDamageByTarget.Remove(target);
            else
                reservedDamageByTarget[target] = remaining;
        }

        private void RecycleBullet(Bullet bullet, ShootableTarget assignedTarget, float assignedDamage)
        {
            ReleaseReservedDamage(assignedTarget, assignedDamage);
            if (bullet == null) return;

            GameObject sourcePrefab;
            sourcePrefabByBullet.TryGetValue(bullet, out sourcePrefab);
            bullet.gameObject.SetActive(false);
            GetBulletPool(sourcePrefab).Push(bullet);
        }

        private void BuildPrototypeMuzzleFlash()
        {
            if (muzzle == null || muzzleFlash != null) return;

            muzzleFlash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            muzzleFlash.name = "Prototype Muzzle Flash";
            muzzleFlash.transform.SetParent(muzzle, false);
            muzzleFlash.transform.localPosition = Vector3.forward * 0.1f;
            Collider collider = muzzleFlash.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            Renderer renderer = muzzleFlash.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = PrototypeMaterials.Bullet;
            muzzleFlash.SetActive(false);
        }
    }
}
