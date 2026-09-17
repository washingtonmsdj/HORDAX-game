using UnityEngine;

namespace HORDAX.Combat
{
    public sealed class CombatFx : MonoBehaviour
    {
        private CombatFxPool owner;
        private Renderer cachedRenderer;
        private float age;
        private float duration;
        private float startScale;
        private float endScale;
        private Vector3 drift;

        public void Play(CombatFxPool pool, Vector3 position, Material material, float scale, float life, Vector3 velocity)
        {
            owner = pool;
            if (cachedRenderer == null) cachedRenderer = GetComponent<Renderer>();
            if (cachedRenderer != null) cachedRenderer.sharedMaterial = material;

            transform.position = position;
            transform.rotation = Random.rotation;
            startScale = Mathf.Max(0.01f, scale);
            endScale = startScale * 1.8f;
            transform.localScale = Vector3.one * startScale;
            duration = Mathf.Max(0.02f, life);
            age = 0f;
            drift = velocity;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / duration);
            transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, t);
            transform.position += drift * Time.deltaTime;
            drift *= Mathf.Pow(0.08f, Time.deltaTime);

            if (age >= duration)
                owner?.Release(this);
        }
    }
}
