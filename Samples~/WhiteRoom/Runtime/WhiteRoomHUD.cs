using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Yu5h1Lib;

namespace Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom
{
    /// <summary>
    /// Represents a timestamped log entry in a WhiteRoom session.
    /// </summary>
    [Serializable]
    public struct CombatLogEntry
    {
        public float timestamp;
        public string message;
    }

    /// <summary>
    /// Serializable container for exporting WhiteRoom session logs to JSON.
    /// </summary>
    [Serializable]
    public class CombatSessionLogExport
    {
        public string sessionTimestamp;
        public string unityVersion;
        public string scenario;
        public List<CombatLogEntry> logs = new List<CombatLogEntry>();
    }

    /// <summary>
    /// Shared IMGUI HUD displaying entity states, active interaction mode, key rebinding, scenario controls, and log export.
    /// </summary>
    public class WhiteRoomHUD : MonoBehaviour
    {
        private readonly List<string> _actionLogs = new List<string>();
        private readonly List<CombatLogEntry> _allHistoryLogs = new List<CombatLogEntry>();
        private const int MaxDisplayLogs = 7;

        private readonly SimulatedNetworkChannel _networkChannel = new SimulatedNetworkChannel();
        private bool _showDebugPanel = true;

        private Rect _rectStatusPanel;
        private Rect _rectDebugPanel;
        private Rect _rectHelpPanel;
        private Rect _rectLogsPanel;
        private Rect _rectCustomPanel;

        private int _rebindingPlayerIndex = -1;

        public SimulatedNetworkChannel NetworkChannel => _networkChannel;
        public bool IsInteractionMode { get; set; }
        public string ActiveInteractionLabel { get; set; } = "握手 (Handshake)";
        public string CustomScenarioName { get; set; } = "";
        public Action CustomStatusGUI { get; set; }
        public Action CustomScenarioGUI { get; set; }
        public Action CustomHelpGUI { get; set; }
        public IReadOnlyList<CombatLogEntry> AllHistoryLogs => _allHistoryLogs;

        private void Start()
        {
            RefreshBindings();

            CombatEntity.OnGlobalArenaReset += HandleGlobalReset;

            _networkChannel.OnPacketDropped += msg =>
            {
                LogAction($"<color=red>[網路丟包] 序號 #{msg.SequenceId} 遺失！</color>");
            };
        }

        private void OnDestroy()
        {
            CombatEntity.OnGlobalArenaReset -= HandleGlobalReset;
            UnbindAll();
        }

        private void Update()
        {
            _networkChannel.UpdateQueue();

            if (InputHandler.GetKeyDown(KeyCode.F1))
            {
                _showDebugPanel = !_showDebugPanel;
            }
        }

        /// <summary>
        /// Subscribes the HUD log stream to all currently active <see cref="CombatEntity"/> instances.
        /// </summary>
        public void RefreshBindings()
        {
            UnbindAll();
            foreach (var entity in CombatEntity.ActiveEntities)
            {
                if (entity != null)
                {
                    entity.OnCombatLog += LogAction;
                }
            }
        }

        private void UnbindAll()
        {
            foreach (var entity in CombatEntity.ActiveEntities)
            {
                if (entity != null)
                {
                    entity.OnCombatLog -= LogAction;
                }
            }
        }

        private void HandleGlobalReset()
        {
            _networkChannel.ResetStats();
            LogAction("【全場統一重設】角色狀態、位置、AI 設定及佇列已還原。");
        }

        /// <summary>
        /// Appends a message to the on-screen and exportable session log history.
        /// </summary>
        public void LogAction(string message)
        {
            string formatted = $"[{Time.time:F2}s] {message}";
            _actionLogs.Add(formatted);
            if (_actionLogs.Count > MaxDisplayLogs)
            {
                _actionLogs.RemoveAt(0);
            }

            _allHistoryLogs.Add(new CombatLogEntry
            {
                timestamp = Time.time,
                message = message
            });
        }

        /// <summary>
        /// Clears the accumulated log history.
        /// </summary>
        public void ClearLogs()
        {
            _actionLogs.Clear();
            _allHistoryLogs.Clear();
        }

        /// <summary>
        /// Exports the structured session log history to a JSON file in <see cref="Application.persistentDataPath"/> or a custom path.
        /// </summary>
        public string ExportStructuredLogs(string customFilePath = null)
        {
            var builder = ObjectUtility.FindObject<WhiteRoomSceneBuilder>(false);
            string scenarioLabel = !string.IsNullOrEmpty(CustomScenarioName)
                ? CustomScenarioName
                : (builder != null ? builder.CurrentScenario.ToString() : "Default");

            var export = new CombatSessionLogExport
            {
                sessionTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                unityVersion = Application.unityVersion,
                scenario = scenarioLabel,
                logs = new List<CombatLogEntry>(_allHistoryLogs)
            };

            string json = JsonUtility.ToJson(export, true);
            string filename = IsInteractionMode
                ? $"WhiteRoom_Handshake_Logs_{DateTime.Now:yyyyMMdd_HHmmss}.json"
                : $"WhiteRoom_Logs_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            string path = string.IsNullOrEmpty(customFilePath)
                ? Path.Combine(Application.persistentDataPath, filename)
                : customFilePath;

            try
            {
                File.WriteAllText(path, json);
                LogAction($"<color=cyan>【紀錄匯出成功】({export.logs.Count} 筆) 路徑: {path}</color>");
                Debug.Log($"[WhiteRoomHUD] 結構化日誌成功匯出至: {path}");
            }
            catch (Exception ex)
            {
                LogAction($"<color=red>【紀錄匯出失敗】{ex.Message}</color>");
            }

            return path;
        }

        private void HandleInteractiveKeyRebinding()
        {
            if (_rebindingPlayerIndex < 0) return;

            Event e = Event.current;
            if (e != null && e.isKey && e.type == EventType.KeyDown && e.keyCode != KeyCode.None)
            {
                if (e.keyCode != KeyCode.Escape)
                {
                    var characters = ObjectUtility.FindObjects<WhiteRoomCharacter>(false);
                    foreach (var ch in characters)
                    {
                        if (ch == null) continue;
                        if (_rebindingPlayerIndex == 1 && !ch.UsePlayer2ControlScheme)
                        {
                            ch.InteractKey = e.keyCode;
                        }
                        else if (_rebindingPlayerIndex == 2 && ch.UsePlayer2ControlScheme)
                        {
                            ch.Player2InteractKey = e.keyCode;
                        }
                    }

                    if (_rebindingPlayerIndex == 2)
                    {
                        foreach (var ch in characters)
                        {
                            if (ch != null) ch.Player2InteractKey = e.keyCode;
                        }
                    }

                    LogAction($"[按鍵設定] P{_rebindingPlayerIndex} 互動鍵已改為: {e.keyCode}");
                }
                _rebindingPlayerIndex = -1;
                e.Use();
            }
        }

        private void OnGUI()
        {
            HandleInteractiveKeyRebinding();

            GUI.skin.label.fontSize = 12;
            GUI.skin.box.fontSize = 12;
            GUI.skin.button.fontSize = 12;

            _rectStatusPanel = new Rect(15, 15, 390, IsInteractionMode ? 260 : 275);
            _rectCustomPanel = new Rect(15, 285, 390, 230);
            _rectDebugPanel = new Rect(Screen.width - 340, 15, 325, 295);
            _rectHelpPanel = new Rect(15, Screen.height - 185, 430, 170);
            _rectLogsPanel = new Rect(Screen.width - 480, Screen.height - 215, 465, 200);

            Vector2 mousePos = Event.current.mousePosition;
            bool isMouseOverUI = _rectStatusPanel.Contains(mousePos) ||
                                (!IsInteractionMode && _showDebugPanel && _rectDebugPanel.Contains(mousePos)) ||
                                (IsInteractionMode && CustomScenarioGUI != null && _rectCustomPanel.Contains(mousePos)) ||
                                _rectHelpPanel.Contains(mousePos) ||
                                _rectLogsPanel.Contains(mousePos);
            WhiteRoomCharacter.IsUIFocused = isMouseOverUI;

            GUILayout.BeginArea(_rectStatusPanel, GUI.skin.box);
            if (IsInteractionMode)
            {
                GUILayout.Label("<b><size=14>WhiteRoom 移動＋握手互動 (Stage 2b)</size></b>");
                GUILayout.Label($"<color=#4da6ff><b>目前選擇的互動動作：{ActiveInteractionLabel}</b></color>");
                GUILayout.Space(2);
                DrawKeyRebindRow();
                GUILayout.Space(4);
                CustomStatusGUI?.Invoke();
            }
            else
            {
                GUILayout.Label("<b><size=14>WhiteRoom 戰鬥與情境控制</size></b>");
                GUILayout.Space(2);

                var entities = CombatEntity.ActiveEntities;
                for (int i = 0; i < entities.Count; i++)
                {
                    var ent = entities[i];
                    if (ent == null) continue;

                    string teamLabel = ent.TeamId == 0 ? "<color=#4da6ff>[隊伍0-友方]</color>" : "<color=#ff6666>[隊伍1-敵方]</color>";
                    GUILayout.Label($"<b>{ent.EntityId}</b> {teamLabel} HP: {ent.CurrentHealth:F0}/{ent.MaxHealth:F0} [{ent.State}]");
                    Color barColor = ent.TeamId == 0 ? Color.green : Color.red;
                    DrawHealthBar(ent.CurrentHealth, ent.MaxHealth, barColor);
                }
            }

            GUILayout.Space(6);
            if (GUILayout.Button("【全場快速重設 [R]】"))
            {
                CombatEntity.ResetAllArenaEntities();
            }

            if (!IsInteractionMode)
            {
                GUILayout.Space(4);
                GUILayout.Label("<b>情境配置切換：</b>");
                GUILayout.BeginHorizontal();
                var builder = ObjectUtility.FindObject<WhiteRoomSceneBuilder>(false);
                if (GUILayout.Button("1P vs 1 AI") && builder != null) builder.ApplyScenario(ArenaScenario.OnePlayerVsOneAI);
                if (GUILayout.Button("1P vs 2 AI") && builder != null) builder.ApplyScenario(ArenaScenario.OnePlayerVsTwoAI);
                if (GUILayout.Button("2P vs 2 AI") && builder != null) builder.ApplyScenario(ArenaScenario.TwoPlayerVsTwoAI);
                if (GUILayout.Button("2P PvP") && builder != null) builder.ApplyScenario(ArenaScenario.TwoPlayerPvP);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndArea();

            if (IsInteractionMode && CustomScenarioGUI != null)
            {
                GUILayout.BeginArea(_rectCustomPanel, GUI.skin.box);
                CustomScenarioGUI.Invoke();
                GUILayout.EndArea();
            }

            if (!IsInteractionMode && _showDebugPanel)
            {
                GUILayout.BeginArea(_rectDebugPanel, GUI.skin.box);
                GUILayout.Label("<b><size=13>診斷面板 (F1 開關) - 網路狀態標記</size></b>");
                GUILayout.Space(2);
                GUILayout.Label("• <b>真實傳輸層</b>：<color=yellow>未連接 (不可用 / N/A)</color>");
                GUILayout.Label("• <b>主機判定延遲 / 修正</b>：<color=orange>[不可用 - 尚未接入真實網路]</color>");
                GUILayout.Label("• <b>本地模擬通道</b>：單一程序記憶體佇列 (僅供延遲參數實驗)");
                GUILayout.Space(4);

                GUILayout.Label($"模擬延遲 (Latency)：{_networkChannel.LatencyMs:F0} ms");
                _networkChannel.LatencyMs = GUILayout.HorizontalSlider(_networkChannel.LatencyMs, 0f, 300f);

                GUILayout.Label($"模擬抖動 (Jitter)：±{_networkChannel.JitterMs:F0} ms");
                _networkChannel.JitterMs = GUILayout.HorizontalSlider(_networkChannel.JitterMs, 0f, 50f);

                GUILayout.Label($"模擬丟包率 (Packet Loss)：{(_networkChannel.PacketLossRate * 100f):F0} %");
                _networkChannel.PacketLossRate = GUILayout.HorizontalSlider(_networkChannel.PacketLossRate, 0f, 0.25f);

                GUILayout.Space(4);
                GUILayout.Label($"佇列送出: {_networkChannel.TotalSent} | 遺失: {_networkChannel.TotalDropped}");

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("發送心跳封包"))
                {
                    _networkChannel.SendPacket("P1", CombatActionIntent.Empty);
                }
                if (GUILayout.Button("【匯出結構化紀錄 (JSON)】"))
                {
                    ExportStructuredLogs();
                }
                GUILayout.EndHorizontal();

                GUILayout.EndArea();
            }

            GUILayout.BeginArea(_rectHelpPanel, GUI.skin.box);
            if (IsInteractionMode && CustomHelpGUI != null)
            {
                CustomHelpGUI.Invoke();
            }
            else
            {
                GUILayout.Label("<b>操作說明 (UX Verified)</b>");
                GUILayout.Label("• <b>WASD</b>：8 方向移動；<b>Space</b>：起跳落地恢復");
                GUILayout.Label("• <b>Left Ctrl</b>：按住蹲下（放開起身）；<b>C</b>：切換蹲下");
                GUILayout.Label("• <b>滑鼠左鍵</b>：出招（前搖 0.2s -> 打擊 0.15s -> 收招 0.25s）");
                GUILayout.Label("• <b>滑鼠右鍵</b>：長按防禦（減傷80%，可<b>警戒慢步</b>）；<b>快速點按放開</b>觸發<b>精準彈刀 (Parry)</b> 中止敵方攻擊！");
                GUILayout.Label("• <b>R 鍵</b>：全場重設（重置生命、位置、AI冷卻與目標、網路佇列）");
                GUILayout.Label("• <b>F1</b>：開關診斷面板；滑鼠進入面板時自動釋放游標並暫停戰鬥輸入");
            }
            GUILayout.EndArea();

            GUILayout.BeginArea(_rectLogsPanel, GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"<b>即時判定與事件紀錄（共 {_allHistoryLogs.Count} 筆）</b>");
            if (GUILayout.Button("匯出 JSON", GUILayout.Width(95)))
            {
                ExportStructuredLogs();
            }
            GUILayout.EndHorizontal();
            foreach (var log in _actionLogs)
            {
                GUILayout.Label(log);
            }
            GUILayout.EndArea();
        }

        private void DrawKeyRebindRow()
        {
            var player = ObjectUtility.FindObject<WhiteRoomCharacter>(false);
            KeyCode p1Key = player != null ? player.InteractKey : KeyCode.E;
            KeyCode p2Key = player != null ? player.Player2InteractKey : KeyCode.Keypad3;

            GUILayout.BeginHorizontal();
            string p1Text = _rebindingPlayerIndex == 1 ? "<color=yellow>[請按任意鍵...]</color>" : $"P1 互動鍵: [{p1Key}]";
            if (GUILayout.Button(p1Text))
            {
                _rebindingPlayerIndex = _rebindingPlayerIndex == 1 ? -1 : 1;
            }

            string p2Text = _rebindingPlayerIndex == 2 ? "<color=yellow>[請按任意鍵...]</color>" : $"P2 互動鍵: [{p2Key}]";
            if (GUILayout.Button(p2Text))
            {
                _rebindingPlayerIndex = _rebindingPlayerIndex == 2 ? -1 : 2;
            }
            GUILayout.EndHorizontal();
        }

        private void DrawHealthBar(float current, float max, Color barColor)
        {
            Rect r = GUILayoutUtility.GetRect(200, 12);
            GUI.Box(r, "");
            float pct = Mathf.Clamp01(current / Mathf.Max(1f, max));
            Rect fillRect = new Rect(r.x + 1, r.y + 1, (r.width - 2) * pct, r.height - 2);

            Color oldColor = GUI.color;
            GUI.color = barColor;
            GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
            GUI.color = oldColor;
        }
    }
}
