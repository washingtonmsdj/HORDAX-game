using System.Collections.Generic;
using UnityEngine;
using HORDAX.Prototype;
using HORDAX.Data;

namespace HORDAX.Combat
{
    public sealed class CombatFxPool : MonoBehaviour
    {
        [SerializeField] private int prewarmCount = 48;

        private readonly Stack<CombatFx> inactive = new Stack<CombatFx>();
        public static CombatFxPool Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            for (int i = 0; i < prewarmCount; i++)
                inactive.Push(CreateFx());
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void PlayImpact(Vector3 position, float scale = 0.22f)
        {
            Play(position, PrototypeMaterials.Bullet, scale, 0.09f, Random.insideUnitSphere * 0.6f);
        }

        public void PlayDeath(Vector3 position, float scale = 0.75f)
        {
            PlayDeath(position, scale, EnemyRank.Grunt);
        }

        public void PlayDeath(Vector3 position, float scale, EnemyRank rank)
        {
            Material material = rank == EnemyRank.Boss
                ? PrototypeMaterials.Boss
                : rank == EnemyRank.Elite ? PrototypeMaterials.Elite : PrototypeMaterials.Enemy;

            Play(position, material, scale, 0.16f, Vector3.up * 0.8f + Random.insideUnitSphere * 0.8f);
        }

        public void PlayGateBreak(Vector3 position, float scale = 1.2f)
        {
            Play(position, PrototypeMaterials.Gate, scale, 0.22f, Vector3.up * 0.5f);
        }

        private void Play(Vector3 position, Material material, float scale, float duration, Vector3 velocity)
        {
            CombatFx fx = inactive.Count > 0 ? inactive.Pop() : CreateFx();
            fx.transform.SetParent(null, true);
            fx.Play(this, position, material, scale, duration, velocity);
        }

        internal void Release(CombatFx fx)
        {
            if (fx == null) return;
            fx.gameObject.SetActive(false);
            fx.transform.SetParent(transform, false);
            inactive.Push(fx);
        }

        private CombatFx CreateFx()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Combat FX (pooled)";
            go.transform.SetParent(transform, false);
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            CombatFx fx = go.AddComponent<CombatFx>();
            go.SetActive(false);
            return fx;
        }
    }
}
