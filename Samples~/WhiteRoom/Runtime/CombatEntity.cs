using System;
using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom
{
    /// <summary>
    /// Enumerates the combat and locomotion states of a <see cref="CombatEntity"/>.
    /// </summary>
    public enum CombatState
    {
        Idle,
        Moving,
        Jumping,
        Crouching,
        AttackStartup,
        AttackActive,
        AttackRecovery,
        Guarding,
        Parrying,
        Flinching,
        Stunned,
        Incapacitated
    }

    /// <summary>
    /// Shared character entity handling locomotion, jumping, gravity, combat windows, and global arena reset.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class CombatEntity : MonoBehaviour
    {
        private static readonly List<CombatEntity> s_ActiveEntities = new List<CombatEntity>();
        public static IReadOnlyList<CombatEntity> ActiveEntities => s_ActiveEntities;

        [Header("Identity & Team")]
        [SerializeField] private string _entityId = "Entity";
        [SerializeField] private int _teamId = 0;
        [SerializeField] private bool _enableCombatActions = true;

        [Header("Health & Stats")]
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _currentHealth = 100f;

        [Header("Locomotion Settings")]
        [SerializeField] private float _walkSpeed = 5.0f;
        [SerializeField] private float _guardSpeed = 2.2f;
        [SerializeField] private float _crouchSpeed = 2.5f;
        [SerializeField] private float _turnSpeed = 720f;
        [SerializeField] private float _jumpHeight = 1.5f;
        [SerializeField] private float _gravity = -20f;

        [Header("Crouch Collider Settings")]
        [SerializeField] private float _standingHeight = 2.0f;
        [SerializeField] private float _crouchingHeight = 1.0f;
        [SerializeField] private float _colliderRadius = 0.5f;
        [SerializeField] private Vector3 _standingCenter = new Vector3(0, 1.0f, 0);
        [SerializeField] private Vector3 _crouchingCenter = new Vector3(0, 0.5f, 0);

        [Header("Attack Windows & Parameters")]
        [SerializeField] private float _attackStartupTime = 0.20f;
        [SerializeField] private float _attackActiveTime = 0.15f;
        [SerializeField] private float _attackRecoveryTime = 0.25f;
        [SerializeField] private float _attackRange = 1.5f;
        [SerializeField] private float _attackDamage = 20f;
        [SerializeField] private float _attackKnockback = 7.5f;

        [Header("Defense Parameters")]
        [SerializeField] private float _parryWindow = 0.18f;
        [SerializeField] private float _parryStunDuration = 1.0f;

        private CharacterController _controller;
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;
        private Vector3 _residualVelocity;
        private float _verticalVelocity;
        private bool _wasGrounded = true;
        private CombatState _state = CombatState.Idle;
        private float _stateTimer = 0f;
        private float _guardStartTime = -1f;
        private bool _isCrouching = false;
        private bool _hitRegisteredInCurrentAttack = false;
        private CombatActionIntent _currentIntent;

        public Transform VisualRoot { get; set; }
        public Transform FistTransform { get; set; }
        public bool LocomotionLocked { get; set; }
        public float SpeedOverride { get; set; } = -1f;
        public CombatActionIntent CurrentIntent => _currentIntent;
        public bool IsGrounded => _controller != null ? _controller.isGrounded : _wasGrounded;

        public bool EnableCombatActions
        {
            get => _enableCombatActions;
            set => _enableCombatActions = value;
        }

        public static event Action OnGlobalArenaReset;
        public event Action<string> OnCombatLog;
        public event Action<CombatEntity, float, float> OnHealthChanged;
        public event Action<CombatEntity, CombatState> OnStateChanged;
        public event Action<CombatActionIntent> OnIntentStepped;
        public event Action OnEntityReset;

        public string EntityId => _entityId;
        public int TeamId => _teamId;
        public float MaxHealth => _maxHealth;
        public float CurrentHealth => _currentHealth;
        public CombatState State => _state;
        public bool IsCrouching => _isCrouching;
        public bool IsIncapacitated => _state == CombatState.Incapacitated;
        public bool IsGuarding => _state == CombatState.Guarding || _state == CombatState.Parrying;
        public float GuardSpeed => _guardSpeed;
        public float WalkSpeed => _walkSpeed;

        protected virtual void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (_controller != null)
            {
                _controller.height = _standingHeight;
                _controller.center = _standingCenter;
                _controller.radius = _colliderRadius;
                _controller.minMoveDistance = 0f;
            }
            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;
            _currentHealth = _maxHealth;
        }

        protected virtual void OnEnable()
        {
            if (!s_ActiveEntities.Contains(this)) s_ActiveEntities.Add(this);
        }

        protected virtual void OnDisable()
        {
            s_ActiveEntities.Remove(this);
        }

        /// <summary>
        /// Emits a formatted message to the combat/interaction log stream.
        /// </summary>
        public void EmitLog(string message)
        {
            OnCombatLog?.Invoke(message);
        }

        /// <summary>
        /// Configures the CharacterController radius for specific scenario requirements such as close interaction.
        /// </summary>
        public void SetColliderRadius(float radius)
        {
            _colliderRadius = radius;
            if (_controller != null)
            {
                _controller.radius = radius;
                _controller.minMoveDistance = 0f;
            }
        }

        /// <summary>
        /// Initiates a vertical jump impulse when grounded.
        /// </summary>
        public void Jump()
        {
            if (_wasGrounded)
            {
                _verticalVelocity = Mathf.Sqrt(2f * _jumpHeight * Mathf.Abs(_gravity));
                _wasGrounded = false;
                SetState(CombatState.Jumping);
                OnCombatLog?.Invoke($"[{_entityId}] 起跳！");
            }
        }

        /// <summary>
        /// Advances one simulation step for this entity using the supplied action intent.
        /// </summary>
        public void StepCombat(CombatActionIntent intent, Transform cameraTransform = null)
        {
            _currentIntent = intent;

            if (intent.GlobalReset)
            {
                ResetAllArenaEntities();
                return;
            }

            OnIntentStepped?.Invoke(intent);

            if (_state == CombatState.Incapacitated)
            {
                HandleLocomotion(CombatActionIntent.Empty, null);
                return;
            }

            if (_enableCombatActions)
            {
                HandleDefenseInput(intent);
                HandleCrouchInput(intent);
                UpdateAttackTimeline();

                if (intent.Attack && CanInitiateAttack())
                {
                    InitiateAttack(intent.MoveInput, cameraTransform);
                }

                UpdateFlinchAndStunTimers();
            }

            HandleLocomotion(intent, cameraTransform);
        }

        private void HandleDefenseInput(CombatActionIntent intent)
        {
            if (_state == CombatState.AttackStartup || _state == CombatState.AttackActive ||
                _state == CombatState.Flinching || _state == CombatState.Stunned)
            {
                return;
            }

            if (intent.ParryTap)
            {
                _guardStartTime = Time.time;
                SetState(CombatState.Parrying);
                _stateTimer = _parryWindow;
                OnCombatLog?.Invoke($"[{_entityId}] 進入精準彈刀姿態（{_parryWindow:F2}s 時窗）");
            }
            else if (intent.GuardHold)
            {
                if (_state != CombatState.Guarding && _state != CombatState.Parrying)
                {
                    _guardStartTime = Time.time;
                    SetState(CombatState.Guarding);
                    OnCombatLog?.Invoke($"[{_entityId}] 進入正面防禦姿態（可警戒慢步）");
                }
            }
            else
            {
                if (_state == CombatState.Guarding)
                {
                    SetState(_isCrouching ? CombatState.Crouching : CombatState.Idle);
                    _guardStartTime = -1f;
                }
            }
        }

        private void HandleCrouchInput(CombatActionIntent intent)
        {
            if (_state == CombatState.AttackStartup || _state == CombatState.AttackActive ||
                _state == CombatState.Flinching || _state == CombatState.Stunned)
            {
                return;
            }

            bool wantCrouch = _isCrouching;
            if (intent.CrouchToggle)
            {
                wantCrouch = !_isCrouching;
            }
            else if (intent.CrouchHold)
            {
                wantCrouch = true;
            }
            else if (!intent.CrouchHold && _isCrouching && !intent.CrouchToggle)
            {
                wantCrouch = false;
            }

            if (wantCrouch != _isCrouching && _controller.isGrounded)
            {
                _isCrouching = wantCrouch;
                _controller.height = _isCrouching ? _crouchingHeight : _standingHeight;
                _controller.center = _isCrouching ? _crouchingCenter : _standingCenter;
                OnCombatLog?.Invoke($"[{_entityId}] {(_isCrouching ? "進入蹲下姿態" : "恢復站立姿態")}");
            }
        }

        private bool CanInitiateAttack()
        {
            return !LocomotionLocked && (_state == CombatState.Idle || _state == CombatState.Moving || _state == CombatState.Crouching);
        }

        private void InitiateAttack(Vector2 moveInput, Transform cameraTransform)
        {
            if (moveInput.sqrMagnitude > 0.05f)
            {
                Vector3 fwd = transform.forward;
                if (cameraTransform != null)
                {
                    Vector3 camFwd = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
                    Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
                    fwd = (camFwd * moveInput.y + camRight * moveInput.x).normalized;
                }
                transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
                OnCombatLog?.Invoke($"[{_entityId}] 發起【方向攻擊】（蓄力前搖 {_attackStartupTime:F2}s）");
            }
            else
            {
                OnCombatLog?.Invoke($"[{_entityId}] 發起【原地攻擊】（蓄力前搖 {_attackStartupTime:F2}s）");
            }

            SetState(CombatState.AttackStartup);
            _stateTimer = _attackStartupTime;
            _hitRegisteredInCurrentAttack = false;
        }

        private void UpdateAttackTimeline()
        {
            if (_state == CombatState.AttackStartup)
            {
                _stateTimer -= Time.deltaTime;
                if (_stateTimer <= 0f)
                {
                    SetState(CombatState.AttackActive);
                    _stateTimer = _attackActiveTime;
                    PerformHitCheck();
                }
            }
            else if (_state == CombatState.AttackActive)
            {
                if (!_hitRegisteredInCurrentAttack)
                {
                    PerformHitCheck();
                }

                _stateTimer -= Time.deltaTime;
                if (_stateTimer <= 0f)
                {
                    SetState(CombatState.AttackRecovery);
                    _stateTimer = _attackRecoveryTime;
                }
            }
            else if (_state == CombatState.AttackRecovery)
            {
                _stateTimer -= Time.deltaTime;
                if (_stateTimer <= 0f)
                {
                    SetState(_isCrouching ? CombatState.Crouching : CombatState.Idle);
                }
            }
        }

        private void PerformHitCheck()
        {
            if (_hitRegisteredInCurrentAttack) return;

            Vector3 hitCenter = transform.position + Vector3.up * 1.1f + transform.forward * (_attackRange * 0.7f);
            Collider[] colliders = Physics.OverlapSphere(hitCenter, _attackRange * 0.6f);

            foreach (var col in colliders)
            {
                if (col.gameObject == gameObject) continue;

                CombatEntity target = col.GetComponent<CombatEntity>();
                if (target != null && target.TeamId != _teamId && !target.IsIncapacitated)
                {
                    _hitRegisteredInCurrentAttack = true;
                    Vector3 knockbackDir = (target.transform.position - transform.position).normalized;
                    knockbackDir.y = 0.15f;
                    Vector3 impulse = knockbackDir.normalized * _attackKnockback;

                    target.TakeHit(this, _attackDamage, impulse);
                    break;
                }
            }
        }

        /// <summary>
        /// Evaluates incoming hit damage against guard and parry states and applies knockback.
        /// </summary>
        public void TakeHit(CombatEntity attacker, float damage, Vector3 knockback)
        {
            if (_state == CombatState.Incapacitated) return;

            Vector3 toAttacker = (attacker.transform.position - transform.position).normalized;
            float facingDot = Vector3.Dot(transform.forward, toAttacker);

            if (IsGuarding && facingDot > 0.3f)
            {
                if (_state == CombatState.Parrying)
                {
                    _stateTimer = 0.3f;
                    OnCombatLog?.Invoke($"【精準防禦 (Parry) 成功！】[{_entityId}] 完美化解傷害，並擊暈反制 [{attacker.EntityId}]！");
                    attacker.InterruptAttack(_parryStunDuration);
                    return;
                }
                else if (_state == CombatState.Guarding)
                {
                    damage *= 0.20f;
                    knockback *= 0.30f;
                    _residualVelocity += knockback;
                    ApplyDamage(damage);
                    OnCombatLog?.Invoke($"【普通防禦】[{_entityId}] 格擋承受 {damage:F1} 傷害。");
                    return;
                }
            }

            _residualVelocity += knockback;
            SetState(CombatState.Flinching);
            _stateTimer = 0.35f;
            ApplyDamage(damage);
            OnCombatLog?.Invoke($"【直擊受創】[{_entityId}] 受到 {damage:F1} 點重擊！");
        }

        /// <summary>
        /// Interrupts an active attack and places this entity into the stunned state.
        /// </summary>
        public void InterruptAttack(float stunDuration)
        {
            _hitRegisteredInCurrentAttack = true;
            SetState(CombatState.Stunned);
            _stateTimer = stunDuration;
            OnCombatLog?.Invoke($"【攻擊被中斷！】[{_entityId}] 招式遭對手彈開，陷入暈眩 {stunDuration:F1} 秒！");
        }

        private void ApplyDamage(float damage)
        {
            _currentHealth = Mathf.Max(0f, _currentHealth - damage);
            OnHealthChanged?.Invoke(this, _currentHealth, _maxHealth);

            if (_currentHealth <= 0f)
            {
                SetState(CombatState.Incapacitated);
                OnCombatLog?.Invoke($"【失去戰力 (KO)】[{_entityId}] 生命值歸零，倒下！");
            }
        }

        private void UpdateFlinchAndStunTimers()
        {
            if (_state == CombatState.Flinching || _state == CombatState.Stunned || _state == CombatState.Parrying)
            {
                _stateTimer -= Time.deltaTime;
                if (_stateTimer <= 0f)
                {
                    SetState(_isCrouching ? CombatState.Crouching : CombatState.Idle);
                }
            }
        }

        private void HandleLocomotion(CombatActionIntent intent, Transform cameraTransform)
        {
            Vector3 moveDir = Vector3.zero;
            Vector2 input = intent.MoveInput;

            bool canMove = !LocomotionLocked && (_state == CombatState.Idle || _state == CombatState.Moving || _state == CombatState.Crouching || _state == CombatState.Guarding || _state == CombatState.Jumping);
            if (canMove && input.sqrMagnitude > 0.01f)
            {
                if (cameraTransform != null)
                {
                    Vector3 camFwd = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
                    Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
                    moveDir = (camFwd * input.y + camRight * input.x).normalized;
                }
                else
                {
                    moveDir = new Vector3(input.x, 0, input.y).normalized;
                }

                if (moveDir.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, _turnSpeed * Time.deltaTime);
                }
            }

            float currentSpeed = _isCrouching ? _crouchSpeed : _walkSpeed;
            if (SpeedOverride >= 0f)
            {
                currentSpeed = SpeedOverride;
            }
            else if (_state == CombatState.Guarding)
            {
                currentSpeed = _guardSpeed;
            }
            else if (_state == CombatState.Jumping)
            {
                currentSpeed = _walkSpeed * 0.85f;
            }
            else if (_state == CombatState.AttackStartup || _state == CombatState.AttackActive)
            {
                currentSpeed *= 0.15f;
            }

            if (!canMove || _state == CombatState.Parrying || _state == CombatState.Flinching || _state == CombatState.Stunned || _state == CombatState.Incapacitated)
            {
                currentSpeed = 0f;
            }

            Vector3 driveVelocity = moveDir * (currentSpeed * Mathf.Clamp01(input.magnitude));

            if (_wasGrounded)
            {
                _verticalVelocity = -2f;

                if (intent.Jump && canMove && !_isCrouching && _state != CombatState.Guarding)
                {
                    Jump();
                }
            }
            else
            {
                _verticalVelocity += _gravity * Time.deltaTime;
                if (_verticalVelocity < -2.5f && _state != CombatState.Flinching && _state != CombatState.Stunned && _state != CombatState.Incapacitated)
                {
                    SetState(CombatState.Jumping);
                }
            }

            _residualVelocity = Vector3.Lerp(_residualVelocity, Vector3.zero, 5f * Time.deltaTime);

            Vector3 totalVelocity = driveVelocity + _residualVelocity;
            totalVelocity.y = _verticalVelocity;

            if (_controller.enabled)
            {
                _controller.Move(totalVelocity * Time.deltaTime);
            }

            bool nowGrounded = _controller.isGrounded;
            if (nowGrounded)
            {
                _verticalVelocity = -2f;
                if (!_wasGrounded)
                {
                    _wasGrounded = true;
                    if (_state == CombatState.Jumping)
                    {
                        if (_isCrouching)
                            SetState(CombatState.Crouching);
                        else if (canMove && input.sqrMagnitude > 0.05f)
                            SetState(CombatState.Moving);
                        else
                            SetState(CombatState.Idle);
                        OnCombatLog?.Invoke($"[{_entityId}] 著地");
                    }
                }
                else if (_state == CombatState.Idle || _state == CombatState.Moving)
                {
                    if (_isCrouching)
                        SetState(CombatState.Crouching);
                    else if (canMove && input.sqrMagnitude > 0.05f)
                        SetState(CombatState.Moving);
                    else
                        SetState(CombatState.Idle);
                }
            }
            else
            {
                _wasGrounded = false;
                if (_verticalVelocity < -2.5f && _state != CombatState.Flinching && _state != CombatState.Stunned && _state != CombatState.Incapacitated)
                {
                    SetState(CombatState.Jumping);
                }
            }
        }

        private void SetState(CombatState newState)
        {
            if (_state != newState)
            {
                _state = newState;
                OnStateChanged?.Invoke(this, _state);
            }
        }

        /// <summary>
        /// Resets all active arena entities and notifies subscribers of the global arena reset.
        /// </summary>
        public static void ResetAllArenaEntities()
        {
            foreach (var entity in s_ActiveEntities)
            {
                if (entity != null)
                {
                    entity.ResetEntity();
                }
            }
            OnGlobalArenaReset?.Invoke();
        }

        /// <summary>
        /// Restores this entity to its initial spawn transform, full health, and idle state.
        /// </summary>
        public void ResetEntity()
        {
            if (_controller != null) _controller.enabled = false;
            transform.position = _spawnPosition;
            transform.rotation = _spawnRotation;
            Physics.SyncTransforms();
            if (_controller != null) _controller.enabled = true;

            _currentHealth = _maxHealth;
            _residualVelocity = Vector3.zero;
            _verticalVelocity = -2f;
            _wasGrounded = true;
            _isCrouching = false;
            _stateTimer = 0f;
            _guardStartTime = -1f;
            _hitRegisteredInCurrentAttack = false;
            _currentIntent = CombatActionIntent.Empty;
            LocomotionLocked = false;
            SpeedOverride = -1f;

            if (_controller != null)
            {
                _controller.height = _standingHeight;
                _controller.center = _standingCenter;
                _controller.radius = _colliderRadius;
                _controller.minMoveDistance = 0f;
            }

            SetState(CombatState.Idle);
            OnHealthChanged?.Invoke(this, _currentHealth, _maxHealth);
            OnEntityReset?.Invoke();
            OnCombatLog?.Invoke($"[{_entityId}] 狀態重設完成");
        }

        /// <summary>
        /// Initializes the identity, team, and health parameters of this entity.
        /// </summary>
        public void InitializeSettings(string id, int team, float maxHp)
        {
            _entityId = id;
            _teamId = team;
            _maxHealth = maxHp;
            _currentHealth = maxHp;
            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;
        }
    }
}
