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
        AiReleaseAfterGrip = 5,
        ContestWin = 6,
        ContestLose = 7,
        ContestTie = 8,
        ContestZeroZero = 9,
        AiCatchPlayer = 10,
        AiCaughtByPlayer = 11,
        AiEscape = 12,
        AiChaseCatch = 13,
        AiJumpNoEscape = 14,
        AiCatchAndRelease = 15
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

        [Header("Batch 3 Contest & Catch Settings")]
        [SerializeField] private int _contestTapTarget = 0;
        [SerializeField] private float _contestTapInterval = 0.1f;
        [SerializeField] private float _holdingReleaseDelay = 0.8f;
        [SerializeField] private bool _pullBackWhenCaught = true;
        [SerializeField] private bool _jumpWhenCaught = false;
        [SerializeField] private bool _followWhenCatching = true;
        [SerializeField] private bool _repressWhenCaught = false;
        [SerializeField] private float _repressDelay = 0.5f;

        private CombatEntity _entity;
        private HandshakeParticipant _participant;
        private HandshakeParticipant _targetPlayer;
        private float _timer = 0f;
        private float _holdTimer = 0f;
        private float _gripTimer = 0f;
        private float _postGripTimer = 0f;
        private float _tapTimer = 0f;
        private int _tapsDone = 0;
        private bool _tapLowFrame = false;
        private float _caughtTimer = 0f;
        private float _catchingTimer = 0f;
        private bool _isHoldingInteract = false;
        private bool _hasCompletedHoldCycle = false;
        private HandshakeState _lastParticipantState = HandshakeState.None;

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
        public int ContestTapTarget { get => _contestTapTarget; set => _contestTapTarget = value; }
        public float ContestTapInterval { get => _contestTapInterval; set => _contestTapInterval = value; }
        public float HoldingReleaseDelay { get => _holdingReleaseDelay; set => _holdingReleaseDelay = value; }
        public bool PullBackWhenCaught { get => _pullBackWhenCaught; set => _pullBackWhenCaught = value; }
        public bool JumpWhenCaught { get => _jumpWhenCaught; set => _jumpWhenCaught = value; }
        public bool FollowWhenCatching { get => _followWhenCatching; set => _followWhenCatching = value; }
        public bool RepressWhenCaught { get => _repressWhenCaught; set => _repressWhenCaught = value; }
        public float RepressDelay { get => _repressDelay; set => _repressDelay = value; }

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
            _lastParticipantState = HandshakeState.None;
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

                case HandshakeAiMode.ContestWin:
                    _holdDuration = -1f;
                    _contestTapTarget = 5;
                    _contestTapInterval = 0.08f;
                    _holdingReleaseDelay = 0.15f;
                    _walkIn = true;
                    break;

                case HandshakeAiMode.ContestLose:
                    _holdDuration = -1f;
                    _contestTapTarget = 0;
                    _contestTapInterval = 0.2f;
                    _holdingReleaseDelay = 0.8f;
                    _walkIn = true;
                    break;

                case HandshakeAiMode.ContestTie:
                    _holdDuration = -1f;
                    _contestTapTarget = 2;
                    _contestTapInterval = 0.12f;
                    _holdingReleaseDelay = 0.8f;
                    _walkIn = true;
                    break;

                case HandshakeAiMode.ContestZeroZero:
                    _holdDuration = -1f;
                    _contestTapTarget = 0;
                    _contestTapInterval = 0.2f;
                    _holdingReleaseDelay = 0.15f;
                    _walkIn = true;
                    break;

                case HandshakeAiMode.AiCatchPlayer:
                    _holdDuration = -1f;
                    _contestTapTarget = 0;
                    _holdingReleaseDelay = 999f;
                    _followWhenCatching = true;
                    _walkIn = true;
                    break;

                case HandshakeAiMode.AiCaughtByPlayer:
                    _holdDuration = -1f;
                    _contestTapTarget = 0;
                    _holdingReleaseDelay = 0.05f;
                    _pullBackWhenCaught = true;
                    _jumpWhenCaught = false;
                    _walkIn = true;
                    break;

                case HandshakeAiMode.AiEscape:
                    _holdDuration = -1f;
                    _contestTapTarget = 0;
                    _holdingReleaseDelay = 0.05f;
                    _pullBackWhenCaught = true;
                    _jumpWhenCaught = true;
                    _walkIn = true;
                    break;

                case HandshakeAiMode.AiChaseCatch:
                    _holdDuration = -1f;
                    _contestTapTarget = 0;
                    _holdingReleaseDelay = 999f;
                    _followWhenCatching = true;
                    _walkIn = true;
                    break;

                case HandshakeAiMode.AiJumpNoEscape:
                    _holdDuration = -1f;
                    _contestTapTarget = 0;
                    _holdingReleaseDelay = 0.05f;
                    _pullBackWhenCaught = false;
                    _jumpWhenCaught = true;
                    _walkIn = true;
                    break;

                case HandshakeAiMode.AiCatchAndRelease:
                    _holdDuration = -1f;
                    _contestTapTarget = 0;
                    _holdingReleaseDelay = 999f;
                    _followWhenCatching = false;
                    _walkIn = true;
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
            bool isContestOrCatchMode = _mode >= HandshakeAiMode.ContestWin;

            if (_mode == HandshakeAiMode.AiResponder || isContestOrCatchMode)
            {
                bool playerReady = (_targetPlayer.State == HandshakeState.Reaching || _targetPlayer.State == HandshakeState.Waiting ||
                                    _targetPlayer.State == HandshakeState.Caught || _targetPlayer.State == HandshakeState.Catching ||
                                    _participant.State == HandshakeState.Grip || _participant.State == HandshakeState.Shaking ||
                                    _participant.State == HandshakeState.Holding || _participant.State == HandshakeState.Catching);
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

            if (_isHoldingInteract && _participant.State != HandshakeState.Holding)
            {
                _holdTimer += Time.deltaTime;
                if (_holdDuration > 0f && _holdTimer >= _holdDuration)
                {
                    _isHoldingInteract = false;
                    _hasCompletedHoldCycle = true;
                }
            }

            if (_participant.State == HandshakeState.Holding)
            {
                if (_lastParticipantState != HandshakeState.Holding)
                {
                    _holdTimer = 0f;
                    _tapTimer = 0f;
                    _tapsDone = 0;
                    _tapLowFrame = false;
                }

                if (_tapsDone < _contestTapTarget)
                {
                    _tapTimer += Time.deltaTime;
                    if (_tapLowFrame)
                    {
                        _isHoldingInteract = true;
                        _tapLowFrame = false;
                        _tapsDone++;
                        _tapTimer = 0f;
                    }
                    else if (_tapTimer >= _contestTapInterval)
                    {
                        _isHoldingInteract = false;
                        _tapLowFrame = true;
                    }
                }
                else
                {
                    _holdTimer += Time.deltaTime;
                    if (_holdTimer >= _holdingReleaseDelay)
                    {
                        _isHoldingInteract = false;
                        _hasCompletedHoldCycle = true;
                    }
                    else
                    {
                        _isHoldingInteract = true;
                    }
                }
            }
            else if (_participant.State == HandshakeState.Caught)
            {
                if (_pullBackWhenCaught)
                {
                    Vector3 away = -toTarget.normalized;
                    intent.MoveInput = new Vector2(away.x, away.z);
                }

                _caughtTimer += Time.deltaTime;
                if (_jumpWhenCaught && _caughtTimer >= 0.1f)
                {
                    intent.Jump = true;
                    _jumpWhenCaught = false;
                }

                if (_repressWhenCaught && _caughtTimer >= _repressDelay)
                {
                    _isHoldingInteract = true;
                }
            }
            else if (_participant.State == HandshakeState.Catching)
            {
                _catchingTimer += Time.deltaTime;

                if (_mode == HandshakeAiMode.AiCatchAndRelease && _catchingTimer >= 0.25f)
                {
                    _isHoldingInteract = false;
                    _hasCompletedHoldCycle = true;
                }
                else
                {
                    _isHoldingInteract = true;
                    if (_followWhenCatching && distance > 0.45f)
                    {
                        Vector3 toward = toTarget.normalized;
                        intent.MoveInput = new Vector2(toward.x, toward.z);
                    }
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
            _lastParticipantState = _participant.State;
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
            _tapTimer = 0f;
            _tapsDone = 0;
            _tapLowFrame = false;
            _caughtTimer = 0f;
            _catchingTimer = 0f;
            _isHoldingInteract = false;
            _hasCompletedHoldCycle = false;
            _lastParticipantState = HandshakeState.None;
        }
    }
}
