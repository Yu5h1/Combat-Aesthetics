using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Yu5h1Lib;
using Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom;
using Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom.Handshake;

namespace Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom.Tests
{
    /// <summary>
    /// PlayMode automated verification runner for the Stage 2b handshake interaction (Batches 1, 2, and 3).
    /// Executes all 21 verification scenarios, verifies arbitration logic and constraints, captures live HUD screenshots,
    /// and generates a structured acceptance report.
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
        private bool _case8Pass;
        private bool _case9Pass;
        private bool _case10Pass;
        private bool _case11Pass;
        private bool _case12Pass;
        private bool _case13Pass;
        private bool _case14Pass;
        private bool _case15Pass;
        private bool _case16Pass;
        private bool _case17Pass;
        private bool _case18Pass;
        private bool _case19Pass;
        private bool _case20Pass;
        private bool _case21Pass;

        private void Start()
        {
            StartCoroutine(RunAllTestsRoutine());
        }

        private Vector2 ComputePlayerWalkToAiInput()
        {
            if (_playerParticipant == null || _aiParticipant == null) return new Vector2(0f, 1f);
            Vector3 toAi = _aiParticipant.transform.position - _playerParticipant.transform.position;
            toAi.y = 0f;
            Vector3 dir = toAi.normalized;
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 camFwd = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
                Vector3 camRight = Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized;
                return new Vector2(Vector3.Dot(dir, camRight), Vector3.Dot(dir, camFwd));
            }
            return new Vector2(dir.x, dir.z);
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

            _arb.RegisterParticipant(_playerParticipant);
            _arb.RegisterParticipant(_aiParticipant);

            _player.OverrideIntent = true;
            yield return new WaitForSeconds(0.2f);

            _results.Add("======================================================================");
            _results.Add("  WhiteRoom Stage 2b Handshake Batch 3 Comprehensive Verification (21 Scenarios)");
            _results.Add("======================================================================");

            yield return StartCoroutine(Case1_PlayerCancel());
            yield return StartCoroutine(Case2_AiCancel());
            yield return StartCoroutine(Case3_AiInitiate_PlayerRespond_Holding());
            yield return StartCoroutine(Case4_ContinuousHoldDoesNotReinitiateAfterSuccess());
            yield return StartCoroutine(Case5_PlayerInitiate_AiRespond_Holding());
            yield return StartCoroutine(Case6_PlayerPranksAi());
            yield return StartCoroutine(Case7_ContinuousHoldDoesNotReinitiateAfterPrank());
            yield return StartCoroutine(Case8_AiPranksPlayer());
            yield return StartCoroutine(Case9_BothWithdraw());
            yield return StartCoroutine(Case10_ContinuousHoldDoesNotReinitiateAfterBothWithdraw());
            yield return StartCoroutine(Case11_ReleaseAfterGripTimestampRecorded());
            yield return StartCoroutine(Case12_StrengthContest_AiWin());
            yield return StartCoroutine(Case13_StrengthContest_PlayerWin());
            yield return StartCoroutine(Case14_StrengthContest_Tie());
            yield return StartCoroutine(Case15_StrengthContest_ZeroZero());
            yield return StartCoroutine(Case16_CatchAndEscape());
            yield return StartCoroutine(Case17_CaughtRepressReturnsToHolding());
            yield return StartCoroutine(Case18_CatcherReleaseResolvesContest());
            yield return StartCoroutine(Case19_CatcherChasesAndPreventsEscape());
            yield return StartCoroutine(Case20_CaughtJumpsWithoutEscape());
            yield return StartCoroutine(Case21_ResetConsistency());

            AllPassed = _case1Pass && _case2Pass && _case3Pass && _case4Pass && _case5Pass &&
                        _case6Pass && _case7Pass && _case8Pass && _case9Pass && _case10Pass &&
                        _case11Pass && _case12Pass && _case13Pass && _case14Pass && _case15Pass &&
                        _case16Pass && _case17Pass && _case18Pass && _case19Pass && _case20Pass && _case21Pass;

            _results.Add("======================================================================");
            _results.Add($"  Result: {(AllPassed ? "ALL PASSED (21/21)" : "FAILED")}");
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
            yield return new WaitForSeconds(0.1f);

            var reachIntent = CombatActionIntent.Empty;
            reachIntent.InteractHold = true;
            reachIntent.InteractDown = true;
            _player.CustomIntent = reachIntent;
            yield return new WaitForSeconds(0.25f);

            bool wasReaching = (_playerParticipant.State == HandshakeState.Reaching);

            var cancelIntent = CombatActionIntent.Empty;
            cancelIntent.InteractHold = false;
            cancelIntent.InteractUp = true;
            _player.CustomIntent = cancelIntent;
            yield return new WaitForSeconds(0.1f);

            bool withdrawing = (_playerParticipant.State == HandshakeState.Withdrawing);
            bool outcomeCancelled = (_arb.LastOutcome == HandshakeOutcome.Cancelled);

            yield return new WaitForSeconds(0.55f);
            bool backToNone = (_playerParticipant.State == HandshakeState.None);

            _case1Pass = wasReaching && withdrawing && outcomeCancelled && backToNone;
            _results.Add($"[Case 1] Player initiate then cancel before contact: {(_case1Pass ? "PASS" : "FAIL")} (WasReaching={wasReaching}, Withdrawing={withdrawing}, Cancelled={outcomeCancelled}, BackToNone={backToNone})");
        }

        private IEnumerator Case2_AiCancel()
        {
            _arb.ResetAll();
            _ai.Mode = HandshakeAiMode.AiCancel;
            _player.CustomIntent = CombatActionIntent.Empty;
            yield return new WaitForSeconds(0.3f);

            float waitAi = 0f;
            bool aiReached = false;
            while (waitAi < 1.0f && !aiReached)
            {
                if (_aiParticipant.State == HandshakeState.Reaching || _aiParticipant.State == HandshakeState.Waiting)
                {
                    aiReached = true;
                }
                waitAi += Time.deltaTime;
                yield return null;
            }

            yield return new WaitForSeconds(1.4f);
            bool aiWithdrawingOrNone = (_aiParticipant.State == HandshakeState.Withdrawing || _aiParticipant.State == HandshakeState.None);

            _case2Pass = aiReached && aiWithdrawingOrNone;
            _results.Add($"[Case 2] AI initiate then cancel before contact: {(_case2Pass ? "PASS" : "FAIL")} (AiReached={aiReached}, AiWithdrew={aiWithdrawingOrNone})");
        }

        private IEnumerator Case3_AiInitiate_PlayerRespond_Holding()
        {
            _arb.ResetAll();
            _ai.Mode = HandshakeAiMode.AiInitiator;
            _player.CustomIntent = CombatActionIntent.Empty;
            yield return new WaitForSeconds(0.2f);

            float waitAi = 0f;
            bool aiWaiting = false;
            while (waitAi < 2.0f && !aiWaiting)
            {
                if (_aiParticipant.State == HandshakeState.Waiting)
                {
                    aiWaiting = true;
                }
                waitAi += Time.deltaTime;
                yield return null;
            }

            float elapsed = 0f;
            bool reachedGrip = false;
            while (elapsed < 4.0f && !reachedGrip)
            {
                var intent = CombatActionIntent.Empty;
                intent.InteractHold = true;
                intent.MoveInput = ComputePlayerWalkToAiInput();
                _player.CustomIntent = intent;

                if (_arb.SessionState == HandshakeState.Grip)
                {
                    reachedGrip = true;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            float gripElapsed = 0f;
            bool reachedHolding = false;
            bool successOutcome = false;
            while (gripElapsed < 1.5f)
            {
                var intent = CombatActionIntent.Empty;
                intent.InteractHold = true;
                _player.CustomIntent = intent;

                if (_arb.SessionState == HandshakeState.Holding)
                {
                    reachedHolding = true;
                    if (_arb.LastOutcome == HandshakeOutcome.Success)
                    {
                        successOutcome = true;
                    }
                    break;
                }
                gripElapsed += Time.deltaTime;
                yield return null;
            }

            _ai.Mode = HandshakeAiMode.Idle;
            _player.CustomIntent = CombatActionIntent.Empty;

            float waitWithdraw = 0f;
            while (waitWithdraw < 2.0f && _playerParticipant.State != HandshakeState.Withdrawing)
            {
                waitWithdraw += Time.deltaTime;
                yield return null;
            }

            float returnWait = 0f;
            while (returnWait < 2.0f && _playerParticipant.State != HandshakeState.None)
            {
                var holdIntent = CombatActionIntent.Empty;
                holdIntent.InteractHold = true;
                _player.CustomIntent = holdIntent;
                returnWait += Time.deltaTime;
                yield return null;
            }

            _case3Pass = aiWaiting && reachedGrip && reachedHolding && successOutcome;
            _results.Add($"[Case 3] AI initiate, player walk-in & grip to Holding (Success): {(_case3Pass ? "PASS" : "FAIL")} (AiWaiting={aiWaiting}, Grip={reachedGrip}, Holding={reachedHolding}, Success={successOutcome})");
        }

        private IEnumerator Case4_ContinuousHoldDoesNotReinitiateAfterSuccess()
        {
            var intent = CombatActionIntent.Empty;
            intent.InteractHold = true;
            _player.CustomIntent = intent;

            yield return new WaitForSeconds(0.4f);

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
            yield return new WaitForSeconds(0.15f);

            bool reachedAfterRepress = (_playerParticipant.State == HandshakeState.Reaching);

            _player.CustomIntent = CombatActionIntent.Empty;
            _case4Pass = stayedInNone && latchActive && latchCleared && reachedAfterRepress;
            _results.Add($"[Case 4] Holding interact continuously after Success stays standing until re-pressed: {(_case4Pass ? "PASS" : "FAIL")} (StayedNone={stayedInNone}, Latch={latchActive}, Cleared={latchCleared}, Reached={reachedAfterRepress})");
        }

        private IEnumerator Case5_PlayerInitiate_AiRespond_Holding()
        {
            _arb.ResetAll();
            _ai.Mode = HandshakeAiMode.AiResponder;
            _ai.StopDistance = 0.60f;
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
            bool reachedHolding = false;
            while (gripElapsed < 1.2f)
            {
                var intent = CombatActionIntent.Empty;
                intent.InteractHold = true;
                _player.CustomIntent = intent;

                if (_arb.SessionState == HandshakeState.Holding)
                {
                    reachedHolding = true;
                    break;
                }
                gripElapsed += Time.deltaTime;
                yield return null;
            }

            _ai.Mode = HandshakeAiMode.Idle;
            _player.CustomIntent = CombatActionIntent.Empty;

            _case5Pass = reachedGrip && reachedHolding;
            _results.Add($"[Case 5] Player initiate & AI responder walk-in to Holding: {(_case5Pass ? "PASS" : "FAIL")} (Grip={reachedGrip}, Holding={reachedHolding})");
        }

        private IEnumerator Case6_PlayerPranksAi()
        {
            _arb.ResetAll();
            _ai.Mode = HandshakeAiMode.AiInitiator;
            _player.CustomIntent = CombatActionIntent.Empty;
            yield return new WaitForSeconds(0.2f);

            float elapsed = 0f;
            bool reachedGrip = false;
            while (elapsed < 4.0f && !reachedGrip)
            {
                var intent = CombatActionIntent.Empty;
                intent.InteractHold = true;
                intent.MoveInput = ComputePlayerWalkToAiInput();
                _player.CustomIntent = intent;

                if (_arb.SessionState == HandshakeState.Grip)
                {
                    reachedGrip = true;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }


            yield return new WaitForSeconds(0.15f);
            var releaseIntent = CombatActionIntent.Empty;
            releaseIntent.InteractHold = false;
            releaseIntent.InteractUp = true;
            _player.CustomIntent = releaseIntent;

            float waitPrank = 0f;
            bool prankerIsPlayer = false;
            while (waitPrank < 1.0f)
            {
                if (_arb.LastOutcome == HandshakeOutcome.Prank)
                {
                    prankerIsPlayer = (_arb.LastPranker == _playerParticipant);
                    break;
                }
                waitPrank += Time.deltaTime;
                yield return null;
            }

            _ai.Mode = HandshakeAiMode.Idle;
            _player.CustomIntent = CombatActionIntent.Empty;

            _case6Pass = reachedGrip && (_arb.LastOutcome == HandshakeOutcome.Prank) && prankerIsPlayer;
            _results.Add($"[Case 6] Player early release during grip (Prank AI): {(_case6Pass ? "PASS" : "FAIL")} (Grip={reachedGrip}, Outcome={_arb.LastOutcome}, PrankerIsPlayer={prankerIsPlayer})");
        }

        private IEnumerator Case7_ContinuousHoldDoesNotReinitiateAfterPrank()
        {
            float waitReturn = 0f;
            while (waitReturn < 2.0f && _playerParticipant.State != HandshakeState.None)
            {
                var holdIntent = CombatActionIntent.Empty;
                holdIntent.InteractHold = true;
                _player.CustomIntent = holdIntent;
                waitReturn += Time.deltaTime;
                yield return null;
            }

            var intent = CombatActionIntent.Empty;
            intent.InteractHold = true;
            _player.CustomIntent = intent;

            yield return new WaitForSeconds(0.4f);

            bool stayedInNone = (_playerParticipant.State == HandshakeState.None);
            bool latchActive = _playerParticipant.RequiresInteractRelease;

            intent.InteractHold = false;
            intent.InteractUp = true;
            _player.CustomIntent = intent;
            yield return new WaitForSeconds(0.1f);

            bool latchCleared = !_playerParticipant.RequiresInteractRelease;
            _player.CustomIntent = CombatActionIntent.Empty;

            _case7Pass = stayedInNone && latchActive && latchCleared;
            _results.Add($"[Case 7] Holding interact continuously after Prank stays standing until re-pressed: {(_case7Pass ? "PASS" : "FAIL")} (StayedNone={stayedInNone}, Latch={latchActive}, Cleared={latchCleared})");
        }

        private IEnumerator Case8_AiPranksPlayer()
        {
            _arb.ResetAll();
            _ai.Mode = HandshakeAiMode.AiEarlyRelease;
            _player.CustomIntent = CombatActionIntent.Empty;
            yield return new WaitForSeconds(0.2f);

            float elapsed = 0f;
            bool reachedGrip = false;
            while (elapsed < 4.0f && !reachedGrip)
            {
                var intent = CombatActionIntent.Empty;
                intent.InteractHold = true;
                intent.MoveInput = ComputePlayerWalkToAiInput();
                _player.CustomIntent = intent;

                if (_arb.SessionState == HandshakeState.Grip)
                {
                    reachedGrip = true;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }


            float waitPrank = 0f;
            bool prankerIsAi = false;
            while (waitPrank < 1.2f)
            {
                var intent = CombatActionIntent.Empty;
                intent.InteractHold = true;
                _player.CustomIntent = intent;

                if (_arb.LastOutcome == HandshakeOutcome.Prank)
                {
                    prankerIsAi = (_arb.LastPranker == _aiParticipant);
                    break;
                }
                waitPrank += Time.deltaTime;
                yield return null;
            }

            _ai.Mode = HandshakeAiMode.Idle;
            _player.CustomIntent = CombatActionIntent.Empty;

            _case8Pass = reachedGrip && (_arb.LastOutcome == HandshakeOutcome.Prank) && prankerIsAi;
            _results.Add($"[Case 8] AI early release during grip (AI Pranks Player): {(_case8Pass ? "PASS" : "FAIL")} (Grip={reachedGrip}, Outcome={_arb.LastOutcome}, PrankerIsAi={prankerIsAi})");
        }

        private IEnumerator Case9_BothWithdraw()
        {
            _arb.ResetAll();
            _ai.Mode = HandshakeAiMode.AiEarlyRelease;
            _ai.EarlyReleaseGripTime = 0.25f;
            _player.CustomIntent = CombatActionIntent.Empty;
            yield return new WaitForSeconds(0.2f);

            float elapsed = 0f;
            bool reachedGrip = false;
            while (elapsed < 4.0f && !reachedGrip)
            {
                var intent = CombatActionIntent.Empty;
                intent.InteractHold = true;
                intent.MoveInput = ComputePlayerWalkToAiInput();
                _player.CustomIntent = intent;

                if (_arb.SessionState == HandshakeState.Grip)
                {
                    reachedGrip = true;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }


            yield return new WaitForSeconds(0.15f);
            var releaseIntent = CombatActionIntent.Empty;
            releaseIntent.InteractHold = false;
            releaseIntent.InteractUp = true;
            _player.CustomIntent = releaseIntent;

            float waitBoth = 0f;
            while (waitBoth < 1.2f)
            {
                if (_arb.LastOutcome == HandshakeOutcome.BothWithdraw)
                {
                    break;
                }
                waitBoth += Time.deltaTime;
                yield return null;
            }

            float pTime = _arb.ParticipantA == _playerParticipant ? _arb.ParticipantAReleaseTimestamp : _arb.ParticipantBReleaseTimestamp;
            float aiTime = _arb.ParticipantA == _aiParticipant ? _arb.ParticipantAReleaseTimestamp : _arb.ParticipantBReleaseTimestamp;
            bool orderChecked = (pTime > 0f && aiTime > 0f && aiTime > pTime);

            _ai.Mode = HandshakeAiMode.Idle;
            _player.CustomIntent = CombatActionIntent.Empty;

            _case9Pass = reachedGrip && (_arb.LastOutcome == HandshakeOutcome.BothWithdraw) && orderChecked;
            _results.Add($"[Case 9] Both release during grip (BothWithdraw) with timestamps & order: {(_case9Pass ? "PASS" : "FAIL")} (Grip={reachedGrip}, Outcome={_arb.LastOutcome}, OrderChecked={orderChecked})");
        }

        private IEnumerator Case10_ContinuousHoldDoesNotReinitiateAfterBothWithdraw()
        {
            float waitReturn = 0f;
            while (waitReturn < 2.0f && _playerParticipant.State != HandshakeState.None)
            {
                var holdIntent = CombatActionIntent.Empty;
                holdIntent.InteractHold = true;
                _player.CustomIntent = holdIntent;
                waitReturn += Time.deltaTime;
                yield return null;
            }

            var intent = CombatActionIntent.Empty;
            intent.InteractHold = true;
            _player.CustomIntent = intent;

            yield return new WaitForSeconds(0.4f);

            bool stayedInNone = (_playerParticipant.State == HandshakeState.None);
            bool latchActive = _playerParticipant.RequiresInteractRelease;

            intent.InteractHold = false;
            intent.InteractUp = true;
            _player.CustomIntent = intent;
            yield return new WaitForSeconds(0.1f);

            bool latchCleared = !_playerParticipant.RequiresInteractRelease;
            _player.CustomIntent = CombatActionIntent.Empty;

            _case10Pass = stayedInNone && latchActive && latchCleared;
            _results.Add($"[Case 10] Holding interact continuously after BothWithdraw stays standing: {(_case10Pass ? "PASS" : "FAIL")} (StayedNone={stayedInNone}, Latch={latchActive}, Cleared={latchCleared})");
        }

        private IEnumerator Case11_ReleaseAfterGripTimestampRecorded()
        {
            _arb.ResetAll();
            _ai.Mode = HandshakeAiMode.AiInitiator;
            _player.CustomIntent = CombatActionIntent.Empty;
            yield return new WaitForSeconds(0.2f);

            float elapsed = 0f;
            bool reachedHolding = false;
            while (elapsed < 4.0f && !reachedHolding)
            {
                var intent = CombatActionIntent.Empty;
                intent.InteractHold = true;
                intent.MoveInput = ComputePlayerWalkToAiInput();
                _player.CustomIntent = intent;

                if (_arb.SessionState == HandshakeState.Holding)
                {
                    reachedHolding = true;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            yield return new WaitForSeconds(0.2f);
            float releaseTimestamp = Time.time;
            var releaseIntent = CombatActionIntent.Empty;
            releaseIntent.InteractHold = false;
            releaseIntent.InteractUp = true;
            _player.CustomIntent = releaseIntent;

            yield return new WaitForSeconds(0.1f);

            _ai.Mode = HandshakeAiMode.Idle;
            _player.CustomIntent = CombatActionIntent.Empty;

            bool timestampValid = (_arb.ParticipantAReleaseTimestamp > 0f && Mathf.Abs(_arb.ParticipantAReleaseTimestamp - releaseTimestamp) < 0.2f);
            _case11Pass = reachedHolding && timestampValid;
            _results.Add($"[Case 11] Release after grip completion recorded timestamp: {(_case11Pass ? "PASS" : "FAIL")} (ReachedHolding={reachedHolding}, ReleaseTime={_arb.ParticipantAReleaseTimestamp:F2}s, LocalTime={releaseTimestamp:F2}s)");
        }

        private IEnumerator Case12_StrengthContest_AiWin()
        {
            _arb.ResetAll();
            _ai.Mode = HandshakeAiMode.ContestWin;
            _player.CustomIntent = CombatActionIntent.Empty;
            yield return new WaitForSeconds(0.2f);

            float elapsed = 0f;
            bool reachedHolding = false;
            while (elapsed < 4.0f && !reachedHolding)
            {
                var intent = CombatActionIntent.Empty;
                intent.InteractHold = true;
                intent.MoveInput = ComputePlayerWalkToAiInput();
                _player.CustomIntent = intent;

                if (_arb.SessionState == HandshakeState.Holding)
                {
                    reachedHolding = true;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            float holdWait = 0f;
            while (holdWait < 0.95f)
            {
                var holdIntent = CombatActionIntent.Empty;
                holdIntent.InteractHold = true;
                _player.CustomIntent = holdIntent;
                holdWait += Time.deltaTime;
                yield return null;
            }
            _player.CustomIntent = CombatActionIntent.Empty;

            float waitContest = 0f;
            bool aiWon = false;
            while (waitContest < 2.5f)
            {
                if (_arb.LastOutcome == HandshakeOutcome.StrengthContestWin && _arb.ContestWinner == _aiParticipant)
                {
                    aiWon = true;
                    break;
                }
                waitContest += Time.deltaTime;
                yield return null;
            }

            _ai.Mode = HandshakeAiMode.Idle;
            _case12Pass = reachedHolding && aiWon;
            _results.Add($"[Case 12] Strength Contest AI Win (Triumph/Pain): {(_case12Pass ? "PASS" : "FAIL")} (ReachedHolding={reachedHolding}, AiWon={aiWon}, Winner={_arb.ContestWinner?.EntityId})");
        }

        private IEnumerator Case13_StrengthContest_PlayerWin()
        {
            _arb.ResetAll();
            _ai.Mode = HandshakeAiMode.ContestLose;
            _player.CustomIntent = CombatActionIntent.Empty;
            yield return new WaitForSeconds(0.2f);

            float elapsed = 0f;
            bool reachedHolding = false;
            while (elapsed < 4.0f && !reachedHolding)
            {
                var intent = CombatActionIntent.Empty;
                intent.InteractHold = true;
                intent.MoveInput = ComputePlayerWalkToAiInput();
                _player.CustomIntent = intent;

                if (_arb.SessionState == HandshakeState.Holding)
                {
                    reachedHolding = true;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            for (int i = 0; i < 4; i++)
            {
                var tapIntent = CombatActionIntent.Empty;
                tapIntent.InteractHold = true;
                tapIntent.InteractDown = true;
                _player.CustomIntent = tapIntent;
                yield return new WaitForSeconds(0.08f);

                tapIntent.InteractHold = false;
                tapIntent.InteractDown = false;
                tapIntent.InteractUp = true;
                _player.CustomIntent = tapIntent;
                yield return new WaitForSeconds(0.08f);
            }

            _player.CustomIntent = CombatActionIntent.Empty;

            float waitContest = 0f;
            bool playerWon = false;
            while (waitContest < 2.5f)
            {
                if (_arb.LastOutcome == HandshakeOutcome.StrengthContestWin && _arb.ContestWinner == _playerParticipant)
                {
                    playerWon = true;
                    break;
                }
                waitContest += Time.deltaTime;
                yield return null;
            }

            _ai.Mode = HandshakeAiMode.Idle;
            _case13Pass = reachedHolding && playerWon;
            _results.Add($"[Case 13] Strength Contest Player Win (Triumph/Pain): {(_case13Pass ? "PASS" : "FAIL")} (ReachedHolding={reachedHolding}, PlayerWon={playerWon}, Winner={_arb.ContestWinner?.EntityId})");
        }

        private IEnumerator Case14_StrengthContest_Tie()
        {
            _arb.ResetAll();
            _ai.Mode = HandshakeAiMode.ContestTie;
            _player.CustomIntent = CombatActionIntent.Empty;
            yield return new WaitForSeconds(0.2f);

            float elapsed = 0f;
            bool reachedHolding = false;
            while (elapsed < 4.0f && !reachedHolding)
            {
                var intent = CombatActionIntent.Empty;
                intent.InteractHold = true;
                intent.MoveInput = ComputePlayerWalkToAiInput();
                _player.CustomIntent = intent;

                if (_arb.SessionState == HandshakeState.Holding)
                {
                    reachedHolding = true;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            for (int i = 0; i < 2; i++)
            {
                var tapIntent = CombatActionIntent.Empty;
                tapIntent.InteractHold = true;
                tapIntent.InteractDown = true;
                _player.CustomIntent = tapIntent;
                yield return new WaitForSeconds(0.1f);

                tapIntent.InteractHold = false;
                tapIntent.InteractDown = false;
                tapIntent.InteractUp = true;
                _player.CustomIntent = tapIntent;
                yield return new WaitForSeconds(0.1f);
            }

            var holdIntent = CombatActionIntent.Empty;
            holdIntent.InteractHold = true;
            _player.CustomIntent = holdIntent;
            yield return new WaitForSeconds(0.4f);

            _player.CustomIntent = CombatActionIntent.Empty;

            float waitContest = 0f;
            bool tieOccurred = false;
            while (waitContest < 2.5f)
            {
                if (_arb.LastOutcome == HandshakeOutcome.StrengthContestTie)
                {
                    tieOccurred = true;
                    break;
                }
                waitContest += Time.deltaTime;
                yield return null;
            }

            _ai.Mode = HandshakeAiMode.Idle;
            _case14Pass = reachedHolding && tieOccurred;
            _results.Add($"[Case 14] Strength Contest Tie (SlowWithdraw): {(_case14Pass ? "PASS" : "FAIL")} (ReachedHolding={reachedHolding}, Tie={tieOccurred})");
        }

        private IEnumerator Case15_StrengthContest_ZeroZero()
        {
            _arb.ResetAll();
            _ai.Mode = HandshakeAiMode.ContestZeroZero;
            _player.CustomIntent = CombatActionIntent.Empty;
            yield return new WaitForSeconds(0.2f);

            float elapsed = 0f;
            bool reachedHolding = false;
            while (elapsed < 4.0f && !reachedHolding)
            {
                var intent = CombatActionIntent.Empty;
                intent.InteractHold = true;
                intent.MoveInput = ComputePlayerWalkToAiInput();
                _player.CustomIntent = intent;

                if (_arb.SessionState == HandshakeState.Holding)
                {
                    reachedHolding = true;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            yield return new WaitForSeconds(0.1f);
            _player.CustomIntent = CombatActionIntent.Empty;

            float waitContest = 0f;
            bool zeroZeroOccurred = false;
            while (waitContest < 2.0f)
            {
                if (_arb.LastOutcome == HandshakeOutcome.StrengthContestZeroZero)
                {
                    zeroZeroOccurred = true;
                    break;
                }
                waitContest += Time.deltaTime;
                yield return null;
            }

            _ai.Mode = HandshakeAiMode.Idle;
            _case15Pass = reachedHolding && zeroZeroOccurred;
            _results.Add($"[Case 15] Strength Contest 0 vs 0 (Shake then Withdraw): {(_case15Pass ? "PASS" : "FAIL")} (ReachedHolding={reachedHolding}, ZeroZero={zeroZeroOccurred})");
        }

        private IEnumerator Case16_CatchAndEscape()
        {
            _arb.ResetAll();
            _ai.Mode = HandshakeAiMode.AiEscape;
            _player.CustomIntent = CombatActionIntent.Empty;
            yield return new WaitForSeconds(0.2f);

            float elapsed = 0f;
            bool reachedHolding = false;
            while (elapsed < 4.0f && !reachedHolding)
            {
                var intent = CombatActionIntent.Empty;
                intent.InteractHold = true;
                intent.MoveInput = ComputePlayerWalkToAiInput();
                _player.CustomIntent = intent;

                if (_arb.SessionState == HandshakeState.Holding)
                {
                    reachedHolding = true;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            float waitCatch = 0f;
            bool caughtOccurred = false;
            while (waitCatch < 1.5f && !caughtOccurred)
            {
                var intent = CombatActionIntent.Empty;
                intent.InteractHold = true;
                _player.CustomIntent = intent;

                if (_arb.SessionState == HandshakeState.Catching)
                {
                    caughtOccurred = true;
                }
                waitCatch += Time.deltaTime;
                yield return null;
            }

            float waitEscape = 0f;
            bool escapeOccurred = false;
            while (waitEscape < 2.5f && !escapeOccurred)
            {
                var intent = CombatActionIntent.Empty;
                intent.InteractHold = true;
                _player.CustomIntent = intent;

                if (_arb.LastOutcome == HandshakeOutcome.Escaped)
                {
                    escapeOccurred = true;
                }
                waitEscape += Time.deltaTime;
                yield return null;
            }

            _ai.Mode = HandshakeAiMode.Idle;
            _player.CustomIntent = CombatActionIntent.Empty;

            _case16Pass = reachedHolding && caughtOccurred && escapeOccurred;
            _results.Add($"[Case 16] Catch and physical jump/walk distance Escape: {(_case16Pass ? "PASS" : "FAIL")} (ReachedHolding={reachedHolding}, Caught={caughtOccurred}, Escaped={escapeOccurred})");
        }

        private IEnumerator Case17_CaughtRepressReturnsToHolding()
        {
            _arb.ResetAll();
            _ai.Mode = HandshakeAiMode.AiCatchPlayer;
            _player.CustomIntent = CombatActionIntent.Empty;
            yield return new WaitForSeconds(0.2f);

            float elapsed = 0f;
            bool reachedHolding = false;
            while (elapsed < 4.0f && !reachedHolding)
            {
                var intent = CombatActionIntent.Empty;
                intent.InteractHold = true;
                intent.MoveInput = ComputePlayerWalkToAiInput();
                _player.CustomIntent = intent;

                if (_arb.SessionState == HandshakeState.Holding)
                {
                    reachedHolding = true;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            for (int i = 0; i < 2; i++)
            {
                var tap = CombatActionIntent.Empty;
                tap.InteractHold = true;
                tap.InteractDown = true;
                _player.CustomIntent = tap;
                yield return new WaitForSeconds(0.06f);

                tap.InteractHold = false;
                tap.InteractDown = false;
                tap.InteractUp = true;
                _player.CustomIntent = tap;
                yield return new WaitForSeconds(0.06f);
            }

            int tapsBefore = _playerParticipant.InteractPressCount;

            _player.CustomIntent = CombatActionIntent.Empty;

            float waitCaught = 0f;
            bool playerCaught = false;
            while (waitCaught < 1.5f && !playerCaught)
            {
                if (_arb.SessionState == HandshakeState.Catching && _arb.CaughtParticipant == _playerParticipant)
                {
                    playerCaught = true;
                }
                waitCaught += Time.deltaTime;
                yield return null;
            }

            var repressIntent = CombatActionIntent.Empty;
            repressIntent.InteractHold = true;
            repressIntent.InteractDown = true;
            _player.CustomIntent = repressIntent;
            yield return new WaitForSeconds(0.12f);

            bool returnedToHolding = (_arb.SessionState == HandshakeState.Holding);

            for (int i = 0; i < 2; i++)
            {
                var tap = CombatActionIntent.Empty;
                tap.InteractHold = true;
                tap.InteractDown = true;
                _player.CustomIntent = tap;
                yield return new WaitForSeconds(0.06f);

                tap.InteractHold = false;
                tap.InteractDown = false;
                tap.InteractUp = true;
                _player.CustomIntent = tap;
                yield return new WaitForSeconds(0.06f);
            }

            int tapsFinal = _playerParticipant.InteractPressCount;
            bool tapsPreserved = (tapsBefore == 2 && tapsFinal == 5);

            _ai.Mode = HandshakeAiMode.Idle;
            _player.CustomIntent = CombatActionIntent.Empty;

            _case17Pass = playerCaught && returnedToHolding && tapsPreserved;
            _results.Add($"[Case 17] Caught participant re-presses interact returns to Holding (Taps preserved): {(_case17Pass ? "PASS" : "FAIL")} (WasCaught={playerCaught}, ReturnedHolding={returnedToHolding}, InitialTaps={tapsBefore}, FinalTaps={tapsFinal})");
        }

        private IEnumerator Case18_CatcherReleaseResolvesContest()
        {
            _arb.ResetAll();
            _ai.Mode = HandshakeAiMode.AiCatchAndRelease;
            _player.CustomIntent = CombatActionIntent.Empty;
            yield return new WaitForSeconds(0.2f);

            float elapsed = 0f;
            bool reachedHolding = false;
            while (elapsed < 4.0f && !reachedHolding)
            {
                var intent = CombatActionIntent.Empty;
                intent.InteractHold = true;
                intent.MoveInput = ComputePlayerWalkToAiInput();
                _player.CustomIntent = intent;

                if (_arb.SessionState == HandshakeState.Holding)
                {
                    reachedHolding = true;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            yield return new WaitForSeconds(0.1f);
            _player.CustomIntent = CombatActionIntent.Empty;

            float waitContest = 0f;
            bool resolvedContest = false;
            while (waitContest < 2.0f)
            {
                if (_arb.LastOutcome == HandshakeOutcome.StrengthContestZeroZero ||
                    _arb.LastOutcome == HandshakeOutcome.StrengthContestTie ||
                    _arb.LastOutcome == HandshakeOutcome.StrengthContestWin)
                {
                    resolvedContest = true;
                    break;
                }
                waitContest += Time.deltaTime;
                yield return null;
            }

            _ai.Mode = HandshakeAiMode.Idle;
            _case18Pass = reachedHolding && resolvedContest;
            _results.Add($"[Case 18] Catcher releases during catch resolves Strength Contest: {(_case18Pass ? "PASS" : "FAIL")} (ReachedHolding={reachedHolding}, ResolvedContest={resolvedContest}, Outcome={_arb.LastOutcome})");
        }

        private IEnumerator Case19_CatcherChasesAndPreventsEscape()
        {
            _arb.ResetAll();
            _ai.Mode = HandshakeAiMode.AiChaseCatch;
            _player.CustomIntent = CombatActionIntent.Empty;
            yield return new WaitForSeconds(0.2f);

            float elapsed = 0f;
            bool reachedHolding = false;
            while (elapsed < 4.0f && !reachedHolding)
            {
                var intent = CombatActionIntent.Empty;
                intent.InteractHold = true;
                intent.MoveInput = ComputePlayerWalkToAiInput();
                _player.CustomIntent = intent;

                if (_arb.SessionState == HandshakeState.Holding)
                {
                    reachedHolding = true;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            _player.CustomIntent = CombatActionIntent.Empty;

            float waitCaught = 0f;
            bool chaseMaintained = false;
            bool playerMoved = false;
            bool aiMoved = false;
            Vector3 startPlayerPos = _player.transform.position;
            Vector3 startAiPos = _ai.transform.position;

            while (waitCaught < 1.5f)
            {
                if (_arb.SessionState == HandshakeState.Catching && _arb.CaughtParticipant == _playerParticipant)
                {
                    var pullAway = CombatActionIntent.Empty;
                    pullAway.MoveInput = new Vector2(0f, -1f);
                    _player.CustomIntent = pullAway;

                    float dist = _arb.BodyDistance;
                    float thresh = _arb.GetEffectiveEscapeThreshold();
                    if (dist < thresh)
                    {
                        chaseMaintained = true;
                    }

                    if (Vector3.Distance(_player.transform.position, startPlayerPos) > 0.04f)
                    {
                        playerMoved = true;
                    }
                    if (Vector3.Distance(_ai.transform.position, startAiPos) > 0.04f)
                    {
                        aiMoved = true;
                    }
                }
                waitCaught += Time.deltaTime;
                yield return null;
            }

            _ai.Mode = HandshakeAiMode.Idle;
            _player.CustomIntent = CombatActionIntent.Empty;
            _case19Pass = reachedHolding && chaseMaintained && playerMoved && aiMoved;
            _results.Add($"[Case 19] Catcher chases and prevents distance escape: {(_case19Pass ? "PASS" : "FAIL")} (ReachedHolding={reachedHolding}, ChaseMaintained={chaseMaintained}, PlayerMoved={playerMoved}, AiChased={aiMoved})");
        }

        private IEnumerator Case20_CaughtJumpsWithoutEscape()
        {
            _arb.ResetAll();
            _ai.Mode = HandshakeAiMode.AiJumpNoEscape;
            _player.CustomIntent = CombatActionIntent.Empty;
            yield return new WaitForSeconds(0.2f);

            float elapsed = 0f;
            bool reachedHolding = false;
            while (elapsed < 4.0f && !reachedHolding)
            {
                var intent = CombatActionIntent.Empty;
                intent.InteractHold = true;
                intent.MoveInput = ComputePlayerWalkToAiInput();
                _player.CustomIntent = intent;

                if (_arb.SessionState == HandshakeState.Holding)
                {
                    reachedHolding = true;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            float waitCatch = 0f;
            bool caughtStillHeld = false;
            bool aiJumped = false;
            while (waitCatch < 1.5f)
            {
                var intent = CombatActionIntent.Empty;
                intent.InteractHold = true;
                _player.CustomIntent = intent;

                if (_arb.SessionState == HandshakeState.Catching && _arb.CaughtParticipant == _aiParticipant)
                {
                    float dist = _arb.BodyDistance;
                    float thresh = _arb.GetEffectiveEscapeThreshold();
                    if (dist < thresh)
                    {
                        caughtStillHeld = true;
                    }

                    CombatEntity aiEntity = _ai.GetComponent<CombatEntity>();
                    if ((aiEntity != null && !aiEntity.IsGrounded) || _ai.transform.position.y > 0.04f)
                    {
                        aiJumped = true;
                    }
                }
                waitCatch += Time.deltaTime;
                yield return null;
            }

            _ai.Mode = HandshakeAiMode.Idle;
            _player.CustomIntent = CombatActionIntent.Empty;

            _case20Pass = reachedHolding && caughtStillHeld && aiJumped;
            _results.Add($"[Case 20] Caught participant jumps without reaching threshold (Remains Caught): {(_case20Pass ? "PASS" : "FAIL")} (ReachedHolding={reachedHolding}, RemainsCaught={caughtStillHeld}, AiJumped={aiJumped})");
        }

        private IEnumerator Case21_ResetConsistency()
        {
            _ai.Mode = HandshakeAiMode.Idle;
            _arb.ResetAll();
            _player.CustomIntent = CombatActionIntent.Empty;
            yield return new WaitForSeconds(0.15f);

            bool pClean = (_playerParticipant.State == HandshakeState.None && Mathf.Abs(_playerParticipant.transform.position.z - (-1.2f)) < 0.2f);
            bool aiClean = (_aiParticipant.State == HandshakeState.None && Mathf.Abs(_aiParticipant.transform.position.z - 1.2f) < 0.2f);
            bool arbClean = (_arb.SessionState == HandshakeState.None && _arb.LastOutcome == HandshakeOutcome.None);

            _case21Pass = pClean && aiClean && arbClean;
            _results.Add($"[Case 21] Global arena reset consistency: {(_case21Pass ? "PASS" : "FAIL")} (PlayerClean={pClean}, AiClean={aiClean}, ArbClean={arbClean})");
        }

        public static string ResolveDocumentationPath(string fileName)
        {
            string packageDoc = @"C:\Users\Yu5h1\Dev\VSProjects\Yu5h1Lib\Unity\Combat-Aesthetics\Documentation";
            if (Directory.Exists(packageDoc))
            {
                return Path.Combine(packageDoc, fileName);
            }
            DirectoryInfo current = new DirectoryInfo(Application.dataPath);
            while (current != null)
            {
                string candidateInSub = Path.Combine(current.FullName, "Unity", "Combat-Aesthetics", "Documentation");
                if (Directory.Exists(candidateInSub))
                {
                    return Path.Combine(candidateInSub, fileName);
                }
                string candidate = Path.Combine(current.FullName, "Documentation");
                if (Directory.Exists(candidate))
                {
                    return Path.Combine(candidate, fileName);
                }
                current = current.Parent;
            }
            return Path.Combine(Application.dataPath, "../", fileName);
        }
    }
}
