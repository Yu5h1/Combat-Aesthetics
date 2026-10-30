using System;
using UnityEngine;
using Yu5h1Lib;

namespace Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom
{
    /// <summary>
    /// Local player input adapter that polls keyboard/mouse through <see cref="InputHandler"/> and drives a <see cref="CombatEntity"/>.
    /// Supports configurable interact keys for both Player 1 and Player 2 schemes.
    /// </summary>
    [RequireComponent(typeof(CombatEntity))]
    public class WhiteRoomCharacter : MonoBehaviour, ICombatInputSource
    {
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private bool _usePlayer2ControlScheme = false;
        [SerializeField] private KeyCode _interactKey = KeyCode.E;
        [SerializeField] private KeyCode _player2InteractKey = KeyCode.Keypad3;

        public KeyCode InteractKey { get => _interactKey; set => _interactKey = value; }
        public KeyCode Player2InteractKey { get => _player2InteractKey; set => _player2InteractKey = value; }
        public KeyCode ActiveInteractKey
        {
            get => _usePlayer2ControlScheme ? _player2InteractKey : _interactKey;
            set
            {
                if (_usePlayer2ControlScheme) _player2InteractKey = value;
                else _interactKey = value;
            }
        }

        public bool UsePlayer2ControlScheme => _usePlayer2ControlScheme;
        public bool OverrideIntent { get; set; } = false;
        public CombatActionIntent CustomIntent { get; set; } = CombatActionIntent.Empty;

        private CombatEntity _entity;

        public static bool IsUIFocused { get; set; } = false;

        private float _p1RmbDownTime = -1f;
        private bool _p1RmbWasDown = false;

        private float _p2GuardDownTime = -1f;
        private bool _p2GuardWasDown = false;

        private const float ParryTapThreshold = 0.22f;
        private const float GuardHoldThreshold = 0.16f;

        public CombatEntity Entity => _entity;
        public CombatState CurrentState => _entity != null ? _entity.State : CombatState.Idle;
        public bool IsCrouching => _entity != null && _entity.IsCrouching;
        public bool IsGuarding => _entity != null && _entity.IsGuarding;
        public float Health => _entity != null ? _entity.CurrentHealth : 0f;
        public float MaxHealth => _entity != null ? _entity.MaxHealth : 100f;

        public event Action<string> OnCombatAction
        {
            add { if (_entity != null) _entity.OnCombatLog += value; }
            remove { if (_entity != null) _entity.OnCombatLog -= value; }
        }

        private void Awake()
        {
            _entity = GetComponent<CombatEntity>();
        }

        private void Start()
        {
            if (_cameraTransform == null && Camera.main != null)
            {
                _cameraTransform = Camera.main.transform;
            }
        }

        private void Update()
        {
            if (_entity == null) return;

            CombatActionIntent intent = OverrideIntent ? CustomIntent : PollIntent(_entity);
            _entity.StepCombat(intent, _cameraTransform);
        }

        /// <summary>
        /// Polls the current frame's movement, combat, and interact inputs for the configured player scheme.
        /// </summary>
        public CombatActionIntent PollIntent(CombatEntity entity)
        {
            CombatActionIntent intent = CombatActionIntent.Empty;
            intent.Timestamp = Time.time;

            intent.GlobalReset = InputHandler.GetKeyDown(KeyCode.R);

            if (IsUIFocused)
            {
                return intent;
            }

            if (_usePlayer2ControlScheme)
            {
                Vector2 move = Vector2.zero;
                if (InputHandler.GetKey(KeyCode.UpArrow)) move.y += 1f;
                if (InputHandler.GetKey(KeyCode.DownArrow)) move.y -= 1f;
                if (InputHandler.GetKey(KeyCode.LeftArrow)) move.x -= 1f;
                if (InputHandler.GetKey(KeyCode.RightArrow)) move.x += 1f;
                intent.MoveInput = Vector2.ClampMagnitude(move, 1f);

                intent.Jump = InputHandler.GetKeyDown(KeyCode.RightControl) || InputHandler.GetKeyDown(KeyCode.Keypad0);
                intent.CrouchHold = InputHandler.GetKey(KeyCode.DownArrow) && InputHandler.GetKey(KeyCode.RightShift);
                intent.CrouchToggle = InputHandler.GetKeyDown(KeyCode.KeypadPeriod);
                intent.Attack = InputHandler.GetKeyDown(KeyCode.Keypad1) || InputHandler.GetKeyDown(KeyCode.Return);

                intent.InteractHold = InputHandler.GetKey(_player2InteractKey);
                intent.InteractDown = InputHandler.GetKeyDown(_player2InteractKey);
                intent.InteractUp = InputHandler.GetKeyUp(_player2InteractKey);

                bool p2GuardKey = InputHandler.GetKey(KeyCode.Keypad2) || InputHandler.GetKey(KeyCode.RightAlt);
                bool p2GuardKeyDown = InputHandler.GetKeyDown(KeyCode.Keypad2) || InputHandler.GetKeyDown(KeyCode.RightAlt);
                bool p2GuardKeyUp = InputHandler.GetKeyUp(KeyCode.Keypad2) || InputHandler.GetKeyUp(KeyCode.RightAlt);

                if (p2GuardKeyDown)
                {
                    _p2GuardDownTime = Time.time;
                    _p2GuardWasDown = true;
                }

                if (p2GuardKeyUp && _p2GuardWasDown)
                {
                    if (Time.time - _p2GuardDownTime <= ParryTapThreshold)
                    {
                        intent.ParryTap = true;
                    }
                    _p2GuardWasDown = false;
                    _p2GuardDownTime = -1f;
                }
                else if (p2GuardKey && _p2GuardWasDown)
                {
                    if (Time.time - _p2GuardDownTime > GuardHoldThreshold)
                    {
                        intent.GuardHold = true;
                    }
                }
            }
            else
            {
                Vector2 move = Vector2.zero;
                if (InputHandler.GetKey(KeyCode.W)) move.y += 1f;
                if (InputHandler.GetKey(KeyCode.S)) move.y -= 1f;
                if (InputHandler.GetKey(KeyCode.A)) move.x -= 1f;
                if (InputHandler.GetKey(KeyCode.D)) move.x += 1f;
                intent.MoveInput = Vector2.ClampMagnitude(move, 1f);

                intent.Jump = InputHandler.GetKeyDown(KeyCode.Space);
                intent.CrouchHold = InputHandler.GetKey(KeyCode.LeftControl);
                intent.CrouchToggle = InputHandler.GetKeyDown(KeyCode.C);

                intent.Attack = InputHandler.GetMouseButtonDown(0);

                intent.InteractHold = InputHandler.GetKey(_interactKey);
                intent.InteractDown = InputHandler.GetKeyDown(_interactKey);
                intent.InteractUp = InputHandler.GetKeyUp(_interactKey);

                bool rmbKey = InputHandler.GetMouseButton(1);
                bool rmbDown = InputHandler.GetMouseButtonDown(1);
                bool rmbUp = InputHandler.GetMouseButtonUp(1);

                if (rmbDown)
                {
                    _p1RmbDownTime = Time.time;
                    _p1RmbWasDown = true;
                }

                if (rmbUp && _p1RmbWasDown)
                {
                    if (Time.time - _p1RmbDownTime <= ParryTapThreshold)
                    {
                        intent.ParryTap = true;
                    }
                    _p1RmbWasDown = false;
                    _p1RmbDownTime = -1f;
                }
                else if (rmbKey && _p1RmbWasDown)
                {
                    if (Time.time - _p1RmbDownTime > GuardHoldThreshold)
                    {
                        intent.GuardHold = true;
                    }
                }
            }

            return intent;
        }

        /// <summary>
        /// Sets whether this character uses the Player 2 keyboard control scheme.
        /// </summary>
        public void SetControlScheme(bool isPlayer2)
        {
            _usePlayer2ControlScheme = isPlayer2;
        }

        /// <summary>
        /// Forwards a hit event to the underlying <see cref="CombatEntity"/>.
        /// </summary>
        public void TakeHit(float damage, Vector3 knockback, Vector3 attackerPos)
        {
            if (_entity != null)
            {
                CombatEntity dummyAttacker = ObjectUtility.FindObject<CombatEntity>(false);
                _entity.TakeHit(dummyAttacker, damage, knockback);
            }
        }

        /// <summary>
        /// Resets the underlying <see cref="CombatEntity"/>.
        /// </summary>
        public void ResetCharacter()
        {
            if (_entity != null)
            {
                _entity.ResetEntity();
            }
        }
    }
}
