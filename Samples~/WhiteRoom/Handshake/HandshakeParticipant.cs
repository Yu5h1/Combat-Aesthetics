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
        private int _interactPressCount = 0;
        private bool _wasInteractPressedInHolding = false;
        private float _reReachCooldownTimer = 0f;

        private Vector3 _caughtJumpArcVelocity = Vector3.zero;
        private float _caughtJumpArcTimer = 0f;

        public CombatEntity Entity => _entity;
        public string EntityId => _entity != null ? _entity.EntityId : name;
        public int TeamId => _entity != null ? _entity.TeamId : 0;
        public HandshakeState State => _state;
        public float StateTimer => _stateTimer;
        public CombatActionIntent CurrentIntent => _entity != null ? _entity.CurrentIntent : CombatActionIntent.Empty;
        public bool RequiresInteractRelease => _requireInteractRelease;
        public int InteractPressCount => _interactPressCount;
        public float ReReachCooldownTimer => _reReachCooldownTimer;

        public Transform RightHandTransform { get; set; }
        public HandContactBridge ContactBridge { get; set; }

        public float ReachDuration { get => _reachDuration; set => _reachDuration = value; }
        public float WithdrawDuration { get => _withdrawDuration; set => _withdrawDuration = value; }

        public event Action<HandshakeParticipant, HandshakeState> OnStateChanged;
        public event Action<string> OnParticipantLog;

        /// <summary>
        /// Resets the accumulated contest press count to zero for a fresh strength contest.
        /// </summary>
        public void ResetPressCount()
        {
            _interactPressCount = 0;
            _wasInteractPressedInHolding = true;
        }

        /// <summary>
        /// Locks initiation of a new reach for the specified uncontrollable interval duration.
        /// </summary>
        public void LockReachingForDuration(float duration)
        {
            _reReachCooldownTimer = Mathf.Max(_reReachCooldownTimer, duration);
        }

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

        private void Update()
        {
            if (_caughtJumpArcTimer > 0f)
            {
                _caughtJumpArcTimer -= Time.deltaTime;
                CharacterController cc = GetComponent<CharacterController>();
                if (cc != null && cc.enabled)
                {
                    cc.Move(_caughtJumpArcVelocity * Time.deltaTime);
                }
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
                col.radius = 0.25f;
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

            if (newState == HandshakeState.Holding)
            {
                _wasInteractPressedInHolding = true;
            }
            else
            {
                _wasInteractPressedInHolding = false;
            }

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

            var arb = HandshakeArbitrator.Instance;
            float followSpeed = arb != null && arb.Settings != null ? arb.Settings.CatchingFollowSpeed : 1.20f;
            float walkSpeed = arb != null && arb.Settings != null ? arb.Settings.CaughtWalkSpeed : _entity.WalkSpeed;

            if (_state == HandshakeState.Grip || _state == HandshakeState.Shaking ||
                _state == HandshakeState.Laugh || _state == HandshakeState.Angry || _state == HandshakeState.ClapLaugh ||
                _state == HandshakeState.Holding || _state == HandshakeState.ContestTriumph ||
                _state == HandshakeState.ContestPain || _state == HandshakeState.ContestTie ||
                _state == HandshakeState.EscapedStumble || _state == HandshakeState.GraspEmpty)
            {
                _entity.LocomotionLocked = true;
                _entity.SpeedOverride = 0f;
            }
            else if (_state == HandshakeState.Catching)
            {
                _entity.LocomotionLocked = false;
                _entity.SpeedOverride = followSpeed;
            }
            else if (_state == HandshakeState.Caught)
            {
                _entity.LocomotionLocked = false;
                _entity.SpeedOverride = walkSpeed;
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
            if (_reReachCooldownTimer > 0f)
            {
                _reReachCooldownTimer -= Time.deltaTime;
            }

            if (!intent.InteractHold)
            {
                _requireInteractRelease = false;
            }

            switch (_state)
            {
                case HandshakeState.None:
                    if ((intent.InteractHold || intent.InteractDown) && !_requireInteractRelease && _reReachCooldownTimer <= 0f)
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

                case HandshakeState.Holding:
                    bool isPressed = intent.InteractDown || intent.InteractHold;
                    if (isPressed && !_wasInteractPressedInHolding)
                    {
                        _interactPressCount++;
                        EmitLog($"[{EntityId}] 握住中比力氣連按互動鍵！累計: {_interactPressCount} 次");
                    }
                    _wasInteractPressedInHolding = isPressed;
                    break;

                case HandshakeState.Caught:
                    if (intent.InteractDown || intent.InteractHold)
                    {
                        HandshakeArbitrator.Instance?.NotifyCaughtParticipantRepressed(this);
                    }
                    else if (intent.Jump)
                    {
                        ApplyCaughtJumpDisplacement();
                    }
                    break;

                case HandshakeState.Catching:
                    if (intent.Jump && _entity != null)
                    {
                        _entity.Jump();
                    }
                    break;

                case HandshakeState.Withdrawing:
                case HandshakeState.Laugh:
                case HandshakeState.Angry:
                case HandshakeState.ClapLaugh:
                case HandshakeState.ContestTriumph:
                case HandshakeState.ContestPain:
                case HandshakeState.ContestTie:
                case HandshakeState.EscapedStumble:
                case HandshakeState.GraspEmpty:
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

        private void ApplyCaughtJumpDisplacement()
        {
            var arb = HandshakeArbitrator.Instance;
            if (arb == null || arb.CatchingParticipant == null || _entity == null) return;

            _entity.Jump();

            Vector3 away = transform.position - arb.CatchingParticipant.transform.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.001f) away = -transform.forward;
            away.Normalize();

            float disp = arb.Settings != null ? arb.Settings.JumpEscapeDisplacement : 0.60f;
            _caughtJumpArcTimer = 0.40f;
            _caughtJumpArcVelocity = away * (disp / 0.40f);

            EmitLog($"[{EntityId}] 被抓住時跳躍往後彈開 {disp:F2}m！");
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
            _reReachCooldownTimer = 0f;
            _interactPressCount = 0;
            _wasInteractPressedInHolding = false;
            _caughtJumpArcTimer = 0f;
            _caughtJumpArcVelocity = Vector3.zero;
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
