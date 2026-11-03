using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom.Handshake
{
    /// <summary>
    /// Presentation-only component that plays the humanoid handshake clips on a humanoid <see cref="Animator"/>
    /// and applies runtime humanoid IK (<c>OnAnimatorIK</c>) to align the right hand visually toward the opponent's hand.
    /// Beyond the hand position, the same callback drives the rest of the pose so a slightly off angle between the two
    /// characters still reads correctly. The humanoid look-at (<c>SetLookAtWeight</c>/<c>SetLookAtPosition</c>) turns the head to the
    /// opponent's face and distributes the turn over spine and chest through the body weight, so the offset is shared by the torso
    /// instead of only stretching the arm; it is used instead of rotating bones by hand because it is solved on top of the clip pose
    /// and honours the avatar's muscle limits, and the weights stay below 1 so the clip's own head motion (laughing, triumph, pain)
    /// is reduced rather than replaced. The hand gets a rotation that continues the forearm line toward the contact point
    /// (axis-independent, so it does not depend on the rig's wrist axes) with the clip's roll kept, and the right elbow gets a hint
    /// position outward and below the shoulder-to-target line so it does not fold inward.
    /// Head and body weights are active from reach to the end of withdraw (any state other than <see cref="HandshakeState.None"/>)
    /// and ramp smoothly; all values live in <see cref="HandshakeSettings"/>. Only visuals change: the root transform, colliders,
    /// the pre-IK contact sphere and arbitration are untouched.
    /// Handles root motion application during Caught, Catching, and Leap states.
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

        public HandshakeParticipant Opponent { get => _opponent; set { _opponent = value; _opponentHead = null; } }

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

            InitVisualFeedback();
        }

        private void Awake()
        {
            if (_participant == null) _participant = GetComponentInParent<HandshakeParticipant>();
            if (_animator == null) _animator = GetComponent<Animator>();
            if (_skinnedRenderer == null) _skinnedRenderer = GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (_contactProxyTransform == null && _participant != null)
            {
                var bridge = _participant.GetComponentInChildren<HandContactBridge>();
                if (bridge != null) _contactProxyTransform = bridge.transform;
            }
            if (_preIkWristTransform == null && _animator != null)
            {
                _preIkWristTransform = _animator.GetBoneTransform(HumanBodyBones.RightHand);
            }

            InitVisualFeedback();
        }

        private void Start()
        {
            if (_preIkWristTransform == null && _animator != null)
            {
                _preIkWristTransform = _animator.GetBoneTransform(HumanBodyBones.RightHand);
            }
            if (_contactProxyTransform != null && _preIkWristTransform != null)
            {
                _contactProxyTransform.position = _preIkWristTransform.position + _preIkWristTransform.rotation * _fistLocalOffsetInWrist;
                _contactProxyTransform.rotation = _preIkWristTransform.rotation;
            }
            ConfigureAnimatorOverrides();
            PlayClipForState(HandshakeState.None);
        }

        private void InitVisualFeedback()
        {
            if (_skinnedRenderer != null && _instanceMaterial == null)
            {
                _instanceMaterial = _skinnedRenderer.material;
                ApplyColor(_defaultBodyColor);
            }

            if (_participant != null)
            {
                _participant.OnStateChanged -= HandleStateChanged;
                _participant.OnStateChanged += HandleStateChanged;
            }

            ConfigureAnimatorOverrides();
            PlayClipForState(HandshakeState.None);
        }

        /// <summary>
        /// Configures the runtime AnimatorOverrideController mapping original clip references to settings-defined clips.
        /// </summary>
        public void ConfigureAnimatorOverrides()
        {
            if (_animator == null || _animator.runtimeAnimatorController == null) return;

            var arb = HandshakeArbitrator.Instance;
            var settings = arb != null ? arb.Settings : null;
            if (settings == null) return;

            RuntimeAnimatorController baseController = _animator.runtimeAnimatorController;
            if (baseController is AnimatorOverrideController existingAoc)
            {
                baseController = existingAoc.runtimeAnimatorController;
            }

            var overrideController = new AnimatorOverrideController(baseController);
            overrideController.name = "HandshakeAnimatorOverride";

            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            overrideController.GetOverrides(overrides);

            for (int i = 0; i < overrides.Count; i++)
            {
                var originalClip = overrides[i].Key;
                if (originalClip != null)
                {
                    AnimationClip replacement = FindReplacementClip(settings, originalClip.name);
                    if (replacement != null)
                    {
                        overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(originalClip, replacement);
                    }
                }
            }

            overrideController.ApplyOverrides(overrides);
            _animator.runtimeAnimatorController = overrideController;
        }

        private AnimationClip FindReplacementClip(HandshakeSettings settings, string clipName)
        {
            if (settings == null) return null;
            string lower = clipName.ToLowerInvariant();
            if (lower.StartsWith("handshake-")) lower = lower.Substring("handshake-".Length);
            else if (lower.StartsWith("handshake_")) lower = lower.Substring("handshake_".Length);

            if (lower.Contains("reach")) return settings.ReachClip;
            if (lower.Contains("wait")) return settings.WaitClip;
            if (lower.Contains("withdraw") && !lower.Contains("slow")) return settings.WithdrawClip;
            if (lower.Contains("grip")) return settings.GripClip;
            if (lower.Contains("shake")) return settings.ShakeClip;
            if (lower.Contains("laugh") && !lower.Contains("clap")) return settings.LaughClip;
            if (lower.Contains("angry")) return settings.AngryClip;
            if (lower.Contains("clap")) return settings.ClapLaughClip;
            if (lower.Contains("hold")) return settings.HoldClip;
            if (lower.Contains("triumph")) return settings.TriumphClip;
            if (lower.Contains("pain")) return settings.PainClip;
            if (lower.Contains("slow")) return settings.SlowWithdrawClip;
            if (lower.Contains("stumble")) return settings.StumbleClip;
            if (lower.Contains("grasp") || lower.Contains("empty")) return settings.GraspEmptyClip;
            if (lower.Contains("caught")) return settings.CaughtPullClip;
            if (lower.Contains("dragged") || lower.Contains("catching")) return settings.CatchingHoldClip;
            if (lower.Contains("leap")) return settings.LeapClip;
            return null;
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

            bool isRootMotionState = (state == HandshakeState.Caught || state == HandshakeState.Catching || state == HandshakeState.Leap);
            _animator.applyRootMotion = isRootMotionState;
            if (_participant != null && _participant.Entity != null)
            {
                _participant.Entity.PauseGravity = (state == HandshakeState.Leap);
            }

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
                    _animator.Play("Laugh", 0, 0f);
                    break;
                case HandshakeState.Angry:
                    _animator.Play("Angry", 0, 0f);
                    break;
                case HandshakeState.ClapLaugh:
                    _animator.Play("ClapLaugh", 0, 0f);
                    break;
                case HandshakeState.Holding:
                    _animator.Play("Hold", 0, 0f);
                    break;
                case HandshakeState.Catching:
                    _animator.Play("Catching", 0, 0f);
                    break;
                case HandshakeState.Caught:
                    _animator.Play("Caught", 0, 0f);
                    break;
                case HandshakeState.Leap:
                    _animator.Play("Leap", 0, 0f);
                    break;
                case HandshakeState.ContestTriumph:
                    _animator.Play("Triumph", 0, 0f);
                    break;
                case HandshakeState.ContestPain:
                    _animator.Play("Pain", 0, 0f);
                    break;
                case HandshakeState.ContestTie:
                    _animator.Play("SlowWithdraw", 0, 0f);
                    break;
                case HandshakeState.EscapedStumble:
                    _animator.Play("Stumble", 0, 0f);
                    break;
                case HandshakeState.GraspEmpty:
                    _animator.Play("GraspEmpty", 0, 0f);
                    break;
            }
        }

        private void OnAnimatorMove()
        {
            if (_animator != null && _animator.applyRootMotion && _participant != null && _participant.Entity != null)
            {
                CharacterController cc = _participant.Entity.GetComponent<CharacterController>();
                if (cc != null)
                {
                    cc.Move(_animator.deltaPosition);
                }
            }
        }

        /// <summary>
        /// Retrieves the clip currently playing on the Base Layer.
        /// </summary>
        public AnimationClip GetCurrentPlayingClip()
        {
            if (_animator == null) return null;
            var infos = _animator.GetCurrentAnimatorClipInfo(0);
            if (infos != null && infos.Length > 0)
            {
                return infos[0].clip;
            }
            return null;
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

                case HandshakeState.Leap:
                    targetColor = Color.Lerp(_defaultBodyColor, new Color(1.0f, 0.4f, 0.1f), 0.8f);
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

            if (_contactProxyTransform != null && _preIkWristTransform != null)
            {
                if (!_hasIkOverride && _currentIkWeight < 0.01f)
                {
                    _contactProxyTransform.position = _preIkWristTransform.position + _preIkWristTransform.rotation * _fistLocalOffsetInWrist;
                    _contactProxyTransform.rotation = _preIkWristTransform.rotation;
                }
            }
        }

        private bool _hasIkOverride = false;
        private float _overrideIkWeight = 0f;
        private Vector3 _overrideIkTarget = Vector3.zero;

        private float _headLookWeight = 0f;
        private float _bodyLookWeight = 0f;
        private Transform _opponentHead;

        public float CurrentIkWeight => _currentIkWeight;
        public float HeadLookWeight => _headLookWeight;
        public float BodyLookWeight => _bodyLookWeight;

        public void SetIkOverride(Vector3 target, float weight)
        {
            _hasIkOverride = true;
            _overrideIkTarget = target;
            _overrideIkWeight = weight;
        }

        public void ClearIkOverride()
        {
            _hasIkOverride = false;
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (_animator == null || _participant == null) return;

            if (_contactProxyTransform != null && _preIkWristTransform != null)
            {
                _contactProxyTransform.position = _preIkWristTransform.position + _preIkWristTransform.rotation * _fistLocalOffsetInWrist;
                _contactProxyTransform.rotation = _preIkWristTransform.rotation;
            }

            float targetWeight = 0f;
            HandshakeState state = _participant.State;

            if (_hasIkOverride)
            {
                targetWeight = _overrideIkWeight;
                _currentIkTarget = _overrideIkTarget;
                _currentIkWeight = _overrideIkWeight;
            }
            else if (_opponent != null && _opponent.RightHandTransform != null)
            {
                bool oppActiveGrip = (_opponent.State == HandshakeState.Grip || _opponent.State == HandshakeState.Shaking || _opponent.State == HandshakeState.Holding);
                bool myActiveGrip = (state == HandshakeState.Grip || state == HandshakeState.Shaking || state == HandshakeState.Holding);

                if (myActiveGrip && oppActiveGrip)
                {
                    targetWeight = 0.85f;
                    Vector3 myHand = _animator.GetIKPosition(AvatarIKGoal.RightHand);
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

            ApplyHandRotationAndElbowHint();
            ApplyHeadAndBodyLook(state);
        }

        private void ApplyHandRotationAndElbowHint()
        {
            var settings = _participant.Settings;
            Transform upperArm = _animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            Transform lowerArm = _animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            bool active = settings != null && upperArm != null && lowerArm != null && _currentIkWeight > 0.001f;

            if (!active)
            {
                _animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
                _animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, 0f);
                return;
            }

            Vector3 elbow = lowerArm.position;
            Vector3 animatedHand = _animator.GetIKPosition(AvatarIKGoal.RightHand);
            Vector3 toAnimatedHand = animatedHand - elbow;
            Vector3 toTarget = _currentIkTarget - elbow;
            Quaternion aim = toAnimatedHand.sqrMagnitude > 1e-6f && toTarget.sqrMagnitude > 1e-6f
                ? Quaternion.FromToRotation(toAnimatedHand, toTarget)
                : Quaternion.identity;

            _animator.SetIKRotationWeight(AvatarIKGoal.RightHand, _currentIkWeight * settings.HandRotationWeight);
            _animator.SetIKRotation(AvatarIKGoal.RightHand, aim * _animator.GetIKRotation(AvatarIKGoal.RightHand));

            Vector3 shoulderToTarget = (upperArm.position + _currentIkTarget) * 0.5f;
            Vector3 hint = shoulderToTarget
                + _participant.transform.right * settings.ElbowHintOutward
                + Vector3.down * settings.ElbowHintDown;
            _animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, _currentIkWeight);
            _animator.SetIKHintPosition(AvatarIKHint.RightElbow, hint);
        }

        private void ApplyHeadAndBodyLook(HandshakeState state)
        {
            var settings = _participant.Settings;
            bool hasFace = TryGetOpponentFace(settings, out Vector3 face);
            bool interacting = settings != null && hasFace && state != HandshakeState.None;

            float step = (settings != null ? settings.LookWeightSpeed : 0f) * Time.deltaTime;
            _headLookWeight = Mathf.MoveTowards(_headLookWeight, interacting ? settings.HeadLookWeight : 0f, step);
            _bodyLookWeight = Mathf.MoveTowards(_bodyLookWeight, interacting ? settings.BodyLookWeight : 0f, step);

            if (settings == null && (_headLookWeight > 0f || _bodyLookWeight > 0f))
            {
                _headLookWeight = 0f;
                _bodyLookWeight = 0f;
            }

            _animator.SetLookAtWeight(1f, _bodyLookWeight, _headLookWeight, 0f, 0.5f);
            if (hasFace)
            {
                _animator.SetLookAtPosition(face);
            }
        }

        private bool TryGetOpponentFace(HandshakeSettings settings, out Vector3 face)
        {
            face = default;
            if (settings == null || _opponent == null) return false;

            if (_opponentHead == null)
            {
                var opponentAnimator = _opponent.GetComponentInChildren<Animator>();
                if (opponentAnimator != null) _opponentHead = opponentAnimator.GetBoneTransform(HumanBodyBones.Head);
            }
            if (_opponentHead == null) return false;

            face = _opponentHead.position + Vector3.up * settings.FaceHeightOffset;
            return true;
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
