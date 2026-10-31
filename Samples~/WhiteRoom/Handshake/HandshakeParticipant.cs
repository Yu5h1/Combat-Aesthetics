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
        private bool _prevInteractHold = false;
        private int _interactPressCount = 0;

        public CombatEntity Entity => _entity;
        public string EntityId => _entity != null ? _entity.EntityId : name;
        public int TeamId => _entity != null ? _entity.TeamId : 0;
        public HandshakeState State => _state;
        public float StateTimer => _stateTimer;
        public CombatActionIntent CurrentIntent => _entity != null ? _entity.CurrentIntent : CombatActionIntent.Empty;
        public bool RequiresInteractRelease => _requireInteractRelease;
        public int InteractPressCount => _interactPressCount;

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
        }

        private void Awake()
        {
            _entity = GetComponent<CombatEntity>();
            if (_entity != null)
            {
                _entity.EnableCombatActions = false;
                _entity.SetColliderRadius(0.25f);
                _entity.OnIntentStepped -= HandleIntentStepped;
                _entity.OnIntentStepped += HandleIntentStepped;
                _entity.OnEntityReset -= HandleEntityReset;
                _entity.OnEntityReset += HandleEntityReset;
            }

            if (ContactBridge == null)
            {
                ContactBridge = GetComponentInChildren<HandContactBridge>();
                if (ContactBridge != null)
                {
                    ContactBridge.Initialize(this);
                }
            }

            if (RightHandTransform == null)
            {
                Transform wrist = transform.Find("HumanoidModel/root/pelvis/spine/chest/shoulder_r/elbow_r/wrist_r/fist_r");
                if (wrist == null) wrist = transform.Find("HumanoidModel/root/pelvis/spine/chest/shoulder_r/elbow_r/wrist_r");
                if (wrist != null) RightHandTransform = wrist;
            }
        }

        private void Start()
        {
            if (HandshakeArbitrator.Instance != null)
            {
                HandshakeArbitrator.Instance.RegisterParticipant(this);
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
                col.radius = 0.40f;
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
                if (CurrentIntent.InteractHold || _prevInteractHold)
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
                _entity.SpeedOverride = _entity.GuardSpeed;
            }
            else if (_state == HandshakeState.Caught)
            {
                _entity.LocomotionLocked = false;
                _entity.SpeedOverride = _entity.WalkSpeed;
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
            bool isInteractDown = intent.InteractDown || (!_prevInteractHold && intent.InteractHold);
            bool isInteractUp = intent.InteractUp || (_prevInteractHold && !intent.InteractHold);
            _prevInteractHold = intent.InteractHold;

            if (!intent.InteractHold)
            {
                _requireInteractRelease = false;
            }

            switch (_state)
            {
                case HandshakeState.None:
                    if (isInteractDown && !_requireInteractRelease)
                    {
                        SetState(HandshakeState.Reaching, _reachDuration);
                        EmitLog($"[{EntityId}] 按下互動鍵發起伸手（{_reachDuration:F2}s）");
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
                    if (isInteractDown)
                    {
                        _interactPressCount++;
                        EmitLog($"[{EntityId}] 握住中比力氣連按互動鍵！累計: {_interactPressCount} 次");
                    }
                    break;

                case HandshakeState.Caught:
                    if (isInteractDown)
                    {
                        _interactPressCount++;
                        EmitLog($"[{EntityId}] 被抓住時連按互動鍵重按！累計: {_interactPressCount} 次");
                        HandshakeArbitrator.Instance?.NotifyCaughtParticipantRepressed(this);
                    }
                    else if (intent.Jump && _entity != null)
                    {
                        _entity.Jump();
                        EmitLog($"[{EntityId}] [暫代] 被抓住時起跳（以角色跳躍暫代根位移跳開）");
                    }
                    break;

                case HandshakeState.Catching:
                    if (isInteractDown)
                    {
                        _interactPressCount++;
                        EmitLog($"[{EntityId}] 抓住對方中連按互動鍵！累計: {_interactPressCount} 次");
                    }
                    if (intent.Jump && _entity != null)
                    {
                        _entity.Jump();
                        EmitLog($"[{EntityId}] [暫代] 抓住對方時起跳（以角色跳躍暫代根位移跳躍）");
                    }
                    break;

                case HandshakeState.Withdrawing:
                    _stateTimer -= Time.deltaTime;
                    if (_stateTimer <= 0f)
                    {
                        var arb = HandshakeArbitrator.Instance;
                        if (arb != null && arb.SessionState == HandshakeState.Grip)
                        {
                            break;
                        }

                        if (intent.InteractHold)
                        {
                            _requireInteractRelease = true;
                        }
                        SetState(HandshakeState.None);
                    }
                    break;

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
            _interactPressCount = 0;
            _prevInteractHold = false;
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
