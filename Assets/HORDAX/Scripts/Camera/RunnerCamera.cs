using UnityEngine;

namespace HORDAX.CameraSystem
{
    public sealed class RunnerCamera : MonoBehaviour
    {
        [SerializeField] private Vector3 offset = new Vector3(0f, 10.5f, -12.5f);
        [SerializeField] private float lookAhead = 8f;
        [SerializeField] private float smoothTime = 0.12f;
        [SerializeField] private float maxShake = 0.45f;
        [SerializeField] private bool frameTrackCenter;
        [SerializeField] private float trackCenterX;

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

        public void ConfigureTrackFraming(float centerX)
        {
            frameTrackCenter = true;
            trackCenterX = centerX;
            offset = new Vector3(0f, 11.8f, -15.2f);
            lookAhead = 10.5f;
            smoothTime = 0.14f;
        }

        public void SetTarget(Transform value)
        {
            target = value;
            if (target != null)
            {
                Vector3 focus = GetFocusPoint();
                transform.position = focus + offset;
                transform.LookAt(focus + Vector3.forward * lookAhead);
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

            Vector3 focus = GetFocusPoint();
            Vector3 desired = focus + offset;
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
            transform.LookAt(focus + Vector3.forward * lookAhead + Vector3.up * 0.5f);
        }

        private Vector3 GetFocusPoint()
        {
            Vector3 focus = target.position;
            if (frameTrackCenter)
                focus.x = trackCenterX;
            return focus;
        }
    }
}
