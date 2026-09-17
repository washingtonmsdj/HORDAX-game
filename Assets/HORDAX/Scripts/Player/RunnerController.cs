using UnityEngine;
using HORDAX.Core;

namespace HORDAX.Player
{
    public sealed class RunnerController : MonoBehaviour
    {
        [Header("Runner")]
        [SerializeField] private float forwardSpeed = 8f;
        [SerializeField] private float keyboardLateralSpeed = 9f;
        [SerializeField] private float dragSensitivity = 14f;
        [SerializeField] private float laneHalfWidth = 5.2f;

        private float desiredX;
        private float lastPointerX;
        private bool pointerDown;

        public float ForwardSpeed => forwardSpeed;

        private void Start()
        {
            desiredX = transform.position.x;
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

            ReadKeyboard();
            ReadPointer();

            desiredX = Mathf.Clamp(desiredX, -laneHalfWidth, laneHalfWidth);
            Vector3 position = transform.position;
            position.x = Mathf.MoveTowards(position.x, desiredX, dragSensitivity * Time.deltaTime);
            position.z += forwardSpeed * Time.deltaTime;
            transform.position = position;
        }

        private void ReadKeyboard()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            desiredX += horizontal * keyboardLateralSpeed * Time.deltaTime;
        }

        private void ReadPointer()
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    pointerDown = true;
                    lastPointerX = touch.position.x;
                }
                else if (pointerDown && (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary))
                {
                    ApplyPointerDelta(touch.position.x - lastPointerX);
                    lastPointerX = touch.position.x;
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    pointerDown = false;
                }
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                pointerDown = true;
                lastPointerX = Input.mousePosition.x;
            }
            else if (pointerDown && Input.GetMouseButton(0))
            {
                float currentX = Input.mousePosition.x;
                ApplyPointerDelta(currentX - lastPointerX);
                lastPointerX = currentX;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                pointerDown = false;
            }
        }

        private void ApplyPointerDelta(float pixelDelta)
        {
            float normalized = pixelDelta / Mathf.Max(1f, Screen.width);
            desiredX += normalized * laneHalfWidth * 4f;
        }
    }
}
