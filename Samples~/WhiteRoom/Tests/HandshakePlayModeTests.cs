using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Yu5h1Lib;
using Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom;
using Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom.Handshake;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom.Tests
{
    /// <summary>
    /// PlayMode automated test fixture for the WhiteRoom Stage 2b handshake interaction and combat baseline.
    /// Runs all verification scenarios through <see cref="HandshakeVerificationRunner"/>, verifies combat jump/landing/attack
    /// constraints, edge falling, pre-IK collision proxy positioning, and Items 1–4 from third-round fix tasks.
    /// </summary>
    public class HandshakePlayModeTests
    {
        private const string HandshakeScenePath = "Assets/Samples/Combat Aesthetic/1.0.0/WhiteRoom/Handshake/Handshake.unity";
        private const string CombatScenePath = "Assets/Samples/Combat Aesthetic/1.0.0/WhiteRoom/Combat/WhiteRoom.unity";

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator RunHandshakeBatch3VerificationSuite()
        {
            var loadOp = EditorSceneManager.LoadSceneAsyncInPlayMode(
                HandshakeScenePath,
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            while (!loadOp.isDone)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.5f);

            var runnerGo = new GameObject("HandshakeVerificationRunnerHost");
            var runner = runnerGo.AddComponent<HandshakeVerificationRunner>();

            while (!runner.IsFinished)
            {
                yield return null;
            }

            var hud = ObjectUtility.FindObject<WhiteRoomHUD>(false);
            if (hud != null)
            {
                string exportPath = HandshakeVerificationRunner.ResolveDocumentationPath("WhiteRoom_Handshake_Logs.json");
                hud.ExportStructuredLogs(exportPath);
            }

            Debug.Log($"[HandshakePlayModeTests Report]\n{runner.TestReport}");
            Assert.IsTrue(runner.AllPassed, runner.TestReport);
        }

        [UnityTest]
        public IEnumerator VerifyCombatCameraLookAtTracking()
        {
            var loadOp = EditorSceneManager.LoadSceneAsyncInPlayMode(
                CombatScenePath,
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            while (!loadOp.isDone)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.3f);

            var cam = ObjectUtility.FindObject<WhiteRoomCamera>(false);
            Assert.IsNotNull(cam, "WhiteRoomCamera must exist in combat scene.");
            Assert.IsNotNull(cam.Target, "Combat camera target must be bound to the player.");

            Transform playerTransform = cam.Target;
            playerTransform.position += new Vector3(3f, 0f, 2f);
            yield return null;
            yield return null;

            Vector3 toPlayer = playerTransform.position - cam.transform.position;
            float lookDot = Vector3.Dot(cam.transform.forward, toPlayer.normalized);
            Assert.Greater(lookDot, 0.7f, "Combat camera must continue facing towards the player after movement.");

            Camera unityCam = Camera.main;
            if (unityCam != null)
            {
                Vector3 vp = unityCam.WorldToViewportPoint(playerTransform.position);
                Assert.IsTrue(vp.z > 0f && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f,
                    "Player must remain within camera viewport frustum after movement.");
            }

            yield return new WaitForSeconds(1f);
            float initialCamDist = Vector3.Distance(cam.transform.position, playerTransform.position);
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = "TestOcclusionObstacle";
            obstacle.transform.position = Vector3.Lerp(playerTransform.position + new Vector3(0f, 1.4f, 0f), cam.transform.position, 0.5f);
            obstacle.transform.localScale = new Vector3(2f, 2f, 0.5f);

            yield return new WaitForSeconds(0.5f);

            float occludedDist = Vector3.Distance(cam.transform.position, playerTransform.position);
            Assert.AreEqual(initialCamDist, occludedDist, 0.05f, "Camera must keep its orbit distance when an obstacle blocks line of sight to player.");
            Object.Destroy(obstacle);
        }

        [UnityTest]
        public IEnumerator VerifyCombatSceneJumpLandingAndAttackPermissions()
        {
            var loadOp = EditorSceneManager.LoadSceneAsyncInPlayMode(
                CombatScenePath,
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            while (!loadOp.isDone)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.3f);

            var character = ObjectUtility.FindObject<WhiteRoomCharacter>(false);
            Assert.IsNotNull(character, "Player character must exist in combat scene.");
            var entity = character.GetComponent<CombatEntity>();
            Assert.IsNotNull(entity, "CombatEntity must exist on player.");

            Assert.IsTrue(entity.IsGrounded, "Character must be grounded at combat spawn.");

            character.OverrideIntent = true;
            var jumpIntent = CombatActionIntent.Empty;
            jumpIntent.Jump = true;
            character.CustomIntent = jumpIntent;
            yield return new WaitForSeconds(0.12f);

            character.CustomIntent = CombatActionIntent.Empty;
            yield return new WaitForSeconds(0.12f);

            Assert.IsFalse(entity.IsGrounded, "Character should be airborne during jump.");

            var airborneAttackIntent = CombatActionIntent.Empty;
            airborneAttackIntent.Attack = true;
            character.CustomIntent = airborneAttackIntent;
            yield return new WaitForSeconds(0.08f);

            bool isAttackingWhileAirborne = (entity.State == CombatState.AttackStartup || entity.State == CombatState.AttackActive);
            Assert.IsFalse(isAttackingWhileAirborne, "Character cannot attack while airborne.");

            character.CustomIntent = CombatActionIntent.Empty;

            float landWait = 0f;
            while (!entity.IsGrounded && landWait < 2.5f)
            {
                landWait += Time.deltaTime;
                yield return null;
            }
            Assert.IsTrue(entity.IsGrounded, "Character must land back onto the ground.");

            var landingAttackIntent = CombatActionIntent.Empty;
            landingAttackIntent.Attack = true;
            character.CustomIntent = landingAttackIntent;
            yield return new WaitForSeconds(0.1f);

            bool isAttackingOnGround = (entity.State == CombatState.AttackStartup || entity.State == CombatState.AttackActive);
            Assert.IsTrue(isAttackingOnGround, "Character can attack after landing on ground.");

            character.CustomIntent = CombatActionIntent.Empty;
            yield return new WaitForSeconds(0.6f);

            var charController = character.GetComponent<CharacterController>();
            if (charController != null) charController.enabled = false;
            character.transform.position = new Vector3(24.5f, 0.05f, 0f);
            Physics.SyncTransforms();
            if (charController != null) charController.enabled = true;
            yield return null;

            var walkOffIntent = CombatActionIntent.Empty;
            walkOffIntent.MoveInput = new Vector2(1f, 0f);
            character.CustomIntent = walkOffIntent;

            float walkTime = 0f;
            while (walkTime < 2.0f && entity.IsGrounded)
            {
                walkTime += Time.deltaTime;
                yield return null;
            }

            Assert.IsFalse(entity.IsGrounded, "Character must become airborne/falling after walking off the arena platform edge.");

            character.CustomIntent = CombatActionIntent.Empty;
            character.OverrideIntent = false;

            var hud = ObjectUtility.FindObject<WhiteRoomHUD>(false);
            Assert.IsNotNull(hud, "WhiteRoomHUD must exist in combat scene.");
            Assert.Greater(hud.AllHistoryLogs.Count, 0, "Event logs must be recorded during combat actions.");
        }

        [UnityTest]
        public IEnumerator VerifyPreIkContactSpherePosition()
        {
            var loadOp = EditorSceneManager.LoadSceneAsyncInPlayMode(
                HandshakeScenePath,
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            while (!loadOp.isDone)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.4f);

            var participants = ObjectUtility.FindObjects<HandshakeParticipant>(false);
            Assert.AreEqual(2, participants.Length, "Handshake scene must spawn 2 participants.");

            HandshakeParticipant player = null;
            foreach (var p in participants)
            {
                if (p.TeamId == 0) player = p;
            }
            Assert.IsNotNull(player, "Player participant must exist.");

            Assert.IsNotNull(player.ContactBridge, "HandContactBridge must be attached to player hand proxy.");
            Transform proxyTransform = player.ContactBridge.transform;
            Assert.IsNotNull(proxyTransform, "Contact proxy transform must exist.");

            Animator anim = player.GetComponentInChildren<Animator>();
            Assert.IsNotNull(anim, "Player Animator must exist.");
            Transform wristBone = anim.GetBoneTransform(HumanBodyBones.RightHand);
            Assert.IsNotNull(wristBone, "HumanBodyBones.RightHand must exist.");

            yield return null;
            yield return null;

            float distToWrist = Vector3.Distance(proxyTransform.position, wristBone.position);
            Assert.Less(distToWrist, 0.35f, "Contact proxy sphere must be anchored to the animated wrist bone position.");

            // Verify pre-IK immunity with IK target near hand (not 10m away, natural 0.20m offset)
            HandshakeVisualFeedback feedback = player.GetComponentInChildren<HandshakeVisualFeedback>();
            Assert.IsNotNull(feedback, "Feedback component must exist.");
            Vector3 initialProxyPos = proxyTransform.position;
            Vector3 nearbyTarget = wristBone.position + new Vector3(0.20f, 0.10f, 0.15f);
            feedback.SetIkOverride(nearbyTarget, 1.0f);

            yield return null;
            yield return null;

            float distToInitialProxy = Vector3.Distance(proxyTransform.position, initialProxyPos);
            float distToNearbyTarget = Vector3.Distance(proxyTransform.position, nearbyTarget);
            Assert.Less(distToInitialProxy, 0.05f, "Contact proxy sphere must remain at pre-IK wrist position.");
            Assert.Greater(distToNearbyTarget, 0.10f, "Contact proxy sphere must NOT be shifted by IK target.");

            feedback.ClearIkOverride();
        }

        [UnityTest]
        public IEnumerator VerifyHeadAndBodyLookWeightsFollowInteraction()
        {
            var loadOp = EditorSceneManager.LoadSceneAsyncInPlayMode(
                HandshakeScenePath,
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            while (!loadOp.isDone) yield return null;
            yield return new WaitForSeconds(0.4f);

            var player = ObjectUtility.FindObject<WhiteRoomCharacter>(false);
            var participant = player.GetComponent<HandshakeParticipant>();
            var ai = ObjectUtility.FindObject<HandshakeAiController>(false);
            if (ai != null) ai.Mode = HandshakeAiMode.Idle;

            var feedback = player.GetComponentInChildren<HandshakeVisualFeedback>();
            Assert.IsNotNull(feedback, "Feedback component must exist.");
            var settings = HandshakeArbitrator.Instance.Settings;
            Assert.Greater(settings.HeadLookWeight, 0f, "Settings must define a positive head look weight.");
            Assert.Greater(settings.BodyLookWeight, 0f, "Settings must define a positive body look weight.");

            Assert.AreEqual(HandshakeState.None, participant.State, "Participant must start standing.");
            Assert.AreEqual(0f, feedback.HeadLookWeight, 1e-4f, "Head weight must be 0 while standing.");
            Assert.AreEqual(0f, feedback.BodyLookWeight, 1e-4f, "Body weight must be 0 while standing.");

            Quaternion rootRotationBefore = player.transform.rotation;
            Vector3 rootPositionBefore = player.transform.position;

            player.OverrideIntent = true;
            var reachIntent = CombatActionIntent.Empty;
            reachIntent.InteractHold = true;
            player.CustomIntent = reachIntent;
            yield return new WaitForSeconds(0.6f);

            Assert.AreNotEqual(HandshakeState.None, participant.State, "Participant must be interacting.");
            Assert.Greater(feedback.HeadLookWeight, 0.1f, "Head weight must be above 0 during the interaction.");
            Assert.Greater(feedback.BodyLookWeight, 0.1f, "Spine weight must be above 0 during the interaction.");
            Assert.LessOrEqual(feedback.HeadLookWeight, settings.HeadLookWeight + 1e-4f);
            Assert.LessOrEqual(feedback.BodyLookWeight, settings.BodyLookWeight + 1e-4f);
            Assert.AreEqual(0f, Quaternion.Angle(rootRotationBefore, player.transform.rotation), 0.01f,
                "Procedural head and body animation must not change the root rotation.");
            Assert.AreEqual(0f, Vector3.Distance(rootPositionBefore, player.transform.position), 0.05f,
                "Reaching must not move the root through the procedural animation.");

            player.CustomIntent = CombatActionIntent.Empty;
            float wait = 0f;
            while (participant.State != HandshakeState.None && wait < 3f)
            {
                wait += Time.deltaTime;
                yield return null;
            }
            Assert.AreEqual(HandshakeState.None, participant.State, "Participant must return to standing.");
            yield return new WaitForSeconds(0.6f);

            Assert.AreEqual(0f, feedback.HeadLookWeight, 1e-4f, "Head weight must return to 0 after the interaction.");
            Assert.AreEqual(0f, feedback.BodyLookWeight, 1e-4f, "Body weight must return to 0 after the interaction.");
            player.OverrideIntent = false;
        }

        [UnityTest]
        public IEnumerator VerifyItem1_ControlIntervalsGovernRePress()
        {
            var loadOp = EditorSceneManager.LoadSceneAsyncInPlayMode(
                HandshakeScenePath,
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            while (!loadOp.isDone) yield return null;
            yield return new WaitForSeconds(0.4f);

            var player = ObjectUtility.FindObject<WhiteRoomCharacter>(false);
            var participant = player.GetComponent<HandshakeParticipant>();
            var ai = ObjectUtility.FindObject<HandshakeAiController>(false);
            if (ai != null) ai.Mode = HandshakeAiMode.Idle;

            player.OverrideIntent = true;

            // 1. Initiate reaching, then release to withdraw
            var reachIntent = CombatActionIntent.Empty;
            reachIntent.InteractHold = true;
            player.CustomIntent = reachIntent;
            yield return new WaitForSeconds(0.15f);
            Assert.AreEqual(HandshakeState.Reaching, participant.State, "Must be Reaching");

            // Release interact -> enters Withdrawing
            player.CustomIntent = CombatActionIntent.Empty;
            yield return null;
            Assert.AreEqual(HandshakeState.Withdrawing, participant.State, "Must be Withdrawing");

            // withdraw clip is 15 frames (0.50s):
            // Frames 0-10 (0.00-0.33s): UnderControl = false
            // Frames 10-15 (0.33-0.50s): UnderControl = true

            // Test uncontrol interval at t ~ 0.10s: re-press must be REJECTED
            yield return new WaitForSeconds(0.08f);
            var repressIntent = CombatActionIntent.Empty;
            repressIntent.InteractDown = true;
            repressIntent.InteractHold = true;
            player.CustomIntent = repressIntent;
            yield return null;
            Assert.AreEqual(HandshakeState.Withdrawing, participant.State, "Re-press during uncontrol interval must be rejected and remain Withdrawing");

            player.CustomIntent = CombatActionIntent.Empty;
            yield return null;
            // Wait until entering controllable interval (t ~ 0.43s, normalized time ~0.86)
            yield return new WaitForSeconds(0.35f);
            Assert.AreEqual(HandshakeState.Withdrawing, participant.State, "Still Withdrawing in controllable interval");

            // Test controllable interval: re-press must SUCCEED into Reaching
            repressIntent = CombatActionIntent.Empty;
            repressIntent.InteractDown = true;
            repressIntent.InteractHold = true;
            player.CustomIntent = repressIntent;
            yield return null;
            Assert.AreEqual(HandshakeState.Reaching, participant.State, "Re-press during controllable interval must succeed and enter Reaching");

            player.CustomIntent = CombatActionIntent.Empty;
            player.OverrideIntent = false;
        }

        [UnityTest]
        public IEnumerator VerifyItem2_ClipsActuallyPlayInAnimator()
        {
            var loadOp = EditorSceneManager.LoadSceneAsyncInPlayMode(
                HandshakeScenePath,
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            while (!loadOp.isDone) yield return null;
            yield return new WaitForSeconds(0.4f);

            var player = ObjectUtility.FindObject<WhiteRoomCharacter>(false);
            var participant = player.GetComponent<HandshakeParticipant>();
            var feedback = player.GetComponentInChildren<HandshakeVisualFeedback>();
            var settings = HandshakeArbitrator.Instance.Settings;

            Assert.IsNotNull(feedback, "Feedback component must exist.");
            Assert.IsNotNull(settings, "Settings must exist.");

            // Test states and their corresponding clips
            HandshakeState[] testStates = new HandshakeState[]
            {
                HandshakeState.Reaching,
                HandshakeState.Waiting,
                HandshakeState.Withdrawing,
                HandshakeState.Holding,
                HandshakeState.Caught,
                HandshakeState.Catching,
                HandshakeState.Leap
            };

            bool origArbEnabled = HandshakeArbitrator.Instance != null && HandshakeArbitrator.Instance.enabled;
            if (HandshakeArbitrator.Instance != null) HandshakeArbitrator.Instance.enabled = false;

            try
            {
                foreach (var state in testStates)
                {
                    if (state == HandshakeState.Reaching || state == HandshakeState.Waiting || state == HandshakeState.Holding)
                    {
                        player.OverrideIntent = true;
                        var intent = CombatActionIntent.Empty;
                        intent.InteractHold = true;
                        player.CustomIntent = intent;
                    }
                    else
                    {
                        player.CustomIntent = CombatActionIntent.Empty;
                        player.OverrideIntent = false;
                    }

                    participant.SetState(state);
                    yield return null;
                    yield return null;

                    AnimationClip playingClip = feedback.GetCurrentPlayingClip();
                    AnimationClip expectedClip = settings.GetClipForState(state);

                    Assert.IsNotNull(expectedClip, $"Settings clip for {state} must not be null.");
                    Assert.IsNotNull(playingClip, $"Playing clip in Animator for {state} must not be null.");
                    Assert.AreEqual(expectedClip.name, playingClip.name, $"Animator clip for {state} must match Settings clip.");
                }

                // Verify that swapping a clip in settings causes the Animator to play the swapped override clip (distinguishing from controller default)
                AnimationClip origLaughClip = settings.LaughClip;
                AnimationClip replacementClip = settings.ClapLaughClip;
                Assert.IsNotNull(origLaughClip, "Original LaughClip must not be null.");
                Assert.IsNotNull(replacementClip, "Replacement ClapLaughClip must not be null.");
                Assert.AreNotEqual(origLaughClip.name, replacementClip.name, "Original and replacement clips must have distinct names.");

                try
                {
                    settings.LaughClip = replacementClip;
                    feedback.ConfigureAnimatorOverrides();

                    player.CustomIntent = CombatActionIntent.Empty;
                    player.OverrideIntent = false;

                    participant.SetState(HandshakeState.Laugh);
                    yield return null;
                    yield return null;

                    AnimationClip playingLaugh = feedback.GetCurrentPlayingClip();
                    Assert.IsNotNull(playingLaugh, "Playing clip for overridden Laugh state must not be null.");
                    Assert.AreEqual(replacementClip.name, playingLaugh.name, "Animator must play the swapped override clip from settings, NOT the controller default.");
                }
                finally
                {
                    settings.LaughClip = origLaughClip;
                    feedback.ConfigureAnimatorOverrides();
                }
            }
            finally
            {
                if (HandshakeArbitrator.Instance != null) HandshakeArbitrator.Instance.enabled = origArbEnabled;
            }
        }

        [UnityTest]
        public IEnumerator VerifyItem3_RootMotionDisplacementInCaughtCatchingLeap()
        {
            var loadOp = EditorSceneManager.LoadSceneAsyncInPlayMode(
                HandshakeScenePath,
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            while (!loadOp.isDone) yield return null;
            yield return new WaitForSeconds(0.4f);

            var player = ObjectUtility.FindObject<WhiteRoomCharacter>(false);
            var participant = player.GetComponent<HandshakeParticipant>();

            // 1. Verify Caught root motion
            participant.SetState(HandshakeState.Caught);
            Vector3 pos0 = player.transform.position;
            yield return new WaitForSeconds(0.4f);
            Vector3 pos1 = player.transform.position;
            float caughtDisp = Vector3.Distance(pos0, pos1);
            Assert.Greater(caughtDisp, 0.02f, "Caught state must produce root motion displacement along XZ.");

            // 2. Verify Leap root motion (vertical jump and horizontal displacement)
            participant.SetState(HandshakeState.Leap);
            Vector3 leapPos0 = player.transform.position;
            yield return new WaitForSeconds(0.25f);
            Vector3 leapPosMid = player.transform.position;
            Assert.Greater(leapPosMid.y, leapPos0.y + 0.04f, "Leap state must displace character upwards via root motion.");
            float horizDisp = Vector2.Distance(new Vector2(leapPos0.x, leapPos0.z), new Vector2(leapPosMid.x, leapPosMid.z));
            Assert.Greater(horizDisp, 0.03f, "Leap state must displace character backwards via root motion.");
        }

        [UnityTest]
        public IEnumerator VerifyItem4_StandardDistanceWalkingAndNaturalHandSphere()
        {
            var loadOp = EditorSceneManager.LoadSceneAsyncInPlayMode(
                HandshakeScenePath,
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            while (!loadOp.isDone) yield return null;
            yield return new WaitForSeconds(0.4f);

            var player = ObjectUtility.FindObject<WhiteRoomCharacter>(false);
            var participant = player.GetComponent<HandshakeParticipant>();
            var ai = ObjectUtility.FindObject<HandshakeAiController>(false);
            var aiParticipant = ai.GetComponent<HandshakeParticipant>();

            // 1. Verify Hand Sphere Radius is natural hand size (0.18m, <= 0.25m)
            var playerSphere = participant.ContactBridge.GetComponent<SphereCollider>();
            var aiSphere = aiParticipant.ContactBridge.GetComponent<SphereCollider>();
            Assert.LessOrEqual(playerSphere.radius, 0.25f, "Player hand sphere radius must be natural hand size (<= 0.25m).");
            Assert.LessOrEqual(aiSphere.radius, 0.25f, "AI hand sphere radius must be natural hand size (<= 0.25m).");

            // 2. Verify CharacterController radius is 0.30m
            var playerCc = player.GetComponent<CharacterController>();
            var aiCc = ai.GetComponent<CharacterController>();
            Assert.AreEqual(0.30f, playerCc.radius, 0.01f, "Player CharacterController radius must be 0.30m.");
            Assert.AreEqual(0.30f, aiCc.radius, 0.01f, "AI CharacterController radius must be 0.30m.");

            // 3. Characters can walk to standard 0.80m distance without capsule obstruction
            HandshakeArbitrator.Instance.ResetAll();
            yield return new WaitForSeconds(0.2f);

            player.OverrideIntent = true;
            float walkTime = 0f;
            while (walkTime < 2.5f && Vector3.Distance(player.transform.position, ai.transform.position) > 0.82f)
            {
                var moveIntent = CombatActionIntent.Empty;
                Vector3 toAi = (ai.transform.position - player.transform.position).normalized;
                moveIntent.MoveInput = new Vector2(toAi.x, toAi.z);
                player.CustomIntent = moveIntent;
                walkTime += Time.deltaTime;
                yield return null;
            }

            player.CustomIntent = CombatActionIntent.Empty;
            yield return null;

            float pelvisDistanceAtArrival = Vector3.Distance(player.transform.position, ai.transform.position);
            Assert.LessOrEqual(pelvisDistanceAtArrival, 0.85f, "Characters must be able to walk to standard distance (~0.80m) without capsule obstruction.");
            Assert.GreaterOrEqual(pelvisDistanceAtArrival, 0.70f, "Characters must not clip into each other.");

            // 4. Physical hand sphere contact occurs at standard distance
            var reachHold = CombatActionIntent.Empty;
            reachHold.InteractHold = true;
            player.CustomIntent = reachHold;

            ai.Mode = HandshakeAiMode.AiResponder;
            ai.StopDistance = 0.80f;

            float waitContact = 0f;
            bool reachedGrip = false;
            while (waitContact < 2.0f && !reachedGrip)
            {
                waitContact += Time.deltaTime;
                if (HandshakeArbitrator.Instance.SessionState == HandshakeState.Grip ||
                    HandshakeArbitrator.Instance.SessionState == HandshakeState.Holding)
                {
                    reachedGrip = true;
                }
                yield return null;
            }

            Assert.IsTrue(reachedGrip, "Physical hand sphere contact must occur at standard walking distance.");

            player.CustomIntent = CombatActionIntent.Empty;
            player.OverrideIntent = false;

            // 5. Verify Combat scene character radius remains 0.50m
            var loadCombatOp = EditorSceneManager.LoadSceneAsyncInPlayMode(
                CombatScenePath,
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            while (!loadCombatOp.isDone) yield return null;
            yield return new WaitForSeconds(0.3f);

            var combatChar = ObjectUtility.FindObject<WhiteRoomCharacter>(false);
            var combatCc = combatChar.GetComponent<CharacterController>();
            Assert.AreEqual(0.50f, combatCc.radius, 0.01f, "Combat scene CharacterController radius must remain 0.50m.");
        }

        [UnityTest]
        public IEnumerator VerifyEscape_DoesNotResolve_WhenArmReachCannotBeMeasured()
        {
            var loadOp = EditorSceneManager.LoadSceneAsyncInPlayMode(
                HandshakeScenePath,
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            while (!loadOp.isDone) yield return null;
            yield return new WaitForSeconds(0.4f);

            var player = ObjectUtility.FindObject<WhiteRoomCharacter>(false);
            var participant = player.GetComponent<HandshakeParticipant>();
            var ai = ObjectUtility.FindObject<HandshakeAiController>(false);
            var aiParticipant = ai.GetComponent<HandshakeParticipant>();
            var arb = HandshakeArbitrator.Instance;
            Assert.IsNotNull(arb, "HandshakeArbitrator must exist.");

            // 1. Move player far away (> 5.0m)
            player.transform.position = new Vector3(0, 0, 5.0f);
            ai.transform.position = Vector3.zero;

            player.OverrideIntent = true;
            var holdIntent = CombatActionIntent.Empty;
            holdIntent.InteractHold = true;
            player.CustomIntent = holdIntent;
            ai.Mode = HandshakeAiMode.Idle;

            var aiAnim = ai.GetComponentInChildren<Animator>();
            Assert.IsNotNull(aiAnim, "AI Animator must exist.");
            Avatar origAvatar = aiAnim.avatar;

            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var arbType = typeof(HandshakeArbitrator);
            try
            {
                arbType.GetField("_participantA", flags).SetValue(arb, participant);
                arbType.GetField("_participantB", flags).SetValue(arb, aiParticipant);
                aiAnim.avatar = null;

                LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Cannot measure arm reach"));
                arbType.GetMethod("EnterCaughtState", flags).Invoke(arb, new object[] { participant, aiParticipant });

                Assert.AreEqual(HandshakeState.Catching, arb.SessionState, "Arbitrator must be in the catching session so the escape check actually runs.");
                Assert.AreEqual(-1f, arb.GetEffectiveEscapeThreshold(), "Escape threshold must be -1 when arm bones cannot be measured.");

                yield return new WaitForSeconds(0.2f);

                Assert.AreEqual(HandshakeState.Catching, arb.SessionState, "Session must stay in catching when the threshold cannot be measured.");
                Assert.AreEqual(HandshakeState.Caught, aiParticipant.State, "Participant must remain Caught when threshold is -1.");
                Assert.AreNotEqual(HandshakeOutcome.Escaped, arb.LastOutcome, "Escape outcome must not be resolved when threshold is -1.");
            }
            finally
            {
                aiAnim.avatar = origAvatar;
                player.CustomIntent = CombatActionIntent.Empty;
                player.OverrideIntent = false;
            }
        }
#endif
    }
}
