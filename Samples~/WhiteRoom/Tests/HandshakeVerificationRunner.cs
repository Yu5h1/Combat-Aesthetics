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
        private bool _case8Pass;
        private bool _case9Pass;
        private bool _case10Pass;

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
            _results.Add("  WhiteRoom Stage 2b Handshake Batch 2 Supplemental Automated Check");
            _results.Add("======================================================================");

            yield return StartCoroutine(Case1_PlayerCancel());
            yield return StartCoroutine(Case2_AiInitiate_PlayerRespond_Success());
            yield return StartCoroutine(Case3_ContinuousHoldDoesNotReinitiate());
            yield return StartCoroutine(Case4_PlayerInitiate_AiRespond_Success());
            yield return StartCoroutine(Case5_PlayerPranksAi());
            yield return StartCoroutine(Case6_AiPranksPlayer());
            yield return StartCoroutine(Case7_BothWithdraw());
            yield return StartCoroutine(Case8_PrankWhenOtherReleasesAfterGrip());
            yield return StartCoroutine(Case9_AiCancel());
            yield return StartCoroutine(Case10_ResetConsistency());

            AllPassed = _case1Pass && _case2Pass && _case3Pass && _case4Pass && _case5Pass &&
                        _case6Pass && _case7Pass && _case8Pass && _case9Pass && _case10Pass;
            _results.Add("======================================================================");
            _results.Add($"  Result: {(AllPassed ? "ALL PASSED (10/10)" : "FAILED")}");
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
            if (_case2Pass)
            {
                CaptureCameraToFile("C:/Users/Yu5h1/Dev/VSProjects/Yu5h1Lib/Unity/Combat-Aesthetics/Documentation/Screenshots/Handshake_PlayMode_Success.png");
            }
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

            Vector3 pHand = _playerParticipant.ContactBridge != null ? _playerParticipant.ContactBridge.transform.position : Vector3.zero;
            Vector3 aHand = _aiParticipant.ContactBridge != null ? _aiParticipant.ContactBridge.transform.position : Vector3.zero;
            float handDist = Vector3.Distance(pHand, aHand);
            float bodyDist = Vector3.Distance(_player.transform.position, _ai.transform.position);
            Debug.Log($"[CASE2_DEBUG] At end of walk: bodyDist={bodyDist:F3}, handDist={handDist:F3}, PlayerHand={pHand}, AiHand={aHand}");

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

        private IEnumerator Case5_PlayerPranksAi()
        {
            _arb.ResetAll();
            _ai.Mode = HandshakeAiMode.AiResponder;
            _ai.HoldDuration = -1f;
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

            yield return new WaitForSeconds(0.15f);

            var releaseIntent = CombatActionIntent.Empty;
            releaseIntent.InteractHold = false;
            releaseIntent.InteractUp = true;
            _player.CustomIntent = releaseIntent;
            yield return new WaitForSeconds(0.05f);

            bool playerWithdrawingDuringGrip = (_playerParticipant.State == HandshakeState.Withdrawing);
            bool aiStillInGrip = (_aiParticipant.State == HandshakeState.Grip);
            bool sessionStillInGrip = (_arb.SessionState == HandshakeState.Grip);

            yield return new WaitForSeconds(0.40f);

            bool outcomePrank = (_arb.LastOutcome == HandshakeOutcome.Prank);
            bool prankerIsPlayer = (_arb.LastPranker == _playerParticipant);
            bool prankedIsAi = (_arb.LastPranked == _aiParticipant);

            _case5Pass = reachedGrip && playerWithdrawingDuringGrip && aiStillInGrip && sessionStillInGrip && outcomePrank && prankerIsPlayer && prankedIsAi;
            if (_case5Pass)
            {
                CaptureCameraToFile("C:/Users/Yu5h1/Dev/VSProjects/Yu5h1Lib/Unity/Combat-Aesthetics/Documentation/Screenshots/Handshake_PlayMode_Prank.png");
            }
            _results.Add($"[Case 5] Player release alone during grip (Player Pranks AI): {(_case5Pass ? "PASS" : "FAIL")} (PlayerWithdrewDuringGrip={playerWithdrawingDuringGrip}, AiHeldInGrip={aiStillInGrip}, Outcome={_arb.LastOutcome}, Pranker={_arb.LastPranker?.EntityId})");
        }

        private IEnumerator Case6_AiPranksPlayer()
        {
            _arb.ResetAll();
            _ai.Mode = HandshakeAiMode.AiEarlyRelease;
            _ai.EarlyReleaseGripTime = 0.18f;
            _ai.ReachDelay = 0.15f;
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

            yield return new WaitForSeconds(0.25f);

            bool aiWithdrawingDuringGrip = (_aiParticipant.State == HandshakeState.Withdrawing);
            bool playerStillInGrip = (_playerParticipant.State == HandshakeState.Grip);

            yield return new WaitForSeconds(0.35f);

            bool outcomePrank = (_arb.LastOutcome == HandshakeOutcome.Prank);
            bool prankerIsAi = (_arb.LastPranker == _aiParticipant);
            bool prankedIsPlayer = (_arb.LastPranked == _playerParticipant);

            _case6Pass = reachedGrip && aiWithdrawingDuringGrip && playerStillInGrip && outcomePrank && prankerIsAi && prankedIsPlayer;
            _results.Add($"[Case 6] AI release alone during grip (AI Pranks Player): {(_case6Pass ? "PASS" : "FAIL")} (AiWithdrewDuringGrip={aiWithdrawingDuringGrip}, PlayerHeldInGrip={playerStillInGrip}, Outcome={_arb.LastOutcome}, Pranker={_arb.LastPranker?.EntityId})");
        }

        private IEnumerator Case7_BothWithdraw()
        {
            _arb.ResetAll();
            _ai.Mode = HandshakeAiMode.AiEarlyRelease;
            _ai.EarlyReleaseGripTime = 0.28f;
            _ai.ReachDelay = 0.15f;
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

            yield return new WaitForSeconds(0.12f);
            var releaseIntent = CombatActionIntent.Empty;
            releaseIntent.InteractHold = false;
            releaseIntent.InteractUp = true;
            _player.CustomIntent = releaseIntent;

            yield return new WaitForSeconds(0.45f);

            bool outcomeBoth = (_arb.LastOutcome == HandshakeOutcome.BothWithdraw);
            _case7Pass = reachedGrip && outcomeBoth;
            if (_case7Pass)
            {
                CaptureCameraToFile("C:/Users/Yu5h1/Dev/VSProjects/Yu5h1Lib/Unity/Combat-Aesthetics/Documentation/Screenshots/Handshake_PlayMode_BothWithdraw.png");
            }
            _results.Add($"[Case 7] Both release before grip ends (BothWithdraw): {(_case7Pass ? "PASS" : "FAIL")} (Outcome={_arb.LastOutcome})");
        }

        private IEnumerator Case8_PrankWhenOtherReleasesAfterGrip()
        {
            _arb.ResetAll();
            _ai.Mode = HandshakeAiMode.AiReleaseAfterGrip;
            _ai.ReleaseAfterGripDelay = 0.15f;
            _ai.ReachDelay = 0.15f;
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

            yield return new WaitForSeconds(0.15f);
            var releaseIntent = CombatActionIntent.Empty;
            releaseIntent.InteractHold = false;
            releaseIntent.InteractUp = true;
            _player.CustomIntent = releaseIntent;

            yield return new WaitForSeconds(0.45f);

            bool outcomePrank = (_arb.LastOutcome == HandshakeOutcome.Prank);
            bool prankerIsPlayer = (_arb.LastPranker == _playerParticipant);

            _case8Pass = reachedGrip && outcomePrank && prankerIsPlayer;
            _results.Add($"[Case 8] One release during grip, other releases after grip (Still Prank): {(_case8Pass ? "PASS" : "FAIL")} (Outcome={_arb.LastOutcome}, Pranker={_arb.LastPranker?.EntityId})");
        }

        private IEnumerator Case9_AiCancel()
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

            _case9Pass = aiReached && aiWithdrawingOrNone;
            _results.Add($"[Case 9] Configurable AI initiate then release (AI Cancel): {(_case9Pass ? "PASS" : "FAIL")} (AiReached={aiReached}, AiWithdrew={aiWithdrawingOrNone})");
        }

        private IEnumerator Case10_ResetConsistency()
        {
            _arb.ResetAll();
            _ai.Mode = HandshakeAiMode.AiInitiator;
            _player.CustomIntent = CombatActionIntent.Empty;
            yield return new WaitForSeconds(0.15f);

            bool pClean = (_playerParticipant.State == HandshakeState.None && Mathf.Abs(_playerParticipant.transform.position.z - (-1.2f)) < 0.1f);
            bool aiClean = (_aiParticipant.State == HandshakeState.None && Mathf.Abs(_aiParticipant.transform.position.z - 1.2f) < 0.1f);
            bool arbClean = (_arb.SessionState == HandshakeState.None && _arb.LastOutcome == HandshakeOutcome.None);

            _case10Pass = pClean && aiClean && arbClean;
            _results.Add($"[Case 10] Global arena reset consistency: {(_case10Pass ? "PASS" : "FAIL")} (PlayerClean={pClean}, AiClean={aiClean}, ArbClean={arbClean})");
        }

        /// <summary>
        /// Captures a screenshot of the main camera to the specified disk path using RenderTexture rendering.
        /// </summary>
        private void CaptureCameraToFile(string filePath)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            int w = 1920;
            int h = 1080;
            RenderTexture rt = RenderTexture.GetTemporary(w, h, 24);
            RenderTexture prev = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = prev;

            RenderTexture prevActive = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = prevActive;
            RenderTexture.ReleaseTemporary(rt);

            byte[] bytes = tex.EncodeToPNG();
            DestroyImmediate(tex);
            string dir = System.IO.Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }
            System.IO.File.WriteAllBytes(filePath, bytes);
        }
    }
}
