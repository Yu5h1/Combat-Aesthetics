using UnityEngine;

namespace Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom
{
    /// <summary>
    /// AI combat opponent that drives a <see cref="CombatEntity"/> using the same intent and state rules as players.
    /// </summary>
    [RequireComponent(typeof(CombatEntity))]
    public class WhiteRoomDummy : MonoBehaviour, ICombatInputSource
    {
        [Header("AI Behavior Settings")]
        [SerializeField] private bool _autoEngage = true;
        [SerializeField] private float _attackInterval = 3.5f;
        [SerializeField] private float _desiredDistance = 1.4f;

        [Header("AI Defense Settings")]
        [SerializeField] private bool _enableDefense = true;
        [Range(0f, 1f)]
        [SerializeField] private float _guardProbability = 0.60f;
        [Range(0f, 1f)]
        [SerializeField] private float _parryProbability = 0.30f;
        [SerializeField] private float _defenseHoldDuration = 0.85f;

        private CombatEntity _entity;
        private CombatEntity _targetPlayer;
        private float _actionCooldown = 1.5f;
        private float _defenseTimer = 0f;
        private bool _isAiGuarding = false;
        private bool _isAiParrying = false;

        public CombatEntity Entity => _entity;
        public float Health => _entity != null ? _entity.CurrentHealth : 0f;
        public float MaxHealth => _entity != null ? _entity.MaxHealth : 100f;
        public bool IsStunned => _entity != null && _entity.State == CombatState.Stunned;
        public bool IsIncapacitated => _entity != null && _entity.IsIncapacitated;

        private void Awake()
        {
            _entity = GetComponent<CombatEntity>();
        }

        private void OnEnable()
        {
            CombatEntity.OnGlobalArenaReset += ResetDummy;
        }

        private void OnDisable()
        {
            CombatEntity.OnGlobalArenaReset -= ResetDummy;
        }

        private void Start()
        {
            FindTargetPlayer();
        }

        private void Update()
        {
            if (_entity == null) return;

            if (_targetPlayer == null || _targetPlayer.IsIncapacitated)
            {
                FindTargetPlayer();
            }

            CombatActionIntent intent = PollIntent(_entity);
            _entity.StepCombat(intent);
        }

        /// <summary>
        /// Evaluates AI combat decisions and returns the current frame's <see cref="CombatActionIntent"/>.
        /// </summary>
        public CombatActionIntent PollIntent(CombatEntity entity)
        {
            CombatActionIntent intent = CombatActionIntent.Empty;
            intent.Timestamp = Time.time;

            if (_entity.State == CombatState.Stunned || _entity.State == CombatState.Flinching || _entity.State == CombatState.Incapacitated)
            {
                _isAiGuarding = false;
                _isAiParrying = false;
                _defenseTimer = 0f;
                return intent;
            }

            if (!_autoEngage || _targetPlayer == null || _targetPlayer.IsIncapacitated)
            {
                return intent;
            }

            Vector3 toTarget = _targetPlayer.transform.position - transform.position;
            toTarget.y = 0;
            float distance = toTarget.magnitude;

            if (_enableDefense && distance <= _desiredDistance + 0.8f)
            {
                float facingDot = Vector3.Dot(transform.forward, toTarget.normalized);
                bool playerIsAttacking = (_targetPlayer.State == CombatState.AttackStartup);

                if (playerIsAttacking && facingDot > 0.3f && !_isAiGuarding && !_isAiParrying)
                {
                    float roll = UnityEngine.Random.value;
                    if (roll < _parryProbability)
                    {
                        intent.ParryTap = true;
                        _isAiParrying = true;
                        _defenseTimer = 0.25f;
                    }
                    else if (roll < _parryProbability + _guardProbability)
                    {
                        intent.GuardHold = true;
                        _isAiGuarding = true;
                        _defenseTimer = _defenseHoldDuration;
                    }
                }
            }

            if (_defenseTimer > 0f)
            {
                _defenseTimer -= Time.deltaTime;
                if (_isAiGuarding)
                {
                    intent.GuardHold = true;
                }
                if (_defenseTimer <= 0f)
                {
                    _isAiGuarding = false;
                    _isAiParrying = false;
                }
            }

            if (distance > _desiredDistance)
            {
                Vector3 moveDir = toTarget.normalized;
                intent.MoveInput = new Vector2(moveDir.x, moveDir.z);
            }
            else
            {
                intent.MoveInput = Vector2.zero;
                if (toTarget.sqrMagnitude > 0.001f && _entity.State != CombatState.Parrying)
                {
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation,
                        Quaternion.LookRotation(toTarget),
                        360f * Time.deltaTime
                    );
                }

                if (!_isAiGuarding && !_isAiParrying)
                {
                    _actionCooldown -= Time.deltaTime;
                    if (_actionCooldown <= 0f)
                    {
                        _actionCooldown = _attackInterval;
                        intent.Attack = true;
                    }
                }
            }

            return intent;
        }

        private void FindTargetPlayer()
        {
            float nearestDist = float.MaxValue;
            CombatEntity bestTarget = null;

            foreach (var e in CombatEntity.ActiveEntities)
            {
                if (e != null && e.TeamId == 0 && !e.IsIncapacitated)
                {
                    float d = Vector3.Distance(transform.position, e.transform.position);
                    if (d < nearestDist)
                    {
                        nearestDist = d;
                        bestTarget = e;
                    }
                }
            }
            _targetPlayer = bestTarget;
        }

        /// <summary>
        /// Applies hit damage and knockback to this dummy's entity.
        /// </summary>
        public void TakeHit(float damage, Vector3 knockback)
        {
            if (_entity != null)
            {
                CombatEntity attacker = _targetPlayer;
                _entity.TakeHit(attacker, damage, knockback);
            }
        }

        /// <summary>
        /// Interrupts this dummy's attack and stuns it for the specified duration.
        /// </summary>
        public void ApplyParryStun(float duration)
        {
            if (_entity != null)
            {
                _entity.InterruptAttack(duration);
            }
        }

        /// <summary>
        /// Resets AI cooldowns, defense timers, and target selection upon arena reset.
        /// </summary>
        public void ResetDummy()
        {
            if (_entity != null)
            {
                _entity.ResetEntity();
                _actionCooldown = 1.5f;
                _defenseTimer = 0f;
                _isAiGuarding = false;
                _isAiParrying = false;
                FindTargetPlayer();
            }
        }
    }
}
