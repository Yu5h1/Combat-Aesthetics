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
    /// Session log collector shared by the Combat and Handshake scenes. It draws nothing on screen (no IMGUI, in the Editor or in a
    /// build): entity logs are gathered into a timestamped history that tests read and that can be exported to JSON, and the state
    /// display and test buttons live in the scene builders' Inspectors.
    /// </summary>
    public class WhiteRoomHUD : MonoBehaviour
    {
        private readonly List<CombatLogEntry> _allHistoryLogs = new List<CombatLogEntry>();

        private readonly SimulatedNetworkChannel _networkChannel = new SimulatedNetworkChannel();

        public SimulatedNetworkChannel NetworkChannel => _networkChannel;
        public bool IsInteractionMode { get; set; }
        public string ActiveInteractionLabel { get; set; } = "握手 (Handshake)";
        public string CustomScenarioName { get; set; } = "";
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
        /// Appends a message to the exportable session log history.
        /// </summary>
        public void LogAction(string message)
        {
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
    }
}
