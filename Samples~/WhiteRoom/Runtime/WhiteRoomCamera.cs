using UnityEngine;
using Yu5h1Lib;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom
{
    /// <summary>
    /// Third-person orbit camera that tracks a target transform and yields cursor focus when the HUD is hovered.
    /// </summary>
    public class WhiteRoomCamera : MonoBehaviour
    {
        [Header("Target Settings")]
        [SerializeField] private Transform _target;
        [SerializeField] private Vector3 _targetOffset = new Vector3(0, 1.4f, 0);
        [SerializeField] private Vector3 _shoulderOffset = Vector3.zero;

        [Header("Orbit Settings")]
        [SerializeField] private float _distance = 4.0f;
        [SerializeField] private float _minDistance = 1.5f;
        [SerializeField] private float _maxDistance = 8.0f;
        [SerializeField] private float _mouseSensitivity = 2.0f;
        [SerializeField] private float _minPitch = -25f;
        [SerializeField] private float _maxPitch = 65f;
        [SerializeField] private float _smoothSpeed = 15f;

        private float _yaw = 0f;
        private float _pitch = 15f;
        private bool _hasExplicitOrbit = false;

        public Transform Target
        {
            get => _target;
            set => _target = value;
        }

        /// <summary>
        /// Explicitly sets the orbit angles and distance (e.g. for initial camera framing in specific scenes).
        /// </summary>
        public void SetOrbit(float yaw, float pitch, float distance = -1f)
        {
            _yaw = yaw;
            _pitch = Mathf.Clamp(pitch, _minPitch, _maxPitch);
            if (distance > 0f)
            {
                _distance = Mathf.Clamp(distance, _minDistance, _maxDistance);
            }
            _hasExplicitOrbit = true;
        }

        /// <summary>
        /// Sets a lateral or vertical shoulder offset relative to the camera orbit orientation.
        /// </summary>
        public void SetShoulderOffset(Vector3 offset)
        {
            _shoulderOffset = offset;
        }

        private void Start()
        {
            if (!_hasExplicitOrbit)
            {
                Vector3 angles = transform.eulerAngles;
                _yaw = angles.y;
                _pitch = angles.x;
            }

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
            Vector3 pivot = _target.position + _targetOffset + (rotation * _shoulderOffset);
            Vector3 desiredPosition = pivot - (rotation * Vector3.forward * _distance);

            if (Physics.Raycast(pivot, desiredPosition - pivot, out RaycastHit hit, _distance))
            {
                if (hit.transform != _target)
                {
                    desiredPosition = hit.point + hit.normal * 0.2f;
                }
            }

            transform.position = Vector3.Lerp(transform.position, desiredPosition, _smoothSpeed * Time.deltaTime);

            if (_shoulderOffset == Vector3.zero)
            {
                transform.LookAt(pivot);
            }
            else
            {
                transform.rotation = rotation;
            }
        }
    }
}
