using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yu5h1Lib;
using Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom;
using Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom.Handshake;

namespace Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom.Tests
{
    /// <summary>
    /// Supplemental PlayMode verification runner for the Stage 2b handshake interaction.
    /// Lives in the Tests folder outside the playable sample scene so entering Play Mode in Handshake.unity
    /// remains immediately player-controllable.
    /// </summary>
    public class HandshakeVerificationRunner : MonoBehaviour
    {
        public bool IsFinished { get; private set; }
        public string TestReport { get; private set; } = "";
        public bool AllPassed { get; private set; }

        private readonly List<string> _results = new List<string>();
        private WhiteRoomCharacter _player;
        private HandshakeParticipant _playerParticipant;
        private HandshakeAiController _ai;
        private HandshakeParticipant _aiParticipant;
        private HandshakeArbitrator _arb;

        private bool _case1Pass;
        private bool _case2Pass;
        private bool _case3Pass;
        private bool _case4Pass;
        private bool _case5Pass;
        private bool _case6Pass;
        private bool _case7Pass;

        private void Start()
        {
            StartCoroutine(RunAllTestsRoutine());
        }

        private IEnumerator RunAllTestsRoutine()
        {
            _player = ObjectUtility.FindObject<WhiteRoomCharacter>(false);
            _playerParticipant = _player != null ? _player.GetComponent<HandshakeParticipant>() : null;
            _ai = ObjectUtility.FindObject<HandshakeAiController>(false);
            _aiParticipant = _ai != null ? _ai.GetComponent<HandshakeParticipant>() : null;
            _arb = HandshakeArbitrator.Instance;

            if (_player == null || _playerParticipant == null || _ai == null || _aiParticipant == null || _arb == null)
            {
                TestReport = "Missing required Handshake components in scene.";
                IsFinished = true;
                yield break;
            }

            _player.OverrideIntent = true;
            yield return new WaitForSeconds(0.2f);

            _results.Add("======================================================================");
            _results.Add("  WhiteRoom Stage 2b Handshake Supplemental Automated Check (6000.6.0f1)");
            _results.Add("======================================================================");

            yield return StartCoroutine(Case1_PlayerCancel());
            yield return StartCoroutine(Case2_AiInitiate_PlayerRespond_Success());
            yield return StartCoroutine(Case3_ContinuousHoldDoesNotReinitiate());
            yield return StartCoroutine(Case4_PlayerInitiate_AiRespond_Success());
            yield return StartCoroutine(Case5_EarlyReleaseDuringGrip());
            yield return StartCoroutine(Case6_AiCancel());
            yield return StartCoroutine(Case7_ResetConsistency());

            AllPassed = _case1Pass && _case2Pass && _case3Pass && _case4Pass && _case5Pass && _case6Pass && _case7Pass;
            _results.Add("======================================================================");
            _results.Add($"  Result: {(AllPassed ? "ALL PASSED (7/7)" : "FAILED")}");
            _results.Add("======================================================================");

            TestReport = string.Join("\n", _results);
            IsFinished = true;
            _player.OverrideIntent = false;
            Debug.Log(TestReport);
        }

        private IEnumerator Case1_PlayerCancel()
        {
            _arb.ResetAll();
            _ai.Mode = HandshakeAiMode.Idle;
            _player.CustomIntent = CombatActionIntent.Empty;
            yield return new WaitForSeconds(0.15f);

            var intent = CombatActionIntent.Empty;
            intent.InteractHold = true;
            intent.InteractDown = true;
            _player.CustomIntent = intent;
            yield return new WaitForSeconds(0.15f);

            bool reaching = (_playerParticipant.State == HandshakeState.Reaching);

            intent.InteractHold = false;
            intent.InteractDown = false;
            intent.InteractUp = true;
            _player.CustomIntent = intent;
            yield return new WaitForSeconds(0.10f);

            bool withdrawing = (_playerParticipant.State == HandshakeState.Withdrawing);

            intent.InteractUp = false;
            _player.CustomIntent = intent;
            yield return new WaitForSeconds(0.55f);

            bool backToNone = (_playerParticipant.State == HandshakeState.None);
            _case1Pass = reaching && withdrawing && backToNone && (_arb.LastOutcome == HandshakeOutcome.Cancelled);
            _results.Add($"[Case 1] Player initiate then release (Cancel): {(_case1Pass ? "PASS" : "FAIL")} (Reaching={reaching}, Withdrawing={withdrawing}, None={backToNone})");
        }

        private IEnumerator Case2_AiInitiate_PlayerRespond_Success()
        {
            _arb.ResetAll();
            _ai.Mode = HandshakeAiMode.AiInitiator;
            _ai.ReachDelay = 0.2f;
            _player.CustomIntent = CombatActionIntent.Empty;

            float waitTimer = 0f;
            while (_aiParticipant.State != HandshakeState.Waiting && waitTimer < 2.0f)
            {
                waitTimer += Time.deltaTime;
                yield return null;
            }
            bool aiWaiting = (_aiParticipant.State == HandshakeState.Waiting);

            float elapsed = 0f;
            bool reachedGrip = false;
            while (elapsed < 4.0f && !reachedGrip)
            {
                var intent = CombatActionIntent.Empty;
                intent.InteractHold = true;
                intent.MoveInput = new Vector2(0f, 1f);
                _player.CustomIntent = intent;

                if (_arb.SessionState == HandshakeState.Grip)
                {
                    reachedGrip = true;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            float gripElapsed = 0f;
            bool reachedShaking = false;
            while (gripElapsed < 1.0f)
            {
                var intent = CombatActionIntent.Empty;
                intent.InteractHold = true;
                _player.CustomIntent = intent;

                if (_arb.SessionState == HandshakeState.Shaking)
                {
                    reachedShaking = true;
                }
                gripElapsed += Time.deltaTime;
                yield return null;
            }

            bool outcomeSuccess = (_arb.LastOutcome == HandshakeOutcome.Success);
            _case2Pass = aiWaiting && reachedGrip && reachedShaking && outcomeSuccess;
            _results.Add($"[Case 2] AI initiate, player walk-in & grip to completion (Success): {(_case2Pass ? "PASS" : "FAIL")} (AiWaiting={aiWaiting}, Grip={reachedGrip}, Shake={reachedShaking}, Outcome={_arb.LastOutcome})");
        }

        private IEnumerator Case3_ContinuousHoldDoesNotReinitiate()
        {
            var intent = CombatActionIntent.Empty;
            intent.InteractHold = true;
            _player.CustomIntent = intent;

            yield return new WaitForSeconds(0.8f);

            bool stayedInNone = (_playerParticipant.State == HandshakeState.None);
            bool latchActive = _playerParticipant.RequiresInteractRelease;

            intent.InteractHold = false;
            intent.InteractUp = true;
            _player.CustomIntent = intent;
            yield return new WaitForSeconds(0.1f);

            bool latchCleared = !_playerParticipant.RequiresInteractRelease;

            intent.InteractUp = false;
            intent.InteractHold = true;
            intent.InteractDown = true;
            _player.CustomIntent = intent;
            yield return new WaitForSeconds(0.1f);

            bool reachedAfterRepress = (_playerParticipant.State == HandshakeState.Reaching);

            _player.CustomIntent = CombatActionIntent.Empty;
            _case3Pass = stayedInNone && latchActive && latchCleared && reachedAfterRepress;
            _results.Add($"[Case 3] Holding interact after finish stays standing until released and re-pressed: {(_case3Pass ? "PASS" : "FAIL")} (StayedNone={stayedInNone}, Latch={latchActive}, ClearedOnRelease={latchCleared}, ReachedOnRepress={reachedAfterRepress})");
        }

        private IEnumerator Case4_PlayerInitiate_AiRespond_Success()
        {
            _arb.ResetAll();
            _ai.Mode = HandshakeAiMode.AiResponder;
            _player.CustomIntent = CombatActionIntent.Empty;
            yield return new WaitForSeconds(0.2f);

            float elapsed = 0f;
            bool reachedGrip = false;
            while (elapsed < 5.0f && !reachedGrip)
            {
                var intent = CombatActionIntent.Empty;
                intent.InteractHold = true;
                _player.CustomIntent = intent;

                if (_arb.SessionState == HandshakeState.Grip)
                {
                    reachedGrip = true;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            float gripElapsed = 0f;
            bool reachedShaking = false;
            while (gripElapsed < 1.0f)
            {
                var intent = CombatActionIntent.Empty;
                intent.InteractHold = true;
                _player.CustomIntent = intent;

                if (_arb.SessionState == HandshakeState.Shaking)
                {
                    reachedShaking = true;
                }
                gripElapsed += Time.deltaTime;
                yield return null;
            }

            bool outcomeSuccess = (_arb.LastOutcome == HandshakeOutcome.Success);
            yield return new WaitForSeconds(0.7f);

            _case4Pass = reachedGrip && reachedShaking && outcomeSuccess;
            _results.Add($"[Case 4] Player initiate, AI walk-in & respond (Role Swap Success): {(_case4Pass ? "PASS" : "FAIL")} (Grip={reachedGrip}, Shake={reachedShaking}, Outcome={_arb.LastOutcome})");
        }

        private IEnumerator Case5_EarlyReleaseDuringGrip()
        {
            _arb.ResetAll();
            _ai.Mode = HandshakeAiMode.AiEarlyRelease;
            _ai.ReachDelay = 0.1f;
            _player.CustomIntent = CombatActionIntent.Empty;
            yield return new WaitForSeconds(0.2f);

            float elapsed = 0f;
            bool reachedGrip = false;
            while (elapsed < 4.0f && !reachedGrip)
            {
                var intent = CombatActionIntent.Empty;
                intent.InteractHold = true;
                intent.MoveInput = new Vector2(0f, 1f);
                _player.CustomIntent = intent;

                if (_arb.SessionState == HandshakeState.Grip)
                {
                    reachedGrip = true;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            yield return new WaitForSeconds(0.35f);

            bool earlyReleaseTriggered = (_arb.LastOutcome == HandshakeOutcome.EarlyReleaseDuringGrip);
            bool sessionClosed = (_arb.SessionState == HandshakeState.None);

            _case5Pass = reachedGrip && earlyReleaseTriggered && sessionClosed;
            _results.Add($"[Case 5] Release during grip (Batch 2 placeholder withdraw): {(_case5Pass ? "PASS" : "FAIL")} (Grip={reachedGrip}, EarlyRelease={earlyReleaseTriggered}, Closed={sessionClosed})");
        }

        private IEnumerator Case6_AiCancel()
        {
            _arb.ResetAll();
            _ai.Mode = HandshakeAiMode.AiCancel;
            _ai.ReachDelay = 0.2f;
            _ai.HoldDuration = 0.8f;
            _player.CustomIntent = CombatActionIntent.Empty;
            yield return new WaitForSeconds(0.4f);

            bool aiReached = (_aiParticipant.State == HandshakeState.Reaching || _aiParticipant.State == HandshakeState.Waiting);

            yield return new WaitForSeconds(0.8f);
            bool aiWithdrawingOrNone = (_aiParticipant.State == HandshakeState.Withdrawing || _aiParticipant.State == HandshakeState.None);

            _case6Pass = aiReached && aiWithdrawingOrNone;
            _results.Add($"[Case 6] Configurable AI initiate then release (AI Cancel): {(_case6Pass ? "PASS" : "FAIL")} (AiReached={aiReached}, AiWithdrew={aiWithdrawingOrNone})");
        }

        private IEnumerator Case7_ResetConsistency()
        {
            _arb.ResetAll();
            _ai.Mode = HandshakeAiMode.AiInitiator;
            _player.CustomIntent = CombatActionIntent.Empty;
            yield return new WaitForSeconds(0.15f);

            bool pClean = (_playerParticipant.State == HandshakeState.None && Mathf.Abs(_playerParticipant.transform.position.z - (-1.2f)) < 0.1f);
            bool aiClean = (_aiParticipant.State == HandshakeState.None && Mathf.Abs(_aiParticipant.transform.position.z - 1.2f) < 0.1f);
            bool arbClean = (_arb.SessionState == HandshakeState.None && _arb.LastOutcome == HandshakeOutcome.None);

            _case7Pass = pClean && aiClean && arbClean;
            _results.Add($"[Case 7] Global arena reset consistency: {(_case7Pass ? "PASS" : "FAIL")} (PlayerClean={pClean}, AiClean={aiClean}, ArbClean={arbClean})");
        }
    }
}
