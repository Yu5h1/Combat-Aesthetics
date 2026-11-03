using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom.Handshake
{
    /// <summary>
    /// Named action interval marked along an animation clip timeline.
    /// Controls whether player inputs are accepted according to the action rationality constraint.
    /// </summary>
    [Serializable]
    public struct ActionInterval
    {
        public string Name;
        [Range(0f, 1f)] public float StartNormalizedTime;
        [Range(0f, 1f)] public float EndNormalizedTime;
        public bool UnderControl;
        public bool Cancellable;

        public ActionInterval(string name, float start, float end, bool underControl, bool cancellable)
        {
            Name = name;
            StartNormalizedTime = start;
            EndNormalizedTime = end;
            UnderControl = underControl;
            Cancellable = cancellable;
        }
    }

    /// <summary>
    /// Binding associating an animation clip with its timeline action intervals.
    /// </summary>
    [Serializable]
    public class ClipIntervalBinding
    {
        public AnimationClip Clip;
        public ActionInterval[] Intervals;

        public ClipIntervalBinding(AnimationClip clip, params ActionInterval[] intervals)
        {
            Clip = clip;
            Intervals = intervals;
        }
    }

    /// <summary>
    /// Configurable ScriptableObject settings asset for the Stage 2b handshake interaction across Batches 1, 2, and 3.
    /// Values adjusted during Play Mode persist on the asset on disk to satisfy manual scene authoring requirements.
    /// Sole authoritative source for animation clips and interaction tuning.
    /// </summary>
    public class HandshakeSettings : ScriptableObject
    {
        [Header("Timing and Contest Settings")]
        [Tooltip("Grace period where brief key releases during holding are not treated as full release (default 0.30s).")]
        [SerializeField] private float _releaseGraceTime = 0.30f;

        [Tooltip("Minimum press count difference required for a definitive win in the strength contest (default 1).")]
        [SerializeField] private int _contestThreshold = 1;

        [Tooltip("Distance compensation added to skeleton measured arm reach (adjustable, default 0.0m).")]
        [SerializeField] private float _escapeDistanceCompensation = 0.0f;

        [Header("Action Rationality Constraints & Intervals")]
        [Tooltip("Custom action intervals bound to animation clips. Adjusting boundaries tunes control feel without arbitrary fixed timers.")]
        [SerializeField] private ClipIntervalBinding[] _clipIntervalBindings;

        [Header("Procedural Head, Body and Arm IK (presentation only)")]
        [Tooltip("How strongly the head turns toward the opponent's face during an interaction (0 keeps the clip's head motion).")]
        [Range(0f, 1f)] [SerializeField] private float _headLookWeight = 0.8f;

        [Tooltip("How strongly the spine turns or leans toward the opponent during an interaction, sharing the offset with the arm.")]
        [Range(0f, 1f)] [SerializeField] private float _bodyLookWeight = 0.45f;

        [Tooltip("Weight change per second when the head and body turn start or end.")]
        [SerializeField] private float _lookWeightSpeed = 5f;

        [Tooltip("Height above the opponent's head bone that the head looks at (face centre).")]
        [SerializeField] private float _faceHeightOffset = 0.08f;

        [Tooltip("How strongly the right hand is rotated to continue the forearm line toward the contact point.")]
        [Range(0f, 1f)] [SerializeField] private float _handRotationWeight = 0.85f;

        [Tooltip("Distance the right elbow hint sits outward from the body (metres).")]
        [SerializeField] private float _elbowHintOutward = 0.30f;

        [Tooltip("Distance the right elbow hint sits below the shoulder-to-hand line (metres).")]
        [SerializeField] private float _elbowHintDown = 0.35f;

        [Header("Handshake Grip Pose (presentation only)")]
        [Tooltip("Offset of the grip point in the character's local space (x right, y up, z toward the opponent), metres.")]
        [SerializeField] private Vector3 _gripPositionOffset = Vector3.zero;

        [Tooltip("Euler offset of the hand rotation about the character's local axes, degrees.")]
        [SerializeField] private Vector3 _gripEulerOffset = Vector3.zero;

        [Tooltip("Where the grip point sits between own animated hand (0) and the opponent's hand (1) during grip, shaking and holding.")]
        [Range(0f, 1f)] [SerializeField] private float _gripBias = 0.5f;

        [Tooltip("Right hand IK weight during grip, shaking and holding.")]
        [Range(0f, 1f)] [SerializeField] private float _gripIkWeight = 0.85f;

        [Tooltip("Right hand IK weight for the caught participant.")]
        [Range(0f, 1f)] [SerializeField] private float _caughtIkWeight = 0.95f;

        [Tooltip("Right hand IK weight for the catching participant.")]
        [Range(0f, 1f)] [SerializeField] private float _catchingIkWeight = 0.85f;

        [Header("Batch 1 Animation Clips")]
        [SerializeField] private AnimationClip _reachClip;
        [SerializeField] private AnimationClip _waitClip;
        [SerializeField] private AnimationClip _withdrawClip;
        [SerializeField] private AnimationClip _gripClip;
        [SerializeField] private AnimationClip _shakeClip;

        [Header("Batch 2 Reaction Animation Clips")]
        [SerializeField] private AnimationClip _laughClip;
        [SerializeField] private AnimationClip _angryClip;
        [SerializeField] private AnimationClip _clapLaughClip;

        [Header("Batch 3 Strength Contest Animation Clips")]
        [SerializeField] private AnimationClip _holdClip;
        [SerializeField] private AnimationClip _triumphClip;
        [SerializeField] private AnimationClip _painClip;
        [SerializeField] private AnimationClip _slowWithdrawClip;
        [SerializeField] private AnimationClip _stumbleClip;
        [SerializeField] private AnimationClip _graspEmptyClip;
        [SerializeField] private AnimationClip _caughtPullClip;
        [SerializeField] private AnimationClip _catchingHoldClip;
        [SerializeField] private AnimationClip _leapClip;

        public float ReleaseGraceTime
        {
            get => _releaseGraceTime;
            set
            {
                _releaseGraceTime = value;
                MarkDirty();
            }
        }

        public int ContestThreshold
        {
            get => _contestThreshold;
            set
            {
                _contestThreshold = value;
                MarkDirty();
            }
        }

        public float EscapeDistanceCompensation
        {
            get => _escapeDistanceCompensation;
            set
            {
                _escapeDistanceCompensation = value;
                MarkDirty();
            }
        }

        public float EscapeDistanceThreshold
        {
            get => HandshakeArbitrator.Instance != null ? HandshakeArbitrator.Instance.GetEffectiveEscapeThreshold() : -1f;
            set
            {
                float reach = HandshakeArbitrator.Instance != null ? HandshakeArbitrator.Instance.MeasureTotalArmReach() : -1f;
                if (reach > 0f)
                {
                    _escapeDistanceCompensation = value - reach;
                    MarkDirty();
                }
                else
                {
                    Debug.LogError("[HandshakeSettings] Cannot set EscapeDistanceThreshold: Skeleton arm reach could not be measured from participants.");
                }
            }
        }

        public float HeadLookWeight { get => _headLookWeight; set { _headLookWeight = Mathf.Clamp01(value); MarkDirty(); } }
        public float BodyLookWeight { get => _bodyLookWeight; set { _bodyLookWeight = Mathf.Clamp01(value); MarkDirty(); } }
        public float LookWeightSpeed { get => _lookWeightSpeed; set { _lookWeightSpeed = value; MarkDirty(); } }
        public float FaceHeightOffset { get => _faceHeightOffset; set { _faceHeightOffset = value; MarkDirty(); } }
        public float HandRotationWeight { get => _handRotationWeight; set { _handRotationWeight = Mathf.Clamp01(value); MarkDirty(); } }
        public float ElbowHintOutward { get => _elbowHintOutward; set { _elbowHintOutward = value; MarkDirty(); } }
        public float ElbowHintDown { get => _elbowHintDown; set { _elbowHintDown = value; MarkDirty(); } }

        public Vector3 GripPositionOffset { get => _gripPositionOffset; set { _gripPositionOffset = value; MarkDirty(); } }
        public Vector3 GripEulerOffset { get => _gripEulerOffset; set { _gripEulerOffset = value; MarkDirty(); } }
        public float GripBias { get => _gripBias; set { _gripBias = Mathf.Clamp01(value); MarkDirty(); } }
        public float GripIkWeight { get => _gripIkWeight; set { _gripIkWeight = Mathf.Clamp01(value); MarkDirty(); } }
        public float CaughtIkWeight { get => _caughtIkWeight; set { _caughtIkWeight = Mathf.Clamp01(value); MarkDirty(); } }
        public float CatchingIkWeight { get => _catchingIkWeight; set { _catchingIkWeight = Mathf.Clamp01(value); MarkDirty(); } }

        public ClipIntervalBinding[] ClipIntervalBindings
        {
            get => _clipIntervalBindings;
            set
            {
                _clipIntervalBindings = value;
                MarkDirty();
            }
        }

        /// <summary>
        /// Maps a <see cref="HandshakeState"/> to its assigned <see cref="AnimationClip"/>.
        /// </summary>
        public AnimationClip GetClipForState(HandshakeState state)
        {
            switch (state)
            {
                case HandshakeState.Reaching: return _reachClip;
                case HandshakeState.Waiting: return _waitClip;
                case HandshakeState.Withdrawing: return _withdrawClip;
                case HandshakeState.Grip: return _gripClip;
                case HandshakeState.Shaking: return _shakeClip;
                case HandshakeState.Laugh: return _laughClip;
                case HandshakeState.Angry: return _angryClip;
                case HandshakeState.ClapLaugh: return _clapLaughClip;
                case HandshakeState.Holding: return _holdClip;
                case HandshakeState.ContestTriumph: return _triumphClip;
                case HandshakeState.ContestPain: return _painClip;
                case HandshakeState.ContestTie: return _slowWithdrawClip;
                case HandshakeState.EscapedStumble: return _stumbleClip;
                case HandshakeState.GraspEmpty: return _graspEmptyClip;
                case HandshakeState.Caught: return _caughtPullClip;
                case HandshakeState.Catching: return _catchingHoldClip;
                case HandshakeState.Leap: return _leapClip;
                default: return null;
            }
        }

        /// <summary>
        /// Evaluates whether the participant is under player control during the specified clip and normalized time.
        /// Rationality constraint: non-cancellable reaction and recovery animations keep undercontrol false.
        /// </summary>
        public bool IsActionUnderControl(AnimationClip clip, float normalizedTime)
        {
            if (clip == null) return true;

            if (_clipIntervalBindings != null)
            {
                foreach (var binding in _clipIntervalBindings)
                {
                    if (binding != null && binding.Clip == clip && binding.Intervals != null)
                    {
                        foreach (var interval in binding.Intervals)
                        {
                            if (normalizedTime >= interval.StartNormalizedTime && normalizedTime <= interval.EndNormalizedTime)
                            {
                                return interval.UnderControl;
                            }
                        }
                    }
                }
            }

            return normalizedTime >= 1.0f;
        }

        /// <summary>
        /// Evaluates whether the current action can be cancelled into a withdrawal at the specified normalized time.
        /// </summary>
        public bool IsActionCancellable(AnimationClip clip, float normalizedTime)
        {
            if (clip == null) return false;

            if (_clipIntervalBindings != null)
            {
                foreach (var binding in _clipIntervalBindings)
                {
                    if (binding != null && binding.Clip == clip && binding.Intervals != null)
                    {
                        foreach (var interval in binding.Intervals)
                        {
                            if (normalizedTime >= interval.StartNormalizedTime && normalizedTime <= interval.EndNormalizedTime)
                            {
                                return interval.Cancellable;
                            }
                        }
                    }
                }
            }

            return (clip == _reachClip || clip == _waitClip);
        }

        public AnimationClip ReachClip { get => _reachClip; set { _reachClip = value; MarkDirty(); } }
        public AnimationClip WaitClip { get => _waitClip; set { _waitClip = value; MarkDirty(); } }
        public AnimationClip WithdrawClip { get => _withdrawClip; set { _withdrawClip = value; MarkDirty(); } }
        public AnimationClip GripClip { get => _gripClip; set { _gripClip = value; MarkDirty(); } }
        public AnimationClip ShakeClip { get => _shakeClip; set { _shakeClip = value; MarkDirty(); } }

        public AnimationClip LaughClip { get => _laughClip; set { _laughClip = value; MarkDirty(); } }
        public AnimationClip AngryClip { get => _angryClip; set { _angryClip = value; MarkDirty(); } }
        public AnimationClip ClapLaughClip { get => _clapLaughClip; set { _clapLaughClip = value; MarkDirty(); } }

        public AnimationClip HoldClip { get => _holdClip; set { _holdClip = value; MarkDirty(); } }
        public AnimationClip TriumphClip { get => _triumphClip; set { _triumphClip = value; MarkDirty(); } }
        public AnimationClip PainClip { get => _painClip; set { _painClip = value; MarkDirty(); } }
        public AnimationClip SlowWithdrawClip { get => _slowWithdrawClip; set { _slowWithdrawClip = value; MarkDirty(); } }
        public AnimationClip StumbleClip { get => _stumbleClip; set { _stumbleClip = value; MarkDirty(); } }
        public AnimationClip GraspEmptyClip { get => _graspEmptyClip; set { _graspEmptyClip = value; MarkDirty(); } }
        public AnimationClip CaughtPullClip { get => _caughtPullClip; set { _caughtPullClip = value; MarkDirty(); } }
        public AnimationClip CatchingHoldClip { get => _catchingHoldClip; set { _catchingHoldClip = value; MarkDirty(); } }
        public AnimationClip LeapClip { get => _leapClip; set { _leapClip = value; MarkDirty(); } }

        private void MarkDirty()
        {
#if UNITY_EDITOR
            if (EditorUtility.IsPersistent(this))
            {
                EditorUtility.SetDirty(this);
            }
#endif
        }

        /// <summary>
        /// Creates an in-memory runtime instance with canonical default settings if no asset is assigned.
        /// </summary>
        public static HandshakeSettings CreateDefault()
        {
            var instance = CreateInstance<HandshakeSettings>();
            instance.name = "DefaultHandshakeSettings";
            return instance;
        }
    }
}
