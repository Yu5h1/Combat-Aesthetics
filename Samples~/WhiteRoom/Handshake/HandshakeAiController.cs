using UnityEngine;
using Yu5h1Lib;
using Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom;

namespace Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom.Handshake
{
    /// <summary>
    /// Preset AI behavior modes for handshake scenario testing.
    /// </summary>
    public enum HandshakeAiMode
    {
        Idle = 0,
        AiInitiator = 1,
        AiResponder = 2,
        AiCancel = 3,
        AiEarlyRelease = 4,
        AiReleaseAfterGrip = 5
    }

    /// <summary>
    /// Configurable AI controller for the Stage 2b handshake interaction.
    /// Exposes serialized settings for reach delay, hold duration, walk-in toggle, and stop distance,
    /// and drives the shared <see cref="CombatEntity"/> through <see cref="CombatActionIntent"/>.
    /// </summary>
    [RequireComponent(typeof(CombatEntity))]
    [RequireComponent(typeof(HandshakeParticipant))]
    public class HandshakeAiController : MonoBehaviour, ICombatInputSource
    {
        [Header("Configurable AI Test Settings")]
        [SerializeField] private HandshakeAiMode _mode = HandshakeAiMode.AiInitiator;
        [SerializeField] private float _reachDelay = 0.6f;
        [SerializeField] private float _holdDuration = -1f;
        [SerializeField] private float _cancelHoldDuration = 1.2f;
        [SerializeField] private float _earlyReleaseGripTime = 0.18f;
        [SerializeField] private float _releaseAfterGripDelay = 0.15f;
        [SerializeField] private bool _walkIn = false;
        [SerializeField] private float _stopDistance = 0.78f;

        private CombatEntity _entity;
        private HandshakeParticipant _participant;
        private HandshakeParticipant _targetPlayer;
        private float _timer = 0f;
        private float _holdTimer = 0f;
        private float _gripTimer = 0f;
        private float _postGripTimer = 0f;
        private bool _isHoldingInteract = false;
        private bool _hasCompletedHoldCycle = false;

        public HandshakeAiMode Mode
        {
            get => _mode;
            set
            {
                _mode = value;
                ApplyModeDefaults(value);
                ResetAI();
            }
        }

        public float ReachDelay { get => _reachDelay; set => _reachDelay = value; }
        public float HoldDuration { get => _holdDuration; set => _holdDuration = value; }
        public float CancelHoldDuration { get => _cancelHoldDuration; set => _cancelHoldDuration = value; }
        public float EarlyReleaseGripTime { get => _earlyReleaseGripTime; set => _earlyReleaseGripTime = value; }
        public float ReleaseAfterGripDelay { get => _releaseAfterGripDelay; set => _releaseAfterGripDelay = value; }
        public bool WalkIn { get => _walkIn; set => _walkIn = value; }
        public float StopDistance { get => _stopDistance; set => _stopDistance = value; }

        private void Awake()
        {
            _entity = GetComponent<CombatEntity>();
            _participant = GetComponent<HandshakeParticipant>();
            ApplyModeDefaults(_mode);
        }

        private void OnEnable()
        {
            CombatEntity.OnGlobalArenaReset += ResetAI;
        }

        private void OnDisable()
        {
            CombatEntity.OnGlobalArenaReset -= ResetAI;
        }

        private void Start()
        {
            FindTargetPlayer();
        }

        private void Update()
        {
            if (_entity == null || _participant == null) return;
            if (_targetPlayer == null) FindTargetPlayer();

            CombatActionIntent intent = PollIntent(_entity);
            _entity.StepCombat(intent);
        }

        /// <summary>
        /// Configures the AI test parameters directly for custom scenarios.
        /// </summary>
        public void ConfigureSettings(float reachDelay, float holdDuration, bool walkIn, float stopDistance = 0.85f)
        {
            _reachDelay = reachDelay;
            _holdDuration = holdDuration;
            _walkIn = walkIn;
            _stopDistance = stopDistance;
            ResetAI();
        }

        private void ApplyModeDefaults(HandshakeAiMode mode)
        {
            switch (mode)
            {
                case HandshakeAiMode.Idle:
                    _holdDuration = 0f;
                    _walkIn = false;
                    break;

                case HandshakeAiMode.AiInitiator:
                    _holdDuration = -1f;
                    _walkIn = false;
                    break;

                case HandshakeAiMode.AiResponder:
                    _holdDuration = -1f;
                    _walkIn = true;
                    break;

                case HandshakeAiMode.AiCancel:
                    _holdDuration = _cancelHoldDuration;
                    _walkIn = false;
                    break;

                case HandshakeAiMode.AiEarlyRelease:
                    _holdDuration = -1f;
                    _walkIn = false;
                    break;

                case HandshakeAiMode.AiReleaseAfterGrip:
                    _holdDuration = -1f;
                    _walkIn = false;
                    break;
            }
        }

        private void FindTargetPlayer()
        {
            var participants = ObjectUtility.FindObjects<HandshakeParticipant>(false);
            foreach (var p in participants)
            {
                if (p != null && p != _participant && p.TeamId == 0)
                {
                    _targetPlayer = p;
                    break;
                }
            }
        }

        /// <summary>
        /// Produces the AI's per-frame movement and interact key intent based on its configured parameters.
        /// </summary>
        public CombatActionIntent PollIntent(CombatEntity entity)
        {
            CombatActionIntent intent = CombatActionIntent.Empty;
            intent.Timestamp = Time.time;

            if (_targetPlayer == null || _mode == HandshakeAiMode.Idle)
            {
                _isHoldingInteract = false;
                return intent;
            }

            _timer += Time.deltaTime;

            Vector3 toTarget = _targetPlayer.transform.position - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            bool prevHold = _isHoldingInteract;

            if (_mode == HandshakeAiMode.AiResponder)
            {
                bool playerReady = (_targetPlayer.State == HandshakeState.Reaching || _targetPlayer.State == HandshakeState.Waiting ||
                                    _participant.State == HandshakeState.Grip || _participant.State == HandshakeState.Shaking);
                if (playerReady && !_hasCompletedHoldCycle)
                {
                    _isHoldingInteract = true;
                }
                else if (!playerReady && _participant.State == HandshakeState.None)
                {
                    _isHoldingInteract = false;
                    _holdTimer = 0f;
                }
            }
            else
            {
                if (_timer >= _reachDelay && !_hasCompletedHoldCycle)
                {
                    _isHoldingInteract = true;
                }
            }

            if (_isHoldingInteract)
            {
                _holdTimer += Time.deltaTime;
                if (_holdDuration > 0f && _holdTimer >= _holdDuration)
                {
                    _isHoldingInteract = false;
                    _hasCompletedHoldCycle = true;
                }
            }

            if (_mode == HandshakeAiMode.AiEarlyRelease)
            {
                if (_participant.State == HandshakeState.Grip)
                {
                    _gripTimer += Time.deltaTime;
                    if (_gripTimer >= _earlyReleaseGripTime)
                    {
                        _isHoldingInteract = false;
                        _hasCompletedHoldCycle = true;
                    }
                }
                else
                {
                    _gripTimer = 0f;
                }
            }
            else if (_mode == HandshakeAiMode.AiReleaseAfterGrip)
            {
                if (_participant.State == HandshakeState.Grip)
                {
                    _gripTimer += Time.deltaTime;
                }
                else if (_gripTimer > 0f)
                {
                    _postGripTimer += Time.deltaTime;
                    if (_postGripTimer >= _releaseAfterGripDelay)
                    {
                        _isHoldingInteract = false;
                        _hasCompletedHoldCycle = true;
                    }
                }
            }

            if (_walkIn && _isHoldingInteract && distance > _stopDistance &&
                (_participant.State == HandshakeState.Reaching || _participant.State == HandshakeState.Waiting))
            {
                Vector3 moveDir = toTarget.normalized;
                intent.MoveInput = new Vector2(moveDir.x, moveDir.z);
            }

            intent.InteractHold = _isHoldingInteract;
            intent.InteractDown = !prevHold && _isHoldingInteract;
            intent.InteractUp = prevHold && !_isHoldingInteract;
            return intent;
        }

        /// <summary>
        /// Resets AI timers and hold latches.
        /// </summary>
        public void ResetAI()
        {
            _timer = 0f;
            _holdTimer = 0f;
            _gripTimer = 0f;
            _postGripTimer = 0f;
            _isHoldingInteract = false;
            _hasCompletedHoldCycle = false;
        }
    }
}
