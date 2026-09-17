using UnityEngine;

namespace HORDAX.CameraSystem
{
    public sealed class RunnerCamera : MonoBehaviour
    {
        [SerializeField] private Vector3 offset = new Vector3(0f, 10.5f, -12.5f);
        [SerializeField] private float lookAhead = 8f;
        [SerializeField] private float smoothTime = 0.12f;

        private Transform target;
        private Vector3 velocity;

        public void SetTarget(Transform value)
        {
            target = value;
            if (target != null)
            {
                transform.position = target.position + offset;
                transform.LookAt(target.position + Vector3.forward * lookAhead);
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desired = target.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
            transform.LookAt(target.position + Vector3.forward * lookAhead + Vector3.up * 0.5f);
        }
    }
}
