using UnityEngine;

namespace HORDAX.CameraSystem
{
    public sealed class RunnerCamera : MonoBehaviour
    {
        [SerializeField] private Vector3 offset = new Vector3(0f, 10.5f, -12.5f);
        [SerializeField] private float lookAhead = 8f;
        [SerializeField] private float smoothTime = 0.12f;
        [SerializeField] private float maxShake = 0.45f;

        private Transform target;
        private Vector3 velocity;
        private float shakeAmplitude;
        private float shakeRemaining;
        private float shakeDuration;

        public static RunnerCamera Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SetTarget(Transform value)
        {
            target = value;
            if (target != null)
            {
                transform.position = target.position + offset;
                transform.LookAt(target.position + Vector3.forward * lookAhead);
            }
        }

        public void Shake(float amplitude, float duration)
        {
            shakeAmplitude = Mathf.Max(shakeAmplitude, Mathf.Min(maxShake, amplitude));
            shakeDuration = Mathf.Max(shakeDuration, duration);
            shakeRemaining = Mathf.Max(shakeRemaining, duration);
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desired = target.position + offset;
            Vector3 smoothed = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);

            if (shakeRemaining > 0f)
            {
                shakeRemaining -= Time.deltaTime;
                float normalized = shakeDuration > 0f ? Mathf.Clamp01(shakeRemaining / shakeDuration) : 0f;
                smoothed += Random.insideUnitSphere * (shakeAmplitude * normalized);
                if (shakeRemaining <= 0f)
                {
                    shakeAmplitude = 0f;
                    shakeDuration = 0f;
                }
            }

            transform.position = smoothed;
            transform.LookAt(target.position + Vector3.forward * lookAhead + Vector3.up * 0.5f);
        }
    }
}
