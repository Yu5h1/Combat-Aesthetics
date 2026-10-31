using System;
using System.Collections.Generic;
using UnityEngine;
using Yu5h1Lib;
using Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom;

namespace Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom.Handshake
{
    /// <summary>
    /// Authoritative arbitrator for the Stage 2b handshake interaction across Batches 1, 2, and 3.
    /// Determines contact solely from right-hand collider trigger events between the two touching hands,
    /// arbitrates Grip (Success, Prank, BothWithdraw), Holding, Strength Contest, and Catch and Escape.
    /// </summary>
    public class HandshakeArbitrator : MonoBehaviour
    {
        private static HandshakeArbitrator s_Instance;
        public static HandshakeArbitrator Instance
        {
            get
            {
                if (s_Instance == null) s_Instance = ObjectUtility.FindObject<HandshakeArbitrator>(false);
                return s_Instance;
            }
            private set => s_Instance = value;
        }

        [Header("Handshake Settings Asset (Manual Authoring & Tuning)")]
        [SerializeField, Inline] private HandshakeSettings _settings;

        private readonly List<HandshakeParticipant> _participants = new List<HandshakeParticipant>();
        private HandshakeParticipant _participantA;
        private HandshakeParticipant _participantB;
        private bool _participantAReleasedDuringGrip = false;
        private bool _participantBReleasedDuringGrip = false;
        private float _participantAReleaseTimestamp = -1f;
        private float _participantBReleaseTimestamp = -1f;
        private HandshakeParticipant _lastPranker;
        private HandshakeParticipant _lastPranked;

        private float _aReleaseTimer = 0f;
        private float _bReleaseTimer = 0f;
        private HandshakeParticipant _contestWinner;
        private HandshakeParticipant _contestLoser;
        private int _winnerPressCount = 0;
        private int _loserPressCount = 0;
        private HandshakeParticipant _catchingParticipant;
        private HandshakeParticipant _caughtParticipant;

        private HandshakeState _sessionState = HandshakeState.None;
        private float _sessionTimer = 0f;
        private HandshakeOutcome _lastOutcome = HandshakeOutcome.None;

        public HandshakeSettings Settings
        {
            get
            {
                if (_settings == null) _settings = HandshakeSettings.CreateDefault();
                return _settings;
            }
            set => _settings = value;
        }

        public HandshakeState SessionState => _sessionState;
        public HandshakeOutcome LastOutcome => _lastOutcome;
        public HandshakeParticipant ParticipantA => _participantA;
        public HandshakeParticipant ParticipantB => _participantB;
        public HandshakeParticipant LastPranker => _lastPranker;
        public HandshakeParticipant LastPranked => _lastPranked;
        public bool ParticipantAReleasedDuringGrip => _participantAReleasedDuringGrip;
        public bool ParticipantBReleasedDuringGrip => _participantBReleasedDuringGrip;
        public float ParticipantAReleaseTimestamp => _participantAReleaseTimestamp;
        public float ParticipantBReleaseTimestamp => _participantBReleaseTimestamp;

        public HandshakeParticipant ContestWinner => _contestWinner;
        public HandshakeParticipant ContestLoser => _contestLoser;
        public HandshakeParticipant LastContestWinner => _contestWinner;
        public HandshakeParticipant LastCatcher => _catchingParticipant;
        public HandshakeParticipant LastEscaper => _caughtParticipant;
        public int WinnerPressCount => _winnerPressCount;
        public int LoserPressCount => _loserPressCount;
        public int LastPlayerTaps => _participantA != null && _participantA.TeamId == 0 ? _participantA.InteractPressCount : (_participantB != null && _participantB.TeamId == 0 ? _participantB.InteractPressCount : 0);
        public int LastAiTaps => _participantA != null && _participantA.TeamId != 0 ? _participantA.InteractPressCount : (_participantB != null && _participantB.TeamId != 0 ? _participantB.InteractPressCount : 0);
        public int PlayerTaps => LastPlayerTaps;
        public int AiTaps => LastAiTaps;
        public float BodyDistance => (_participantA != null && _participantB != null) ? Vector3.Distance(_participantA.transform.position, _participantB.transform.position) : 0f;

        public float ReleaseGraceTime
        {
            get => Settings != null ? Settings.ReleaseGraceTime : 0.30f;
            set { if (Settings != null) Settings.ReleaseGraceTime = value; }
        }

        public float EscapeDistanceThreshold
        {
            get => Settings != null ? Settings.EscapeDistanceThreshold : 1.80f;
            set { if (Settings != null) Settings.EscapeDistanceThreshold = value; }
        }

        public HandshakeParticipant CatchingParticipant => _catchingParticipant;
        public HandshakeParticipant CaughtParticipant => _caughtParticipant;

        public AnimationClip ReachClip => Settings.ReachClip;
        public AnimationClip WaitClip => Settings.WaitClip;
        public AnimationClip WithdrawClip => Settings.WithdrawClip;
        public AnimationClip GripClip => Settings.GripClip;
        public AnimationClip ShakeClip => Settings.ShakeClip;

        public AnimationClip LaughClip => Settings.LaughClip;
        public AnimationClip AngryClip => Settings.AngryClip;
        public AnimationClip ClapLaughClip => Settings.ClapLaughClip;

        public AnimationClip HoldClip => Settings.HoldClip;
        public AnimationClip TriumphClip => Settings.TriumphClip;
        public AnimationClip PainClip => Settings.PainClip;
        public AnimationClip SlowWithdrawClip => Settings.SlowWithdrawClip;
        public AnimationClip StumbleClip => Settings.StumbleClip;
        public AnimationClip GraspEmptyClip => Settings.GraspEmptyClip;
        public AnimationClip CaughtPullClip => Settings.CaughtPullClip;
        public AnimationClip CatchingHoldClip => Settings.CatchingHoldClip;

        public float GripDuration => GripClip != null ? GripClip.length : 0.50f;
        public float ShakeDuration => ShakeClip != null ? ShakeClip.length : 0.50f;
        public float ReachDuration => ReachClip != null ? ReachClip.length : 0.50f;
        public float WithdrawDuration => WithdrawClip != null ? WithdrawClip.length : 0.50f;

        public event Action<string> OnArbitrationLog;
        public event Action<HandshakeOutcome> OnHandshakeFinished;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (_settings == null) _settings = HandshakeSettings.CreateDefault();
        }

        private void OnEnable()
        {
            CombatEntity.OnGlobalArenaReset += HandleGlobalReset;
        }

        private void OnDisable()
        {
            CombatEntity.OnGlobalArenaReset -= HandleGlobalReset;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Assigns the configurable settings asset for persistence and Inspector adjustment.
        /// </summary>
        public void ConfigureSettings(HandshakeSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Assigns the five humanoid animation clips so state durations are read directly from clip lengths.
        /// </summary>
        public void ConfigureClips(AnimationClip reach, AnimationClip wait, AnimationClip withdraw, AnimationClip grip, AnimationClip shake)
        {
            Settings.ReachClip = reach;
            Settings.WaitClip = wait;
            Settings.WithdrawClip = withdraw;
            Settings.GripClip = grip;
            Settings.ShakeClip = shake;

            foreach (var p in _participants)
            {
                ApplyClipDurationsToParticipant(p);
            }
        }

        /// <summary>
        /// Configures the Batch 2 reaction humanoid animation clips (laugh, angry, clap-laugh).
        /// </summary>
        public void ConfigureBatch2Clips(AnimationClip laugh, AnimationClip angry, AnimationClip clapLaugh)
        {
            Settings.LaughClip = laugh;
            Settings.AngryClip = angry;
            Settings.ClapLaughClip = clapLaugh;
        }

        /// <summary>
        /// Configures the Batch 3 strength contest humanoid animation clips.
        /// </summary>
        public void ConfigureBatch3Clips(AnimationClip hold, AnimationClip triumph, AnimationClip pain, AnimationClip slowWithdraw, AnimationClip stumble, AnimationClip graspEmpty)
        {
            Settings.HoldClip = hold;
            Settings.TriumphClip = triumph;
            Settings.PainClip = pain;
            Settings.SlowWithdrawClip = slowWithdraw;
            Settings.StumbleClip = stumble;
            Settings.GraspEmptyClip = graspEmpty;
        }

        private void ApplyClipDurationsToParticipant(HandshakeParticipant participant)
        {
            if (participant == null) return;
            if (ReachDuration > 0f) participant.ReachDuration = ReachDuration;
            if (WithdrawDuration > 0f) participant.WithdrawDuration = WithdrawDuration;
        }

        /// <summary>
        /// Registers a participant and subscribes to its right-hand trigger contact bridge.
        /// </summary>
        public void RegisterParticipant(HandshakeParticipant participant)
        {
            if (participant == null || _participants.Contains(participant)) return;

            ApplyClipDurationsToParticipant(participant);
            _participants.Add(participant);

            if (participant.ContactBridge != null)
            {
                participant.ContactBridge.OnTriggerContact += OnHandTriggerContact;
            }
        }

        /// <summary>
        /// Unregisters a participant from the arbitrator.
        /// </summary>
        public void UnregisterParticipant(HandshakeParticipant participant)
        {
            if (participant == null) return;
            _participants.Remove(participant);
            if (participant.ContactBridge != null)
            {
                participant.ContactBridge.OnTriggerContact -= OnHandTriggerContact;
            }
        }

        /// <summary>
        /// Records a cancelled handshake outcome when a participant releases the interact key during reaching or waiting.
        /// </summary>
        public void NotifyCancelled(HandshakeParticipant participant)
        {
            if (_sessionState == HandshakeState.None)
            {
                _lastOutcome = HandshakeOutcome.Cancelled;
            }
        }

        private void OnHandTriggerContact(HandContactBridge selfBridge, HandContactBridge otherBridge)
        {
            if (_sessionState != HandshakeState.None) return;
            if (selfBridge == null || otherBridge == null) return;

            HandshakeParticipant p1 = selfBridge.Owner;
            HandshakeParticipant p2 = otherBridge.Owner;

            if (p1 != null && p2 != null && p1 != p2)
            {
                TryInitiateGrip(p1, p2);
            }
        }

        private void Update()
        {
            if (_sessionState == HandshakeState.Grip)
            {
                UpdateGripState();
            }
            else if (_sessionState == HandshakeState.Holding)
            {
                UpdateHoldingState();
            }
            else if (_sessionState == HandshakeState.Catching)
            {
                UpdateCatchingState();
            }
            else if (_sessionState == HandshakeState.Shaking)
            {
                UpdateShakingState();
            }
            else if (_sessionState == HandshakeState.Laugh || _sessionState == HandshakeState.ClapLaugh ||
                     _sessionState == HandshakeState.ContestTriumph || _sessionState == HandshakeState.ContestTie ||
                     _sessionState == HandshakeState.EscapedStumble)
            {
                UpdateReactionState();
            }
        }

        private void TryInitiateGrip(HandshakeParticipant p1, HandshakeParticipant p2)
        {
            if (_sessionState != HandshakeState.None) return;

            bool p1Ready = (p1.State == HandshakeState.Reaching || p1.State == HandshakeState.Waiting);
            bool p2Ready = (p2.State == HandshakeState.Reaching || p2.State == HandshakeState.Waiting);

            if (!p1Ready || !p2Ready) return;

            _participantA = p1;
            _participantB = p2;
            _participantAReleasedDuringGrip = false;
            _participantBReleasedDuringGrip = false;
            _participantAReleaseTimestamp = -1f;
            _participantBReleaseTimestamp = -1f;
            _lastPranker = null;
            _lastPranked = null;
            _aReleaseTimer = 0f;
            _bReleaseTimer = 0f;

            float gripLen = GripDuration;
            _sessionState = HandshakeState.Grip;
            _sessionTimer = gripLen;

            _participantA.SetState(HandshakeState.Grip, gripLen);
            _participantB.SetState(HandshakeState.Grip, gripLen);

            EmitArbitrationLog($"[握手判定] [{p1.EntityId}] 與 [{p2.EntityId}] 右手碰撞體接觸（碰到）！開始播握緊（{gripLen:F2}s）");
        }

        private void UpdateGripState()
        {
            if (_participantA == null || _participantB == null)
            {
                _sessionState = HandshakeState.None;
                return;
            }

            bool aHolding = _participantA.CurrentIntent.InteractHold;
            bool bHolding = _participantB.CurrentIntent.InteractHold;
            float withdrawLen = WithdrawDuration;

            if (!aHolding && !_participantAReleasedDuringGrip)
            {
                _participantAReleasedDuringGrip = true;
                _participantAReleaseTimestamp = Time.time;
                _participantA.SetState(HandshakeState.Withdrawing, withdrawLen);
                EmitArbitrationLog($"[握手判定] [{_participantA.EntityId}] 於握緊期間放開互動鍵 (時間: {_participantAReleaseTimestamp:F2}s)！先行抽回收手（{withdrawLen:F2}s），持續判定至握緊結束。");
            }

            if (!bHolding && !_participantBReleasedDuringGrip)
            {
                _participantBReleasedDuringGrip = true;
                _participantBReleaseTimestamp = Time.time;
                _participantB.SetState(HandshakeState.Withdrawing, withdrawLen);
                EmitArbitrationLog($"[握手判定] [{_participantB.EntityId}] 於握緊期間放開互動鍵 (時間: {_participantBReleaseTimestamp:F2}s)！先行抽回收手（{withdrawLen:F2}s），持續判定至握緊結束。");
            }

            _sessionTimer -= Time.deltaTime;
            if (_sessionTimer <= 0f)
            {
                ResolveGripOutcome();
            }
        }

        private void ResolveGripOutcome()
        {
            float withdrawLen = WithdrawDuration;

            if (!_participantAReleasedDuringGrip && !_participantBReleasedDuringGrip)
            {
                _lastOutcome = HandshakeOutcome.Success;
                EmitArbitrationLog("[握手判定] 握手成功 (Success)！握緊結束時雙方均按著互動鍵，正式成立握手成功。雙方進入握住，維持緊握姿勢（不搖手）。");
                EnterHoldingState(resetPressCount: true);
                OnHandshakeFinished?.Invoke(_lastOutcome);
            }
            else if (_participantAReleasedDuringGrip && _participantBReleasedDuringGrip)
            {
                _lastOutcome = HandshakeOutcome.BothWithdraw;
                _lastPranker = null;
                _lastPranked = null;

                string orderInfo = _participantAReleaseTimestamp < _participantBReleaseTimestamp
                    ? $"先放開者: {_participantA.EntityId} ({_participantAReleaseTimestamp:F2}s)，後放開者: {_participantB.EntityId} ({_participantBReleaseTimestamp:F2}s)"
                    : $"先放開者: {_participantB.EntityId} ({_participantBReleaseTimestamp:F2}s)，後放開者: {_participantA.EntityId} ({_participantAReleaseTimestamp:F2}s)";

                EmitArbitrationLog($"[握手判定] 判定結果：同時收手 (BothWithdraw)！雙方均在握緊結束前放開 ({orderInfo})。");

                float reactionLock = Settings.PrankReactionDuration;
                _participantA.LockReachingForDuration(reactionLock);
                _participantB.LockReachingForDuration(reactionLock);

                TriggerBothWithdrawPresentation(withdrawLen);
            }
            else
            {
                _lastOutcome = HandshakeOutcome.Prank;
                HandshakeParticipant pranker = _participantAReleasedDuringGrip ? _participantA : _participantB;
                HandshakeParticipant pranked = _participantAReleasedDuringGrip ? _participantB : _participantA;
                _lastPranker = pranker;
                _lastPranked = pranked;

                float reactionLock = Settings.PrankReactionDuration;
                pranker.LockReachingForDuration(reactionLock);
                EmitArbitrationLog($"[握手判定] 判定結果：惡作劇 (Prank)！放開者為【{pranker.EntityId}】，被惡作劇者為【{pranked.EntityId}】。放開者在反應結束前（{reactionLock:F2}s）不可重新發起伸手。");

                TriggerPrankPresentation(pranker, pranked, withdrawLen);
            }
        }

        private void EnterHoldingState(bool resetPressCount = true)
        {
            _sessionState = HandshakeState.Holding;
            _sessionTimer = 0f;
            _aReleaseTimer = 0f;
            _bReleaseTimer = 0f;

            if (resetPressCount)
            {
                _participantA.ResetPressCount();
                _participantB.ResetPressCount();
            }

            _participantA.SetState(HandshakeState.Holding);
            _participantB.SetState(HandshakeState.Holding);

            EmitArbitrationLog($"[握手判定] 進入雙方握住 (Holding)！累計次數: [P1:{PlayerTaps} vs AI:{AiTaps}]。連按互動鍵比力氣，雙方均放開結束比次數。");
        }

        private void UpdateHoldingState()
        {
            if (_participantA == null || _participantB == null)
            {
                _sessionState = HandshakeState.None;
                return;
            }

            bool aHold = _participantA.CurrentIntent.InteractHold;
            bool bHold = _participantB.CurrentIntent.InteractHold;

            float graceTime = Settings.ReleaseGraceTime;

            if (!aHold) _aReleaseTimer += Time.deltaTime;
            else _aReleaseTimer = 0f;

            if (!bHold) _bReleaseTimer += Time.deltaTime;
            else _bReleaseTimer = 0f;

            if (_aReleaseTimer >= graceTime && _bReleaseTimer >= graceTime)
            {
                ResolveStrengthContest();
            }
            else if (_aReleaseTimer >= graceTime && bHold)
            {
                EnterCaughtState(_participantB, _participantA);
            }
            else if (_bReleaseTimer >= graceTime && aHold)
            {
                EnterCaughtState(_participantA, _participantB);
            }
        }

        private void ResolveStrengthContest()
        {
            float fallbackDuration = Settings.ReactionFallbackDuration;
            int countA = _participantA.InteractPressCount;
            int countB = _participantB.InteractPressCount;
            int diff = Mathf.Abs(countA - countB);
            int threshold = Settings.ContestThreshold;

            if (countA == 0 && countB == 0)
            {
                _lastOutcome = HandshakeOutcome.StrengthContestZeroZero;
                EmitArbitrationLog("[握手判定] 握住結束：雙方次數為 0 比 0（無人連按），搖一下再各自收手。");
                float shakeLen = ShakeDuration;
                _sessionState = HandshakeState.Shaking;
                _sessionTimer = shakeLen;
                _participantA.SetState(HandshakeState.Shaking, shakeLen);
                _participantB.SetState(HandshakeState.Shaking, shakeLen);
            }
            else if (diff > threshold)
            {
                _lastOutcome = HandshakeOutcome.StrengthContestWin;
                HandshakeParticipant winner = countA > countB ? _participantA : _participantB;
                HandshakeParticipant loser = countA > countB ? _participantB : _participantA;
                _contestWinner = winner;
                _contestLoser = loser;
                _winnerPressCount = Mathf.Max(countA, countB);
                _loserPressCount = Mathf.Min(countA, countB);

                EmitArbitrationLog($"[握手判定] 比力氣結果：勝負已分！勝方【{winner.EntityId}】({_winnerPressCount}次)，敗方【{loser.EntityId}】({_loserPressCount}次)，差距 {diff} 次超過門檻（{threshold}次）。");
                TriggerContestWinPresentation(winner, loser, fallbackDuration);
            }
            else
            {
                _lastOutcome = HandshakeOutcome.StrengthContestTie;
                _contestWinner = null;
                _contestLoser = null;
                _winnerPressCount = countA;
                _loserPressCount = countB;

                EmitArbitrationLog($"[握手判定] 比力氣結果：門檻內平手！雙方次數 [{countA} vs {countB}]，差距 {diff} 次在門檻（{threshold}次）內，拚力後慢慢鬆開退開再收手。");
                TriggerContestTiePresentation(fallbackDuration);
            }
        }

        private void TriggerContestWinPresentation(HandshakeParticipant winner, HandshakeParticipant loser, float fallbackLen)
        {
            AnimationClip triumph = TriumphClip;
            AnimationClip pain = PainClip;

            if (triumph != null || pain != null)
            {
                float tLen = triumph != null ? triumph.length : fallbackLen;
                float pLen = pain != null ? pain.length : fallbackLen;

                if (triumph != null) winner.SetState(HandshakeState.ContestTriumph, tLen);
                else winner.SetState(HandshakeState.Withdrawing, fallbackLen);

                if (pain != null) loser.SetState(HandshakeState.ContestPain, pLen);
                else loser.SetState(HandshakeState.Withdrawing, fallbackLen);

                _sessionState = HandshakeState.ContestTriumph;
                _sessionTimer = Mathf.Max(tLen, pLen);
            }
            else
            {
                winner.SetState(HandshakeState.Withdrawing, fallbackLen);
                loser.SetState(HandshakeState.Withdrawing, fallbackLen);
                _sessionState = HandshakeState.None;
                OnHandshakeFinished?.Invoke(_lastOutcome);
            }
        }

        private void TriggerContestTiePresentation(float fallbackLen)
        {
            AnimationClip slowClip = SlowWithdrawClip;
            if (slowClip != null)
            {
                float len = slowClip.length;
                _participantA.SetState(HandshakeState.ContestTie, len);
                _participantB.SetState(HandshakeState.ContestTie, len);
                _sessionState = HandshakeState.ContestTie;
                _sessionTimer = len;
            }
            else
            {
                _participantA.SetState(HandshakeState.Withdrawing, fallbackLen);
                _participantB.SetState(HandshakeState.Withdrawing, fallbackLen);
                _sessionState = HandshakeState.None;
                OnHandshakeFinished?.Invoke(_lastOutcome);
            }
        }

        private void EnterCaughtState(HandshakeParticipant catching, HandshakeParticipant caught)
        {
            _sessionState = HandshakeState.Catching;
            _catchingParticipant = catching;
            _caughtParticipant = caught;
            _lastOutcome = HandshakeOutcome.Caught;

            catching.SetState(HandshakeState.Catching);
            caught.SetState(HandshakeState.Caught);

            float threshold = GetEffectiveEscapeThreshold();
            EmitArbitrationLog($"[握手判定] 【{caught.EntityId}】放開超過容許時間，【{catching.EntityId}】按著抓住不放！進入拉扯狀態（骨架掙脫門檻: {threshold:F2}m）。");
        }

        private void UpdateCatchingState()
        {
            if (_participantA == null || _participantB == null || _catchingParticipant == null || _caughtParticipant == null)
            {
                _sessionState = HandshakeState.None;
                return;
            }

            if (!_catchingParticipant.CurrentIntent.InteractHold)
            {
                EmitArbitrationLog($"[握手判定] 抓者【{_catchingParticipant.EntityId}】亦放開互動鍵！雙方均已放開，握住結束，照累計連按次數判定比力氣！");
                ResolveStrengthContest();
                return;
            }

            float bodyDist = Vector3.Distance(_participantA.transform.position, _participantB.transform.position);
            float escapeThreshold = GetEffectiveEscapeThreshold();

            if (bodyDist > escapeThreshold)
            {
                ResolveEscape(bodyDist, escapeThreshold);
            }
        }

        /// <summary>
        /// Calculates the dynamic physical arm reach from both humanoid skeleton bone hierarchies.
        /// </summary>
        public float GetEffectiveEscapeThreshold()
        {
            float totalReach = 0f;
            if (_participantA != null) totalReach += MeasureArmReach(_participantA);
            if (_participantB != null) totalReach += MeasureArmReach(_participantB);

            if (totalReach > 1.0f)
            {
                return Mathf.Max(Settings.EscapeDistanceThreshold, totalReach);
            }
            return Settings.EscapeDistanceThreshold;
        }

        private float MeasureArmReach(HandshakeParticipant p)
        {
            Animator anim = p.GetComponentInChildren<Animator>();
            if (anim == null) return 0.90f;

            Transform shoulder = anim.GetBoneTransform(HumanBodyBones.RightUpperArm);
            Transform elbow = anim.GetBoneTransform(HumanBodyBones.RightLowerArm);
            Transform hand = anim.GetBoneTransform(HumanBodyBones.RightHand);
            Transform chest = anim.GetBoneTransform(HumanBodyBones.Chest) ?? anim.GetBoneTransform(HumanBodyBones.Spine);

            float armLength = 0f;
            if (shoulder != null && elbow != null)
            {
                armLength += Vector3.Distance(shoulder.position, elbow.position);
            }
            if (elbow != null && hand != null)
            {
                armLength += Vector3.Distance(elbow.position, hand.position);
            }

            float chestOffset = 0.25f;
            if (chest != null && shoulder != null)
            {
                chestOffset = Vector3.Distance(chest.position, shoulder.position);
            }

            if (armLength > 0.3f)
            {
                return armLength + chestOffset;
            }
            return 0.90f;
        }

        /// <summary>
        /// Restores mutual holding when a caught participant presses interact again during catch.
        /// Preserves accumulated press counts across the catch episode.
        /// </summary>
        public void NotifyCaughtParticipantRepressed(HandshakeParticipant participant)
        {
            if (_sessionState == HandshakeState.Catching && participant == _caughtParticipant)
            {
                EmitArbitrationLog($"[握手判定] 【{participant.EntityId}】重新按住互動鍵，拉扯結束，重回雙方握住 (Holding)！累計次數維持不歸零 [P1:{PlayerTaps} vs AI:{AiTaps}]。");
                EnterHoldingState(resetPressCount: false);
            }
        }

        private void ResolveEscape(float currentDistance, float threshold)
        {
            float fallbackDuration = Settings.ReactionFallbackDuration;
            _lastOutcome = HandshakeOutcome.Escaped;
            EmitArbitrationLog($"[握手判定] 雙方身體距離（{currentDistance:F2}m）超過骨架掙脫門檻（{threshold:F2}m）！【{_caughtParticipant.EntityId}】成功掙脫，不比次數！");

            AnimationClip stumble = StumbleClip;
            AnimationClip graspEmpty = GraspEmptyClip;

            if (stumble != null || graspEmpty != null)
            {
                float sLen = stumble != null ? stumble.length : fallbackDuration;
                float gLen = graspEmpty != null ? graspEmpty.length : fallbackDuration;

                if (stumble != null) _caughtParticipant.SetState(HandshakeState.EscapedStumble, sLen);
                else _caughtParticipant.SetState(HandshakeState.Withdrawing, fallbackDuration);

                if (graspEmpty != null) _catchingParticipant.SetState(HandshakeState.GraspEmpty, gLen);
                else _catchingParticipant.SetState(HandshakeState.Withdrawing, fallbackDuration);

                _sessionState = HandshakeState.EscapedStumble;
                _sessionTimer = Mathf.Max(sLen, gLen);
            }
            else
            {
                _caughtParticipant.SetState(HandshakeState.Withdrawing, fallbackDuration);
                _catchingParticipant.SetState(HandshakeState.Withdrawing, fallbackDuration);
                _sessionState = HandshakeState.None;
                OnHandshakeFinished?.Invoke(_lastOutcome);
            }
        }

        private void TriggerBothWithdrawPresentation(float fallbackWithdrawLen)
        {
            AnimationClip clap = ClapLaughClip;
            if (clap != null)
            {
                float clapLen = clap.length;
                _participantA.SetState(HandshakeState.ClapLaugh, clapLen);
                _participantB.SetState(HandshakeState.ClapLaugh, clapLen);
                _sessionState = HandshakeState.ClapLaugh;
                _sessionTimer = clapLen;
            }
            else
            {
                if (_participantA.State != HandshakeState.Withdrawing)
                {
                    _participantA.SetState(HandshakeState.Withdrawing, fallbackWithdrawLen);
                }
                if (_participantB.State != HandshakeState.Withdrawing)
                {
                    _participantB.SetState(HandshakeState.Withdrawing, fallbackWithdrawLen);
                }
                _sessionState = HandshakeState.None;
                OnHandshakeFinished?.Invoke(_lastOutcome);
            }
        }

        private void TriggerPrankPresentation(HandshakeParticipant pranker, HandshakeParticipant pranked, float fallbackWithdrawLen)
        {
            AnimationClip laugh = LaughClip;
            AnimationClip angry = AngryClip;

            if (laugh != null || angry != null)
            {
                float laughLen = laugh != null ? laugh.length : fallbackWithdrawLen;
                float angryLen = angry != null ? angry.length : fallbackWithdrawLen;

                if (laugh != null) pranker.SetState(HandshakeState.Laugh, laughLen);
                else if (pranker.State != HandshakeState.Withdrawing) pranker.SetState(HandshakeState.Withdrawing, fallbackWithdrawLen);

                if (angry != null) pranked.SetState(HandshakeState.Angry, angryLen);
                else pranked.SetState(HandshakeState.Withdrawing, fallbackWithdrawLen);

                _sessionState = HandshakeState.Laugh;
                _sessionTimer = Mathf.Max(laughLen, angryLen);
            }
            else
            {
                if (pranked.State != HandshakeState.Withdrawing)
                {
                    pranked.SetState(HandshakeState.Withdrawing, fallbackWithdrawLen);
                }
                _sessionState = HandshakeState.None;
                OnHandshakeFinished?.Invoke(_lastOutcome);
            }
        }

        private void UpdateReactionState()
        {
            _sessionTimer -= Time.deltaTime;
            if (_sessionTimer <= 0f)
            {
                float withdrawLen = WithdrawDuration;
                if (_participantA != null && _participantA.State != HandshakeState.None)
                {
                    _participantA.SetState(HandshakeState.Withdrawing, withdrawLen);
                }
                if (_participantB != null && _participantB.State != HandshakeState.None)
                {
                    _participantB.SetState(HandshakeState.Withdrawing, withdrawLen);
                }
                _sessionState = HandshakeState.None;
                OnHandshakeFinished?.Invoke(_lastOutcome);
            }
        }

        private void UpdateShakingState()
        {
            _sessionTimer -= Time.deltaTime;
            if (_sessionTimer <= 0f)
            {
                float withdrawLen = WithdrawDuration;
                EmitArbitrationLog($"[握手判定] 搖一下結束，雙方各自收手（{withdrawLen:F2}s）。");

                if (_participantA != null) _participantA.SetState(HandshakeState.Withdrawing, withdrawLen);
                if (_participantB != null) _participantB.SetState(HandshakeState.Withdrawing, withdrawLen);

                _sessionState = HandshakeState.None;
                OnHandshakeFinished?.Invoke(_lastOutcome);
            }
        }

        private void EmitArbitrationLog(string message)
        {
            OnArbitrationLog?.Invoke(message);
            var hud = ObjectUtility.FindObject<WhiteRoomHUD>(false);
            if (hud != null)
            {
                hud.LogAction(message);
            }
        }

        private void HandleGlobalReset()
        {
            _sessionState = HandshakeState.None;
            _sessionTimer = 0f;
            _lastOutcome = HandshakeOutcome.None;
            _participantAReleasedDuringGrip = false;
            _participantBReleasedDuringGrip = false;
            _participantAReleaseTimestamp = -1f;
            _participantBReleaseTimestamp = -1f;
            _lastPranker = null;
            _lastPranked = null;
            _aReleaseTimer = 0f;
            _bReleaseTimer = 0f;
            _contestWinner = null;
            _contestLoser = null;
            _winnerPressCount = 0;
            _loserPressCount = 0;
            _catchingParticipant = null;
            _caughtParticipant = null;
            _participantA = null;
            _participantB = null;
        }

        /// <summary>
        /// Triggers the shared global arena reset across all entities, AI controllers, and handshake session state.
        /// </summary>
        public void ResetAll()
        {
            CombatEntity.ResetAllArenaEntities();
        }
    }
}
