using System;
using System.Collections.Generic;
using UnityEngine;
using Yu5h1Lib;
using Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom;

namespace Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom.Handshake
{
    /// <summary>
    /// Authoritative arbitrator for the Stage 2b handshake interaction.
    /// Determines contact solely from actual right-hand collider trigger events between the two touching hands,
    /// and drives grip, shake, and withdraw durations from the imported humanoid animation clips.
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

        [Header("Handshake Humanoid Animation Clips")]
        [SerializeField] private AnimationClip _reachClip;
        [SerializeField] private AnimationClip _waitClip;
        [SerializeField] private AnimationClip _withdrawClip;
        [SerializeField] private AnimationClip _gripClip;
        [SerializeField] private AnimationClip _shakeClip;

        [Header("Batch 2 Reaction Humanoid Animation Clips")]
        [SerializeField] private AnimationClip _laughClip;
        [SerializeField] private AnimationClip _angryClip;
        [SerializeField] private AnimationClip _clapLaughClip;

        private readonly List<HandshakeParticipant> _participants = new List<HandshakeParticipant>();
        private HandshakeParticipant _participantA;
        private HandshakeParticipant _participantB;
        private bool _participantAReleasedDuringGrip = false;
        private bool _participantBReleasedDuringGrip = false;
        private HandshakeParticipant _lastPranker;
        private HandshakeParticipant _lastPranked;

        private HandshakeState _sessionState = HandshakeState.None;
        private float _sessionTimer = 0f;
        private HandshakeOutcome _lastOutcome = HandshakeOutcome.None;

        public HandshakeState SessionState => _sessionState;
        public HandshakeOutcome LastOutcome => _lastOutcome;
        public HandshakeParticipant ParticipantA => _participantA;
        public HandshakeParticipant ParticipantB => _participantB;
        public HandshakeParticipant LastPranker => _lastPranker;
        public HandshakeParticipant LastPranked => _lastPranked;
        public bool ParticipantAReleasedDuringGrip => _participantAReleasedDuringGrip;
        public bool ParticipantBReleasedDuringGrip => _participantBReleasedDuringGrip;

        public AnimationClip LaughClip => _laughClip;
        public AnimationClip AngryClip => _angryClip;
        public AnimationClip ClapLaughClip => _clapLaughClip;

        public float GripDuration => _gripClip != null ? _gripClip.length : 0f;
        public float ShakeDuration => _shakeClip != null ? _shakeClip.length : 0f;
        public float ReachDuration => _reachClip != null ? _reachClip.length : 0f;
        public float WithdrawDuration => _withdrawClip != null ? _withdrawClip.length : 0f;

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
        /// Assigns the five humanoid animation clips so state durations are read directly from clip lengths.
        /// </summary>
        public void ConfigureClips(AnimationClip reach, AnimationClip wait, AnimationClip withdraw, AnimationClip grip, AnimationClip shake)
        {
            _reachClip = reach;
            _waitClip = wait;
            _withdrawClip = withdraw;
            _gripClip = grip;
            _shakeClip = shake;

            foreach (var p in _participants)
            {
                ApplyClipDurationsToParticipant(p);
            }
        }

        /// <summary>
        /// Configures the optional Batch 2 reaction humanoid animation clips (laugh, angry, clap-laugh).
        /// </summary>
        public void ConfigureBatch2Clips(AnimationClip laugh, AnimationClip angry, AnimationClip clapLaugh)
        {
            _laughClip = laugh;
            _angryClip = angry;
            _clapLaughClip = clapLaugh;
        }

        private void ApplyClipDurationsToParticipant(HandshakeParticipant participant)
        {
            if (participant == null) return;
            if (_reachClip != null) participant.ReachDuration = _reachClip.length;
            if (_withdrawClip != null) participant.WithdrawDuration = _withdrawClip.length;
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
            else if (_sessionState == HandshakeState.Shaking)
            {
                UpdateShakingState();
            }
            else if (_sessionState == HandshakeState.Laugh || _sessionState == HandshakeState.ClapLaugh)
            {
                UpdateReactionState();
            }
        }

        private void TryInitiateGrip(HandshakeParticipant p1, HandshakeParticipant p2)
        {
            if (_sessionState != HandshakeState.None) return;
            if (_gripClip == null || _shakeClip == null || _withdrawClip == null) return;

            bool p1Ready = (p1.State == HandshakeState.Reaching || p1.State == HandshakeState.Waiting);
            bool p2Ready = (p2.State == HandshakeState.Reaching || p2.State == HandshakeState.Waiting);

            if (!p1Ready || !p2Ready) return;

            _participantA = p1;
            _participantB = p2;
            _participantAReleasedDuringGrip = false;
            _participantBReleasedDuringGrip = false;
            _lastPranker = null;
            _lastPranked = null;

            float gripLen = _gripClip.length;
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
            float withdrawLen = _withdrawClip != null ? _withdrawClip.length : 0.50f;

            if (!aHolding && !_participantAReleasedDuringGrip)
            {
                _participantAReleasedDuringGrip = true;
                _participantA.SetState(HandshakeState.Withdrawing, withdrawLen);
                EmitArbitrationLog($"[握手判定] [{_participantA.EntityId}] 於握緊期間放開互動鍵！先行抽回收手（{withdrawLen:F2}s），持續判定至握緊結束。");
            }

            if (!bHolding && !_participantBReleasedDuringGrip)
            {
                _participantBReleasedDuringGrip = true;
                _participantB.SetState(HandshakeState.Withdrawing, withdrawLen);
                EmitArbitrationLog($"[握手判定] [{_participantB.EntityId}] 於握緊期間放開互動鍵！先行抽回收手（{withdrawLen:F2}s），持續判定至握緊結束。");
            }

            _sessionTimer -= Time.deltaTime;
            if (_sessionTimer <= 0f)
            {
                ResolveGripOutcome();
            }
        }

        private void ResolveGripOutcome()
        {
            float withdrawLen = _withdrawClip != null ? _withdrawClip.length : 0.50f;

            if (!_participantAReleasedDuringGrip && !_participantBReleasedDuringGrip)
            {
                float shakeLen = _shakeClip != null ? _shakeClip.length : 0.50f;
                _lastOutcome = HandshakeOutcome.Success;
                _lastPranker = null;
                _lastPranked = null;
                EmitArbitrationLog($"[握手判定] 握手成功！雙方按住到握緊結束，開始搖一下（{shakeLen:F2}s）。");

                _sessionState = HandshakeState.Shaking;
                _sessionTimer = shakeLen;

                _participantA.SetState(HandshakeState.Shaking, shakeLen);
                _participantB.SetState(HandshakeState.Shaking, shakeLen);
            }
            else if (_participantAReleasedDuringGrip && _participantBReleasedDuringGrip)
            {
                _lastOutcome = HandshakeOutcome.BothWithdraw;
                _lastPranker = null;
                _lastPranked = null;
                EmitArbitrationLog("[握手判定] 判定結果：同時收手 (BothWithdraw)！雙方均在握緊結束前放開。");

                TriggerBothWithdrawPresentation(withdrawLen);
            }
            else
            {
                _lastOutcome = HandshakeOutcome.Prank;
                HandshakeParticipant pranker = _participantAReleasedDuringGrip ? _participantA : _participantB;
                HandshakeParticipant pranked = _participantAReleasedDuringGrip ? _participantB : _participantA;
                _lastPranker = pranker;
                _lastPranked = pranked;
                EmitArbitrationLog($"[握手判定] 判定結果：惡作劇 (Prank)！放開者為【{pranker.EntityId}】，被惡作劇者為【{pranked.EntityId}】。");

                TriggerPrankPresentation(pranker, pranked, withdrawLen);
            }
        }

        private void TriggerBothWithdrawPresentation(float fallbackWithdrawLen)
        {
            if (_clapLaughClip != null)
            {
                float clapLen = _clapLaughClip.length;
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
            if (_laughClip != null && _angryClip != null)
            {
                float laughLen = _laughClip.length;
                float angryLen = _angryClip.length;
                pranker.SetState(HandshakeState.Laugh, laughLen);
                pranked.SetState(HandshakeState.Angry, angryLen);
                _sessionState = HandshakeState.Laugh;
                _sessionTimer = Mathf.Max(laughLen, angryLen);
            }
            else
            {
                if (pranker.State != HandshakeState.Withdrawing)
                {
                    pranker.SetState(HandshakeState.Withdrawing, fallbackWithdrawLen);
                }
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
                float withdrawLen = _withdrawClip != null ? _withdrawClip.length : 0.50f;
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
                float withdrawLen = _withdrawClip != null ? _withdrawClip.length : 0.50f;
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
            _lastPranker = null;
            _lastPranked = null;
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
