using System;
using UnityEngine;
using Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom;

namespace Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom.Handshake
{
    /// <summary>
    /// Handshake interaction state component that shares <see cref="CombatEntity"/> locomotion, jump, and arena reset.
    /// Governed by action rationality constraints and animation clip intervals rather than arbitrary countdown timers.
    /// Enforces the re-press rule requiring the interact key to be released after completion before re-initiating.
    /// </summary>
    [RequireComponent(typeof(CombatEntity))]
    public class HandshakeParticipant : MonoBehaviour
    {
        private CombatEntity _entity;
        private HandshakeState _state = HandshakeState.None;
        private float _stateTime = 0f;
        private bool _hasLoggedMissingClip = false;
        private bool _requireInteractRelease = false;
        private bool _prevInteractHold = false;
        private int _interactPressCount = 0;

        public CombatEntity Entity => _entity;
        public string EntityId => _entity != null ? _entity.EntityId : name;
        public int TeamId => _entity != null ? _entity.TeamId : 0;
        public HandshakeState State => _state;
        public float StateTime => _stateTime;
        public float StateTimer => _stateTime; // Backward compatibility property
        public CombatActionIntent CurrentIntent => _entity != null ? _entity.CurrentIntent : CombatActionIntent.Empty;
        public bool RequiresInteractRelease => _requireInteractRelease;
        public int InteractPressCount => _interactPressCount;

        public Transform RightHandTransform { get; set; }
        public HandContactBridge ContactBridge { get; set; }

        public HandshakeSettings Settings => HandshakeArbitrator.Instance != null ? HandshakeArbitrator.Instance.Settings : null;

        public float ReachDuration => GetClipLength(HandshakeState.Reaching);
        public float WithdrawDuration => GetClipLength(HandshakeState.Withdrawing);

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
                    col.radius = 0.18f;
                }
                else if (col.radius > 0.35f)
                {
                    col.radius = 0.18f;
                }
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
        /// Duration parameter is deprecated; action duration and control intervals are bound from the animation clip.
        /// </summary>
        public void SetState(HandshakeState newState, float duration = 0f)
        {
            if (_state == newState) return;

            HandshakeState prev = _state;
            _state = newState;
            _stateTime = 0f;
            _hasLoggedMissingClip = false;

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
                _state == HandshakeState.EscapedStumble || _state == HandshakeState.GraspEmpty ||
                _state == HandshakeState.Caught || _state == HandshakeState.Catching || _state == HandshakeState.Leap)
            {
                // Displacement in Caught, Catching, and Leap is driven strictly by animation root motion
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
            bool isInteractDown = intent.InteractDown || (!_prevInteractHold && intent.InteractHold);
            _prevInteractHold = intent.InteractHold;

            if (!intent.InteractHold)
            {
                _requireInteractRelease = false;
            }

            _stateTime += Time.deltaTime;

            AnimationClip currentClip = Settings != null ? Settings.GetClipForState(_state) : null;
            if (currentClip == null && _state != HandshakeState.None && !_hasLoggedMissingClip)
            {
                _hasLoggedMissingClip = true;
                Debug.LogError($"[HandshakeParticipant] Missing AnimationClip for state '{_state}' on '{name}'!");
            }
            float clipLength = currentClip != null ? currentClip.length : 0f;
            float normalizedTime = clipLength > 0f ? (_stateTime / clipLength) : 1.0f;

            switch (_state)
            {
                case HandshakeState.None:
                    if (isInteractDown && !_requireInteractRelease)
                    {
                        SetState(HandshakeState.Reaching);
                        EmitLog($"[{EntityId}] 按下互動鍵發起伸手（片段長度: {clipLength:F2}s）");
                    }
                    break;

                case HandshakeState.Reaching:
                    if (!intent.InteractHold)
                    {
                        SetState(HandshakeState.Withdrawing);
                        HandshakeArbitrator.Instance?.NotifyCancelled(this);
                        EmitLog($"[{EntityId}] 放開互動鍵，收手取消");
                    }
                    else if (normalizedTime >= 1.0f)
                    {
                        SetState(HandshakeState.Waiting);
                        EmitLog($"[{EntityId}] 伸手就緒，等待對方走近接觸");
                    }
                    break;

                case HandshakeState.Waiting:
                    if (!intent.InteractHold)
                    {
                        SetState(HandshakeState.Withdrawing);
                        HandshakeArbitrator.Instance?.NotifyCancelled(this);
                        EmitLog($"[{EntityId}] 放開互動鍵，等待中止並收手");
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
                    else if (intent.Jump)
                    {
                        SetState(HandshakeState.Leap);
                        EmitLog($"[{EntityId}] 被抓住時起跳（播放 handshake-leap 根位移跳開）");
                    }
                    break;

                case HandshakeState.Catching:
                    if (isInteractDown)
                    {
                        _interactPressCount++;
                        EmitLog($"[{EntityId}] 抓住對方中連按互動鍵！累計: {_interactPressCount} 次");
                    }
                    break;

                case HandshakeState.Leap:
                    if (isInteractDown)
                    {
                        bool underControl = CheckActionUnderControl(currentClip, normalizedTime);
                        if (underControl && !_requireInteractRelease)
                        {
                            SetState(HandshakeState.Reaching);
                            EmitLog($"[{EntityId}] 在跳開可控制區間（t={normalizedTime:F2}）重按互動鍵發起伸手");
                        }
                    }
                    else if (normalizedTime >= 1.0f)
                    {
                        if (intent.InteractHold)
                        {
                            _requireInteractRelease = true;
                        }
                        SetState(HandshakeState.None);
                    }
                    break;

                case HandshakeState.Withdrawing:
                    if (isInteractDown)
                    {
                        bool underControl = CheckActionUnderControl(currentClip, normalizedTime);
                        if (underControl && !_requireInteractRelease)
                        {
                            SetState(HandshakeState.Reaching);
                            EmitLog($"[{EntityId}] 在收手可控制區間（t={normalizedTime:F2}）重按互動鍵再次伸手");
                            break;
                        }
                        else
                        {
                            EmitLog($"[{EntityId}] 在收手不可控制區間（t={normalizedTime:F2}）重按互動鍵無效");
                        }
                    }

                    if (normalizedTime >= 1.0f)
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
                    if (isInteractDown)
                    {
                        bool underControl = CheckActionUnderControl(currentClip, normalizedTime);
                        if (underControl && !_requireInteractRelease)
                        {
                            SetState(HandshakeState.Reaching);
                            EmitLog($"[{EntityId}] 在反應動作可控制區間（t={normalizedTime:F2}）重按互動鍵發起伸手");
                            break;
                        }
                        else
                        {
                            EmitLog($"[{EntityId}] 在反應動作不可控制區間（t={normalizedTime:F2}）重按互動鍵無效");
                        }
                    }

                    if (normalizedTime >= 1.0f)
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

        private bool CheckActionUnderControl(AnimationClip clip, float normalizedTime)
        {
            if (Settings == null || clip == null)
            {
                Debug.LogError($"[HandshakeParticipant] Cannot evaluate action control for state '{_state}': Settings or clip is missing on '{name}'!");
                return false;
            }
            return Settings.IsActionUnderControl(clip, normalizedTime);
        }

        private float GetClipLength(HandshakeState state)
        {
            AnimationClip clip = Settings != null ? Settings.GetClipForState(state) : null;
            if (clip == null)
            {
                Debug.LogError($"[HandshakeParticipant] AnimationClip for '{state}' is missing or Settings is null on '{name}'!");
                return 0f;
            }
            return clip.length;
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
            _stateTime = 0f;
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
