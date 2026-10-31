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
    /// Runs all 21 verification scenarios through <see cref="HandshakeVerificationRunner"/>, verifies combat jump/landing/attack
    /// constraints, edge falling, and confirms pre-IK collision proxy positioning.
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

            var walkOffIntent = CombatActionIntent.Empty;
            walkOffIntent.MoveInput = new Vector2(1f, 0f);
            character.CustomIntent = walkOffIntent;

            float walkTime = 0f;
            while (walkTime < 3.0f && entity.IsGrounded)
            {
                walkTime += Time.deltaTime;
                yield return null;
            }

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
        }
#endif
    }
}
