using UnityEngine;

namespace HORDAX.Benchmarks
{
    public sealed class BenchmarkFlyCamera : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float fastMultiplier = 3f;
        [SerializeField] private float lookSpeed = 2f;

        private float yaw;
        private float pitch;

        private void Awake()
        {
            Vector3 euler = transform.eulerAngles;
            yaw = euler.y;
            pitch = euler.x;
        }

        private void Update()
        {
            float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? fastMultiplier : 1f);
            Vector3 move = Vector3.zero;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) move += Vector3.forward;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) move += Vector3.back;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) move += Vector3.left;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) move += Vector3.right;
            if (Input.GetKey(KeyCode.E)) move += Vector3.up;
            if (Input.GetKey(KeyCode.Q)) move += Vector3.down;

            transform.Translate(move.normalized * speed * Time.unscaledDeltaTime, Space.Self);

            if (Input.GetMouseButton(1))
            {
                yaw += Input.GetAxis("Mouse X") * lookSpeed;
                pitch -= Input.GetAxis("Mouse Y") * lookSpeed;
                pitch = Mathf.Clamp(pitch, -89f, 89f);
                transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            }
        }
    }
}
