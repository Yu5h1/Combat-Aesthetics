#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom.Handshake
{
    /// <summary>
    /// Inspector for <see cref="HandshakeSceneBuilder"/>. While playing it shows the arbitration status in a HelpBox that keeps
    /// refreshing, a quick reset button and one button per outcome scenario. It replaces the on-screen IMGUI panels, so nothing
    /// is drawn in the Game view or in a build; the former sliders are fields of <see cref="HandshakeSettings"/> and of the AI
    /// controller, edited in their own Inspectors.
    /// </summary>
    [CustomEditor(typeof(HandshakeSceneBuilder))]
    public class HandshakeSceneBuilderEditor : Editor
    {
        private static readonly (string label, HandshakeTestScenario scenario)[] s_Scenarios =
        {
            ("成功：AI 發起，玩家走近握手", HandshakeTestScenario.AiInitiatorSuccess),
            ("成功：玩家發起，AI 走近回應", HandshakeTestScenario.AiResponderSuccess),
            ("取消：AI 伸手未碰到即收手", HandshakeTestScenario.AiCancel),
            ("惡作劇：玩家握緊中途放開", HandshakeTestScenario.PlayerPrank),
            ("握緊結束後才放開", HandshakeTestScenario.ReleaseAfterGrip),
            ("惡作劇：AI 握緊中途抽手", HandshakeTestScenario.AiPrank),
            ("同時收手", HandshakeTestScenario.BothWithdraw),
            ("比力氣：AI 勝", HandshakeTestScenario.ContestAiWin),
            ("比力氣：玩家勝", HandshakeTestScenario.ContestPlayerWin),
            ("比力氣：門檻內平手", HandshakeTestScenario.ContestTie),
            ("比力氣：0 比 0", HandshakeTestScenario.ContestZeroZero),
            ("抓住與掙脫：玩家抓 AI，AI 掙脫", HandshakeTestScenario.PlayerCatchesAiEscape),
            ("抓住與掙脫：AI 抓玩家", HandshakeTestScenario.AiCatchesPlayer),
        };

        public override bool RequiresConstantRepaint() => Application.isPlaying;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("進入播放模式後，這裡顯示判定狀態並提供測試按鈕。", MessageType.Info);
                return;
            }

            var builder = (HandshakeSceneBuilder)target;
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(BuildStatus(builder), MessageType.None);

            if (GUILayout.Button("快速重設 [R]"))
            {
                builder.QuickReset();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("重現結果", EditorStyles.boldLabel);
            foreach (var (label, scenario) in s_Scenarios)
            {
                if (GUILayout.Button(label))
                {
                    builder.StartTestScenario(scenario);
                }
            }
        }

        private static string BuildStatus(HandshakeSceneBuilder builder)
        {
            var sb = new StringBuilder();
            var arb = HandshakeArbitrator.Instance;
            if (arb == null)
            {
                return "判定器尚未建立。";
            }

            sb.AppendLine($"判定狀態：{arb.SessionState}　最新結果：{arb.LastOutcome}");
            sb.AppendLine($"比力氣次數：玩家 {arb.PlayerTaps}　AI {arb.AiTaps}　身體距離：{arb.BodyDistance:F2} m");
            if (arb.HasPair)
            {
                sb.AppendLine($"臂長合計：{arb.MeasureTotalArmReach():F2} m　掙脫門檻：{arb.GetEffectiveEscapeThreshold():F2} m（補償 {arb.Settings.EscapeDistanceCompensation:+0.00;-0.00;0.00} m）");
            }
            else
            {
                sb.AppendLine("臂長與掙脫門檻：未配對");
            }

            var player = builder.PlayerParticipant;
            if (player != null)
            {
                sb.AppendLine($"玩家：姿態 {player.State}　按住互動 {(player.CurrentIntent.InteractHold ? "是" : "否")}　按下次數 {player.InteractPressCount}{(player.RequiresInteractRelease ? "（需先放開再按）" : "")}");
            }

            var ai = builder.AI;
            var aiParticipant = builder.AiParticipant;
            if (ai != null && aiParticipant != null)
            {
                sb.Append($"AI：模式 {ai.Mode}　姿態 {aiParticipant.State}　按住互動 {(aiParticipant.CurrentIntent.InteractHold ? "是" : "否")}　按下次數 {aiParticipant.InteractPressCount}{(aiParticipant.RequiresInteractRelease ? "（需先放開再按）" : "")}");
            }

            return sb.ToString().TrimEnd();
        }
    }
}
#endif
