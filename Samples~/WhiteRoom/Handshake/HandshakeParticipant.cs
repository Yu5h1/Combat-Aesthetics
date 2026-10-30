using System;
using UnityEngine;
using Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom;

namespace Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom.Handshake
{
    /// <summary>
    /// Handshake interaction state component that shares <see cref="CombatEntity"/> locomotion, jump, and arena reset.
    /// Enforces the re-press rule requiring the interact key to be released after completion before re-initiating.
    /// </summary>
    [RequireComponent(typeof(CombatEntity))]
    public class HandshakeParticipant : MonoBehaviour
    {
        [SerializeField] private float _reachDuration = 0.50f;
        [SerializeField] private float _withdrawDuration = 0.50f;

        private CombatEntity _entity;
        private HandshakeState _state = HandshakeState.None;
        private float _stateTimer = 0f;
        private bool _requireInteractRelease = false;

        public CombatEntity Entity => _entity;
        public string EntityId => _entity != null ? _entity.EntityId : name;
        public int TeamId => _entity != null ? _entity.TeamId : 0;
        public HandshakeState State => _state;
        public float StateTimer => _stateTimer;
        public CombatActionIntent CurrentIntent => _entity != null ? _entity.CurrentIntent : CombatActionIntent.Empty;
        public bool RequiresInteractRelease => _requireInteractRelease;

        public Transform RightHandTransform { get; set; }
        public HandContactBridge ContactBridge { get; set; }

        public float ReachDuration { get => _reachDuration; set => _reachDuration = value; }
        public float WithdrawDuration { get => _withdrawDuration; set => _withdrawDuration = value; }

        public event Action<HandshakeParticipant, HandshakeState> OnStateChanged;
        public event Action<string> OnParticipantLog;

        private void Awake()
        {
            _entity = GetComponent<CombatEntity>();
            if (_entity != null)
            {
                _entity.EnableCombatActions = false;
                _entity.SetColliderRadius(0.25f);
                _entity.OnIntentStepped += HandleIntentStepped;
                _entity.OnEntityReset += HandleEntityReset;
            }
        }

        private void OnDestroy()
        {
            if (_entity != null)
            {
                _entity.OnIntentStepped -= HandleIntentStepped;
                _entity.OnEntityReset -= HandleEntityReset;
            }
        }

        /// <summary>
        /// Initializes the underlying <see cref="CombatEntity"/> identity and disables combat-only actions.
        /// </summary>
        public void Initialize(string id, int team)
        {
            if (_entity == null) _entity = GetComponent<CombatEntity>();
            if (_entity != null)
            {
                _entity.InitializeSettings(id, team, 100f);
                _entity.EnableCombatActions = false;
                _entity.SetColliderRadius(0.25f);
            }
        }

        /// <summary>
        /// Attaches a right-hand trigger collider and <see cref="HandContactBridge"/> for physical contact detection.
        /// </summary>
        public void SetRightHand(Transform visualHandTransform, Transform colliderHostTransform = null)
        {
            RightHandTransform = visualHandTransform;
            Transform host = colliderHostTransform != null ? colliderHostTransform : visualHandTransform;
            if (host != null)
            {
                var col = host.GetComponent<SphereCollider>();
                if (col == null)
                {
                    col = host.gameObject.AddComponent<SphereCollider>();
                }
                col.radius = 0.22f;
                col.isTrigger = true;

                ContactBridge = host.GetComponent<HandContactBridge>();
                if (ContactBridge == null)
                {
                    ContactBridge = host.gameObject.AddComponent<HandContactBridge>();
                }
                ContactBridge.Initialize(this);
            }
        }

        /// <summary>
        /// Transitions this participant to the specified <see cref="HandshakeState"/> and updates locomotion constraints.
        /// </summary>
        public void SetState(HandshakeState newState, float duration = 0f)
        {
            if (_state == newState) return;

            HandshakeState prev = _state;
            _state = newState;
            _stateTimer = duration;

            if (_state == HandshakeState.None && prev != HandshakeState.None)
            {
                if (CurrentIntent.InteractHold)
                {
                    _requireInteractRelease = true;
                }
            }

            UpdateEntityLocomotionConstraints();
            OnStateChanged?.Invoke(this, _state);
        }

        private void UpdateEntityLocomotionConstraints()
        {
            if (_entity == null) return;

            if (_state == HandshakeState.Grip || _state == HandshakeState.Shaking)
            {
                _entity.LocomotionLocked = true;
                _entity.SpeedOverride = 0f;
            }
            else if (_state == HandshakeState.Reaching || _state == HandshakeState.Waiting)
            {
                _entity.LocomotionLocked = false;
                _entity.SpeedOverride = _entity.GuardSpeed;
            }
            else
            {
                _entity.LocomotionLocked = false;
                _entity.SpeedOverride = -1f;
            }
        }

        private void HandleIntentStepped(CombatActionIntent intent)
        {
            UpdateHandshakeState(intent);
        }

        private void UpdateHandshakeState(CombatActionIntent intent)
        {
            if (!intent.InteractHold)
            {
                _requireInteractRelease = false;
            }

            switch (_state)
            {
                case HandshakeState.None:
                    if ((intent.InteractHold || intent.InteractDown) && !_requireInteractRelease)
                    {
                        SetState(HandshakeState.Reaching, _reachDuration);
                        EmitLog($"[{EntityId}] 按住互動鍵發起伸手（{_reachDuration:F2}s）");
                    }
                    break;

                case HandshakeState.Reaching:
                    if (!intent.InteractHold)
                    {
                        SetState(HandshakeState.Withdrawing, _withdrawDuration);
                        HandshakeArbitrator.Instance?.NotifyCancelled(this);
                        EmitLog($"[{EntityId}] 放開互動鍵，收手取消（{_withdrawDuration:F2}s）");
                    }
                    else
                    {
                        _stateTimer -= Time.deltaTime;
                        if (_stateTimer <= 0f)
                        {
                            SetState(HandshakeState.Waiting);
                            EmitLog($"[{EntityId}] 伸手就緒，等待對方走近接觸");
                        }
                    }
                    break;

                case HandshakeState.Waiting:
                    if (!intent.InteractHold)
                    {
                        SetState(HandshakeState.Withdrawing, _withdrawDuration);
                        HandshakeArbitrator.Instance?.NotifyCancelled(this);
                        EmitLog($"[{EntityId}] 放開互動鍵，等待中止並收手（{_withdrawDuration:F2}s）");
                    }
                    break;

                case HandshakeState.Grip:
                case HandshakeState.Shaking:
                    break;

                case HandshakeState.Withdrawing:
                    _stateTimer -= Time.deltaTime;
                    if (_stateTimer <= 0f)
                    {
                        if (intent.InteractHold)
                        {
                            _requireInteractRelease = true;
                        }
                        SetState(HandshakeState.None);
                    }
                    break;
            }
        }

        private void EmitLog(string message)
        {
            OnParticipantLog?.Invoke(message);
            if (_entity != null)
            {
                _entity.EmitLog(message);
            }
        }

        private void HandleEntityReset()
        {
            _stateTimer = 0f;
            _requireInteractRelease = false;
            SetState(HandshakeState.None);
        }

        /// <summary>
        /// Resets this participant and its underlying <see cref="CombatEntity"/>.
        /// </summary>
        public void ResetParticipant()
        {
            if (_entity != null)
            {
                _entity.ResetEntity();
            }
            else
            {
                HandleEntityReset();
            }
        }
    }
}
