using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom.Handshake
{
    /// <summary>
    /// Configurable ScriptableObject settings asset for the Stage 2b handshake interaction across Batches 1, 2, and 3.
    /// Values adjusted during Play Mode persist on the asset on disk to satisfy manual scene authoring requirements.
    /// </summary>
    public class HandshakeSettings : ScriptableObject
    {
        [Header("Timing and Contest Settings")]
        [Tooltip("Grace period where brief key releases during holding are not treated as full release (default 0.30s).")]
        [SerializeField] private float _releaseGraceTime = 0.30f;

        [Tooltip("Minimum press count difference required for a definitive win in the strength contest (default 1).")]
        [SerializeField] private int _contestThreshold = 1;

        [Tooltip("Body distance threshold beyond which a caught participant successfully escapes (default 1.80m).")]
        [SerializeField] private float _escapeDistanceThreshold = 1.80f;

        [Tooltip("Uncontrolled duration where a prank participant who released early cannot initiate a new reach (default 0.60s).")]
        [SerializeField] private float _prankReactionDuration = 0.60f;

        [Tooltip("Fallback duration for reaction presentations when humanoid reaction animation clips are null (default 0.50s).")]
        [SerializeField] private float _reactionFallbackDuration = 0.50f;

        [Header("Catch and Escape Locomotion Speeds")]
        [Tooltip("Normal walk speed of the caught participant when pulling back (default 2.20 m/s).")]
        [SerializeField] private float _caughtWalkSpeed = 2.20f;

        [Tooltip("Slow walk speed of the catching participant trying to follow along (default 1.20 m/s).")]
        [SerializeField] private float _catchingFollowSpeed = 1.20f;

        [Tooltip("Backward impulse displacement applied when the caught participant jumps (default 0.60m).")]
        [SerializeField] private float _jumpEscapeDisplacement = 0.60f;

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

        public float EscapeDistanceThreshold
        {
            get => _escapeDistanceThreshold;
            set
            {
                _escapeDistanceThreshold = value;
                MarkDirty();
            }
        }

        public float PrankReactionDuration
        {
            get => _prankReactionDuration;
            set
            {
                _prankReactionDuration = value;
                MarkDirty();
            }
        }

        public float ReactionFallbackDuration
        {
            get => _reactionFallbackDuration;
            set
            {
                _reactionFallbackDuration = value;
                MarkDirty();
            }
        }

        public float CaughtWalkSpeed
        {
            get => _caughtWalkSpeed;
            set
            {
                _caughtWalkSpeed = value;
                MarkDirty();
            }
        }

        public float CatchingFollowSpeed
        {
            get => _catchingFollowSpeed;
            set
            {
                _catchingFollowSpeed = value;
                MarkDirty();
            }
        }

        public float JumpEscapeDisplacement
        {
            get => _jumpEscapeDisplacement;
            set
            {
                _jumpEscapeDisplacement = value;
                MarkDirty();
            }
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
