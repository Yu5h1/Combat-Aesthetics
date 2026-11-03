using UnityEngine;
using Yu5h1Lib;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom
{
    /// <summary>
    /// Third-person orbit camera that tracks a target transform and yields cursor focus when the HUD is hovered.
    /// Every WhiteRoom scene gets it through <see cref="WhiteRoomSceneBuilder.EnsureCamera"/> and these field defaults are the only framing configuration; no scene or demo overrides them.
    /// </summary>
    public class WhiteRoomCamera : MonoBehaviour
    {
        [Header("Target Settings")]
        [SerializeField] private Transform _target;
        [SerializeField] private Vector3 _targetOffset = new Vector3(0, 1.4f, 0);

        [Header("Orbit Settings")]
        [SerializeField] private float _distance = 4.0f;
        [SerializeField] private float _mouseSensitivity = 2.0f;
        [SerializeField] private float _minPitch = -25f;
        [SerializeField] private float _maxPitch = 65f;
        [SerializeField] private float _smoothSpeed = 15f;

        private float _yaw = 0f;
        private float _pitch = 15f;

        public Transform Target
        {
            get => _target;
            set => _target = value;
        }

        private void Start()
        {
            Vector3 angles = transform.eulerAngles;
            _yaw = angles.y;
            _pitch = angles.x;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (WhiteRoomCharacter.IsUIFocused)
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                return;
            }

            bool toggleCursor = InputHandler.GetKeyDown(KeyCode.LeftAlt) || InputHandler.GetKeyDown(KeyCode.Escape);

            if (toggleCursor)
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 mouseDelta = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
                if (Mouse.current != null)
                {
                    mouseDelta = Mouse.current.delta.ReadValue() * 0.1f;
                }
#else
                mouseDelta.x = InputHandler.GetAxis("Mouse X");
                mouseDelta.y = InputHandler.GetAxis("Mouse Y");
#endif

                _yaw += mouseDelta.x * _mouseSensitivity;
                _pitch -= mouseDelta.y * _mouseSensitivity;
                _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);
            }
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 pivot = _target.position + _targetOffset;
            Vector3 desiredPosition = pivot - (rotation * Vector3.forward * _distance);

            Vector3 rayDir = desiredPosition - pivot;
            float rayDist = rayDir.magnitude;
            if (rayDist > 0.01f)
            {
                rayDir /= rayDist;
                if (Physics.Raycast(pivot, rayDir, out RaycastHit hit, rayDist, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (hit.transform != _target && !hit.transform.IsChildOf(_target))
                    {
                        desiredPosition = hit.point + hit.normal * 0.2f;
                    }
                }
            }

            transform.position = Vector3.Lerp(transform.position, desiredPosition, _smoothSpeed * Time.deltaTime);

            transform.LookAt(pivot);
        }
    }
}
