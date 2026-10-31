using UnityEngine;

namespace Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom.Handshake
{
    /// <summary>
    /// Presentation-only component that plays the five Stage 2b humanoid handshake clips on a humanoid <see cref="Animator"/>
    /// and applies runtime humanoid IK (<c>OnAnimatorIK</c>) to align the right hand visually toward the opponent's hand.
    /// Does not modify or participate in contact or outcome arbitration.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class HandshakeVisualFeedback : MonoBehaviour
    {
        [SerializeField] private HandshakeParticipant _participant;
        [SerializeField] private HandshakeParticipant _opponent;
        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _preIkWristTransform;
        [SerializeField] private Transform _contactProxyTransform;
        [SerializeField] private Vector3 _fistLocalOffsetInWrist = new Vector3(0.08f, 0f, 0f);
        [SerializeField] private Renderer _skinnedRenderer;
        [SerializeField] private Color _defaultBodyColor = new Color(0.88f, 0.92f, 0.96f);

        private HandshakeState _lastPlayedState = (HandshakeState)(-1);
        private float _currentIkWeight = 0f;
        private Vector3 _currentIkTarget;
        private Material _instanceMaterial;

        private static readonly int s_BaseColorFactorId = Shader.PropertyToID("baseColorFactor");
        private static readonly int s_BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int s_ColorId = Shader.PropertyToID("_Color");

        public HandshakeParticipant Opponent { get => _opponent; set => _opponent = value; }

        /// <summary>
        /// Binds the participant, opponent, animator, and pre-IK contact proxy references.
        /// </summary>
        public void BindHumanoid(
            HandshakeParticipant participant,
            Animator animator,
            Transform preIkWrist,
            Transform contactProxy,
            Renderer skinnedRenderer,
            Color baseColor)
        {
            _participant = participant;
            _animator = animator;
            _preIkWristTransform = preIkWrist;
            _contactProxyTransform = contactProxy;
            _skinnedRenderer = skinnedRenderer;
            _defaultBodyColor = baseColor;

            if (_skinnedRenderer != null)
            {
                _instanceMaterial = _skinnedRenderer.material;
                ApplyColor(_defaultBodyColor);
            }

            if (_participant != null)
            {
                _participant.OnStateChanged -= HandleStateChanged;
                _participant.OnStateChanged += HandleStateChanged;
            }

            PlayClipForState(HandshakeState.None);
        }

        private void OnDestroy()
        {
            if (_participant != null)
            {
                _participant.OnStateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(HandshakeParticipant participant, HandshakeState newState)
        {
            PlayClipForState(newState);
        }

        private void PlayClipForState(HandshakeState state)
        {
            if (_animator == null || _lastPlayedState == state) return;
            _lastPlayedState = state;

            var arb = HandshakeArbitrator.Instance;
            switch (state)
            {
                case HandshakeState.None:
                    _animator.Play("Idle", 0, 1.0f);
                    break;

                case HandshakeState.Reaching:
                    _animator.Play("Reach", 0, 0f);
                    break;

                case HandshakeState.Waiting:
                    _animator.Play("Wait", 0, 0f);
                    break;

                case HandshakeState.Grip:
                    _animator.Play("Grip", 0, 0f);
                    break;

                case HandshakeState.Shaking:
                    _animator.Play("Shake", 0, 0f);
                    break;

                case HandshakeState.Withdrawing:
                    _animator.Play("Withdraw", 0, 0f);
                    break;

                case HandshakeState.Laugh:
                    if (arb != null && arb.LaughClip != null && _animator.HasState(0, Animator.StringToHash("Laugh")))
                        _animator.Play("Laugh", 0, 0f);
                    else
                        _animator.Play("Withdraw", 0, 0f);
                    break;

                case HandshakeState.Angry:
                    if (arb != null && arb.AngryClip != null && _animator.HasState(0, Animator.StringToHash("Angry")))
                        _animator.Play("Angry", 0, 0f);
                    else
                        _animator.Play("Withdraw", 0, 0f);
                    break;

                case HandshakeState.ClapLaugh:
                    if (arb != null && arb.ClapLaughClip != null && _animator.HasState(0, Animator.StringToHash("ClapLaugh")))
                        _animator.Play("ClapLaugh", 0, 0f);
                    else
                        _animator.Play("Withdraw", 0, 0f);
                    break;

                case HandshakeState.Holding:
                    if (arb != null && arb.HoldClip != null && _animator.HasState(0, Animator.StringToHash("Hold")))
                        _animator.Play("Hold", 0, 0f);
                    break;

                case HandshakeState.Catching:
                    if (arb != null && arb.CatchingHoldClip != null && _animator.HasState(0, Animator.StringToHash("Catching")))
                        _animator.Play("Catching", 0, 0f);
                    break;

                case HandshakeState.Caught:
                    if (arb != null && arb.CaughtPullClip != null && _animator.HasState(0, Animator.StringToHash("Caught")))
                        _animator.Play("Caught", 0, 0f);
                    else
                        _animator.Play("Withdraw", 0, 0f);
                    break;

                case HandshakeState.ContestTriumph:
                    if (arb != null && arb.TriumphClip != null && _animator.HasState(0, Animator.StringToHash("Triumph")))
                        _animator.Play("Triumph", 0, 0f);
                    else
                        _animator.Play("Withdraw", 0, 0f);
                    break;

                case HandshakeState.ContestPain:
                    if (arb != null && arb.PainClip != null && _animator.HasState(0, Animator.StringToHash("Pain")))
                        _animator.Play("Pain", 0, 0f);
                    else
                        _animator.Play("Withdraw", 0, 0f);
                    break;

                case HandshakeState.ContestTie:
                    if (arb != null && arb.SlowWithdrawClip != null && _animator.HasState(0, Animator.StringToHash("SlowWithdraw")))
                        _animator.Play("SlowWithdraw", 0, 0f);
                    else
                        _animator.Play("Withdraw", 0, 0f);
                    break;

                case HandshakeState.EscapedStumble:
                    if (arb != null && arb.StumbleClip != null && _animator.HasState(0, Animator.StringToHash("Stumble")))
                        _animator.Play("Stumble", 0, 0f);
                    else
                        _animator.Play("Withdraw", 0, 0f);
                    break;

                case HandshakeState.GraspEmpty:
                    if (arb != null && arb.GraspEmptyClip != null && _animator.HasState(0, Animator.StringToHash("GraspEmpty")))
                        _animator.Play("GraspEmpty", 0, 0f);
                    else
                        _animator.Play("Withdraw", 0, 0f);
                    break;
            }
        }

        private void Update()
        {
            if (_participant == null) return;

            HandshakeState state = _participant.State;
            PlayClipForState(state);

            Color targetColor = _defaultBodyColor;
            switch (state)
            {
                case HandshakeState.Reaching:
                case HandshakeState.Waiting:
                    targetColor = Color.Lerp(_defaultBodyColor, new Color(1f, 0.88f, 0.25f), 0.55f);
                    break;

                case HandshakeState.Grip:
                    targetColor = Color.Lerp(_defaultBodyColor, new Color(0.85f, 0.35f, 1.0f), 0.6f);
                    break;

                case HandshakeState.Shaking:
                    targetColor = Color.Lerp(_defaultBodyColor, new Color(0.25f, 0.95f, 0.45f), 0.65f);
                    break;

                case HandshakeState.Withdrawing:
                    targetColor = Color.Lerp(_defaultBodyColor, new Color(0.65f, 0.65f, 0.65f), 0.45f);
                    break;

                case HandshakeState.Laugh:
                case HandshakeState.ClapLaugh:
                    targetColor = Color.Lerp(_defaultBodyColor, new Color(0.2f, 0.8f, 1f), 0.6f);
                    break;

                case HandshakeState.Angry:
                    targetColor = Color.Lerp(_defaultBodyColor, new Color(1f, 0.25f, 0.25f), 0.6f);
                    break;

                case HandshakeState.Holding:
                    targetColor = Color.Lerp(_defaultBodyColor, new Color(1f, 0.55f, 0.15f), 0.7f);
                    break;

                case HandshakeState.Catching:
                    targetColor = Color.Lerp(_defaultBodyColor, new Color(0.95f, 0.45f, 0.1f), 0.65f);
                    break;

                case HandshakeState.Caught:
                    targetColor = Color.Lerp(_defaultBodyColor, new Color(0.95f, 0.25f, 0.25f), 0.75f);
                    break;

                case HandshakeState.ContestTriumph:
                    targetColor = Color.Lerp(_defaultBodyColor, new Color(0.2f, 0.95f, 0.3f), 0.8f);
                    break;

                case HandshakeState.ContestPain:
                    targetColor = Color.Lerp(_defaultBodyColor, new Color(0.95f, 0.2f, 0.2f), 0.8f);
                    break;

                case HandshakeState.ContestTie:
                    targetColor = Color.Lerp(_defaultBodyColor, new Color(0.45f, 0.55f, 0.95f), 0.7f);
                    break;

                case HandshakeState.EscapedStumble:
                    targetColor = Color.Lerp(_defaultBodyColor, new Color(0.25f, 0.85f, 0.85f), 0.7f);
                    break;

                case HandshakeState.GraspEmpty:
                    targetColor = Color.Lerp(_defaultBodyColor, new Color(0.6f, 0.6f, 0.6f), 0.5f);
                    break;
            }

            if (_instanceMaterial != null)
            {
                Color current = GetCurrentColor();
                ApplyColor(Color.Lerp(current, targetColor, 12f * Time.deltaTime));
            }
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (_animator == null || _participant == null) return;

            if (_preIkWristTransform != null && _contactProxyTransform != null)
            {
                _contactProxyTransform.position = _preIkWristTransform.TransformPoint(_fistLocalOffsetInWrist);
                _contactProxyTransform.rotation = _preIkWristTransform.rotation;
            }

            float targetWeight = 0f;
            HandshakeState state = _participant.State;

            if (_opponent != null && _opponent.RightHandTransform != null)
            {
                bool oppActiveGrip = (_opponent.State == HandshakeState.Grip || _opponent.State == HandshakeState.Shaking || _opponent.State == HandshakeState.Holding);
                bool myActiveGrip = (state == HandshakeState.Grip || state == HandshakeState.Shaking || state == HandshakeState.Holding);

                if (myActiveGrip && oppActiveGrip)
                {
                    targetWeight = 0.85f;
                    Vector3 myHand = _preIkWristTransform != null ? _preIkWristTransform.position : _participant.RightHandTransform.position;
                    Vector3 oppHand = _opponent.RightHandTransform.position;
                    _currentIkTarget = Vector3.Lerp(myHand, oppHand, 0.5f);
                }
                else if (state == HandshakeState.Caught && _opponent.State == HandshakeState.Catching)
                {
                    targetWeight = 0.95f;
                    _currentIkTarget = _opponent.RightHandTransform.position;
                }
                else if (state == HandshakeState.Catching && _opponent.State == HandshakeState.Caught)
                {
                    targetWeight = 0.85f;
                    _currentIkTarget = _opponent.RightHandTransform.position;
                }
            }

            _currentIkWeight = Mathf.MoveTowards(_currentIkWeight, targetWeight, 8.0f * Time.deltaTime);

            _animator.SetIKPositionWeight(AvatarIKGoal.RightHand, _currentIkWeight);
            if (_currentIkWeight > 0.001f)
            {
                _animator.SetIKPosition(AvatarIKGoal.RightHand, _currentIkTarget);
            }
        }

        private Color GetCurrentColor()
        {
            if (_instanceMaterial == null) return _defaultBodyColor;
            if (_instanceMaterial.HasProperty(s_BaseColorFactorId)) return _instanceMaterial.GetColor(s_BaseColorFactorId);
            if (_instanceMaterial.HasProperty(s_BaseColorId)) return _instanceMaterial.GetColor(s_BaseColorId);
            if (_instanceMaterial.HasProperty(s_ColorId)) return _instanceMaterial.GetColor(s_ColorId);
            return _defaultBodyColor;
        }

        private void ApplyColor(Color color)
        {
            if (_instanceMaterial == null) return;
            if (_instanceMaterial.HasProperty(s_BaseColorFactorId)) _instanceMaterial.SetColor(s_BaseColorFactorId, color);
            if (_instanceMaterial.HasProperty(s_BaseColorId)) _instanceMaterial.SetColor(s_BaseColorId, color);
            if (_instanceMaterial.HasProperty(s_ColorId)) _instanceMaterial.SetColor(s_ColorId, color);
        }
    }
}
