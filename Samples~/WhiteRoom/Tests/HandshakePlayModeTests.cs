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
    /// PlayMode automated test fixture for the WhiteRoom Stage 2b handshake interaction.
    /// Runs all 10 verification scenarios through <see cref="HandshakeVerificationRunner"/> and exports structured logs.
    /// </summary>
    public class HandshakePlayModeTests
    {
        private const string HandshakeScenePath = "Assets/Samples/Combat Aesthetic/1.0.0/WhiteRoom/Handshake/Handshake.unity";
        private const string CombatScenePath = "Assets/Samples/Combat Aesthetic/1.0.0/WhiteRoom/Combat/WhiteRoom.unity";

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator RunHandshakeBatch2VerificationSuite()
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
                string exportPath = "C:/Users/Yu5h1/Dev/VSProjects/Yu5h1Lib/Unity/Combat-Aesthetics/Documentation/WhiteRoom_Handshake_Logs.json";
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

            yield return new WaitForSeconds(0.2f);
            Assert.IsTrue(cam.transform.position != Vector3.zero, "Combat camera should be positioned tracking the player.");
        }
#endif
    }
}
