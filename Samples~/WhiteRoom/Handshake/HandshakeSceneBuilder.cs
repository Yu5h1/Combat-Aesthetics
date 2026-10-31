using System;
using System.Collections.Generic;
using UnityEngine;
using Yu5h1Lib;
using Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom;

namespace Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom.Handshake
{
    /// <summary>
    /// Builder component for the Stage 2b handshake test scene across Batches 1, 2, and 3.
    /// Sets up the facing player and AI entities, right-hand contact colliders, arbitrator, and shared HUD.
    /// </summary>
    public class HandshakeSceneBuilder : MonoBehaviour
    {
        [Header("Scene Configuration")]
        [SerializeField] private bool _autoSetupOnAwake = true;

        [Header("Character Model and Animation Assets")]
        [SerializeField] private GameObject _characterModelPrefab;
        [SerializeField] private Avatar _humanoidAvatar;
        [SerializeField] private RuntimeAnimatorController _animatorController;

        [Header("Handshake Settings Asset (Manual Authoring & Persistence)")]
        [SerializeField, Inline] private HandshakeSettings _settings;

        [Header("Handshake Animation Clips")]
        [SerializeField] private AnimationClip _reachClip;
        [SerializeField] private AnimationClip _waitClip;
        [SerializeField] private AnimationClip _withdrawClip;
        [SerializeField] private AnimationClip _gripClip;
        [SerializeField] private AnimationClip _shakeClip;

        private readonly List<GameObject> _spawnedEntities = new List<GameObject>();
        private WhiteRoomCharacter _player;
        private HandshakeParticipant _playerParticipant;
        private HandshakeAiController _ai;
        private HandshakeParticipant _aiParticipant;

        public WhiteRoomCharacter Player => _player;
        public HandshakeParticipant PlayerParticipant => _playerParticipant;
        public HandshakeAiController AI => _ai;
        public HandshakeParticipant AiParticipant => _aiParticipant;
        public HandshakeSettings Settings => _settings;

        private void Awake()
        {
            if (_autoSetupOnAwake)
            {
                SetupHandshakeScene();
            }
        }

        /// <summary>
        /// Executes full initialization of the handshake scene, floor, participants, arbitrator, and HUD.
        /// </summary>
        public void SetupHandshakeScene()
        {
            EnsureArbitrator();
            SpawnParticipants();
            ConfigureSharedHUD();
        }

        private void EnsureArbitrator()
        {
            HandshakeArbitrator arb = HandshakeArbitrator.Instance;
            if (arb == null)
            {
                GameObject arbGo = new GameObject("HandshakeArbitrator");
                arb = arbGo.AddComponent<HandshakeArbitrator>();
            }
            if (_settings != null)
            {
                arb.ConfigureSettings(_settings);
            }
            arb.ConfigureClips(_reachClip, _waitClip, _withdrawClip, _gripClip, _shakeClip);
        }

        /// <summary>
        /// Spawns the facing player and AI participants at 2.4m separation and binds their humanoid animators and IK.
        /// </summary>
        public void SpawnParticipants()
        {
            foreach (var go in _spawnedEntities)
            {
                if (go != null) SafeDestroy(go);
            }
            _spawnedEntities.Clear();

            var oldParticipants = ObjectUtility.FindObjects<HandshakeParticipant>(false);
            foreach (var p in oldParticipants)
            {
                if (p != null && p.gameObject != null)
                {
                    SafeDestroy(p.gameObject);
                }
            }

            _player = SpawnPlayer(
                "Player1",
                0,
                new Vector3(0f, 0f, -1.2f),
                Quaternion.identity,
                new Color(0.75f, 0.88f, 1.0f),
                out _playerParticipant,
                out HandshakeVisualFeedback playerFeedback
            );

            _ai = SpawnAI(
                "AI_Opponent",
                1,
                new Vector3(0f, 0f, 1.2f),
                Quaternion.Euler(0f, 180f, 0f),
                new Color(0.96f, 0.78f, 0.78f),
                out _aiParticipant,
                out HandshakeVisualFeedback aiFeedback
            );

            if (playerFeedback != null) playerFeedback.Opponent = _aiParticipant;
            if (aiFeedback != null) aiFeedback.Opponent = _playerParticipant;

            if (_player != null)
            {
                var cam = WhiteRoomSceneBuilder.EnsureCamera(_player.transform);
                if (cam != null)
                {
                    cam.SetOrbit(0f, 12f, 3.4f);
                    cam.SetShoulderOffset(new Vector3(-0.45f, 0f, 0f));
                }
            }

            var arb = HandshakeArbitrator.Instance;
            if (arb != null)
            {
                if (_settings != null)
                {
                    arb.ConfigureSettings(_settings);
                }
                arb.ConfigureClips(_reachClip, _waitClip, _withdrawClip, _gripClip, _shakeClip);
                arb.RegisterParticipant(_playerParticipant);
                arb.RegisterParticipant(_aiParticipant);
            }
        }

        private WhiteRoomCharacter SpawnPlayer(
            string id,
            int team,
            Vector3 position,
            Quaternion rotation,
            Color bodyTint,
            out HandshakeParticipant participant,
            out HandshakeVisualFeedback feedback)
        {
            GameObject playerGo = new GameObject(id);
            playerGo.transform.position = position;
            playerGo.transform.rotation = rotation;

            CombatEntity entity = playerGo.AddComponent<CombatEntity>();
            entity.InitializeSettings(id, team, 100f);
            entity.EnableCombatActions = false;

            participant = playerGo.AddComponent<HandshakeParticipant>();
            participant.Initialize(id, team);

            WhiteRoomCharacter playerChar = playerGo.AddComponent<WhiteRoomCharacter>();
            playerChar.SetControlScheme(false);

            feedback = AttachHumanoidModel(playerGo.transform, participant, bodyTint);

            _spawnedEntities.Add(playerGo);
            return playerChar;
        }

        private HandshakeAiController SpawnAI(
            string id,
            int team,
            Vector3 position,
            Quaternion rotation,
            Color bodyTint,
            out HandshakeParticipant participant,
            out HandshakeVisualFeedback feedback)
        {
            GameObject aiGo = new GameObject(id);
            aiGo.transform.position = position;
            aiGo.transform.rotation = rotation;

            CombatEntity entity = aiGo.AddComponent<CombatEntity>();
            entity.InitializeSettings(id, team, 100f);
            entity.EnableCombatActions = false;

            participant = aiGo.AddComponent<HandshakeParticipant>();
            participant.Initialize(id, team);

            HandshakeAiController aiCtrl = aiGo.AddComponent<HandshakeAiController>();
            aiCtrl.Mode = HandshakeAiMode.AiInitiator;

            feedback = AttachHumanoidModel(aiGo.transform, participant, bodyTint);

            _spawnedEntities.Add(aiGo);
            return aiCtrl;
        }

        private HandshakeVisualFeedback AttachHumanoidModel(Transform parent, HandshakeParticipant participant, Color bodyTint)
        {
            if (_characterModelPrefab == null) return null;

            GameObject modelInstance = Instantiate(_characterModelPrefab, parent, false);
            modelInstance.name = "HumanoidModel";
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;

            Animator animator = modelInstance.GetComponent<Animator>();
            if (animator == null)
            {
                animator = modelInstance.AddComponent<Animator>();
            }
            animator.avatar = _humanoidAvatar;
            animator.runtimeAnimatorController = _animatorController;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            Transform wristR = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (wristR == null)
            {
                wristR = modelInstance.transform.Find("root/pelvis/spine/chest/shoulder_r/elbow_r/wrist_r");
            }
            Transform fistR = modelInstance.transform.Find("root/pelvis/spine/chest/shoulder_r/elbow_r/wrist_r/fist_r");
            Transform handVisual = fistR != null ? fistR : wristR;

            GameObject contactProxyGo = new GameObject("RightHandContactCollider");
            if (wristR != null)
            {
                contactProxyGo.transform.position = wristR.TransformPoint(new Vector3(0.08f, 0f, 0f));
                contactProxyGo.transform.rotation = wristR.rotation;
            }
            contactProxyGo.transform.SetParent(parent, true);

            participant.SetRightHand(handVisual, contactProxyGo.transform);

            Renderer skinnedRenderer = modelInstance.GetComponentInChildren<SkinnedMeshRenderer>(true);

            HandshakeVisualFeedback feedback = modelInstance.AddComponent<HandshakeVisualFeedback>();
            feedback.BindHumanoid(participant, animator, wristR, contactProxyGo.transform, skinnedRenderer, bodyTint);
            return feedback;
        }

        private void ConfigureSharedHUD()
        {
            WhiteRoomHUD hud = WhiteRoomSceneBuilder.EnsureHUD();
            hud.IsInteractionMode = true;
            hud.ActiveInteractionLabel = "握手 (Handshake)";
            hud.CustomScenarioName = "Handshake_Stage2b_Batch3";
            hud.CustomStatusGUI = DrawHandshakeStatusGUI;
            hud.CustomScenarioGUI = DrawHandshakeScenarioGUI;
            hud.CustomHelpGUI = DrawHandshakeHelpGUI;
            hud.RefreshBindings();

            hud.LogAction("【WhiteRoom 握手互動系統就緒 (第 3 批：比力氣與拉扯)】單一 HUD 運作，可使用 WASD 與互動鍵操作。");
        }

        private void DrawHandshakeStatusGUI()
        {
            var arb = HandshakeArbitrator.Instance;
            if (arb != null)
            {
                string outcomeColor = "cyan";
                string outcomeText = arb.LastOutcome.ToString();
                if (arb.LastOutcome == HandshakeOutcome.Success)
                {
                    outcomeColor = "#44ff44";
                    outcomeText = "握手成功 (Success)";
                }
                else if (arb.LastOutcome == HandshakeOutcome.Prank)
                {
                    outcomeColor = "#ff5555";
                    string prankerName = arb.LastPranker != null ? arb.LastPranker.EntityId : "未知";
                    outcomeText = $"惡作劇 (Prank) - 放開者: {prankerName}";
                }
                else if (arb.LastOutcome == HandshakeOutcome.BothWithdraw)
                {
                    outcomeColor = "#ffff44";
                    outcomeText = "同時收手 (BothWithdraw) - 雙方皆放開";
                }
                else if (arb.LastOutcome == HandshakeOutcome.StrengthContestWin)
                {
                    outcomeColor = "#44ff44";
                    string winner = arb.LastContestWinner != null ? arb.LastContestWinner.EntityId : "平手";
                    outcomeText = $"比力氣勝負 (Win) - 勝方: {winner} (P1:{arb.LastPlayerTaps} vs AI:{arb.LastAiTaps})";
                }
                else if (arb.LastOutcome == HandshakeOutcome.StrengthContestTie)
                {
                    outcomeColor = "#88ccff";
                    outcomeText = $"比力氣門檻內 (Tie) (P1:{arb.LastPlayerTaps} vs AI:{arb.LastAiTaps})";
                }
                else if (arb.LastOutcome == HandshakeOutcome.StrengthContestZeroZero)
                {
                    outcomeColor = "#ffff88";
                    outcomeText = "比力氣 0 比 0 (搖手收手)";
                }
                else if (arb.LastOutcome == HandshakeOutcome.Caught)
                {
                    outcomeColor = "#ff8844";
                    string catcher = arb.LastCatcher != null ? arb.LastCatcher.EntityId : "無";
                    outcomeText = $"抓住 (Caught) - 抓人者: {catcher}";
                }
                else if (arb.LastOutcome == HandshakeOutcome.Escaped)
                {
                    outcomeColor = "#44ffff";
                    string escaper = arb.LastEscaper != null ? arb.LastEscaper.EntityId : "無";
                    outcomeText = $"掙脫 (Escaped) - 掙脫者: {escaper}";
                }
                else if (arb.LastOutcome == HandshakeOutcome.Cancelled)
                {
                    outcomeColor = "#ffaa44";
                    outcomeText = "取消 (Cancelled)";
                }

                GUILayout.Label($"<b>判定狀態:</b> <color=yellow>[{arb.SessionState}]</color> | <b>最新結果:</b> <color={outcomeColor}>[{outcomeText}]</color>");
                GUILayout.Label($"<b>比力氣次數:</b> P1: {arb.PlayerTaps} 次 | AI: {arb.AiTaps} 次 | <b>身體相距:</b> {arb.BodyDistance:F2} m");
                GUILayout.Label($"<b>片段時長:</b> 握緊 {arb.GripDuration:F2}s | 搖手 {arb.ShakeDuration:F2}s | 伸手 {arb.ReachDuration:F2}s | 收手 {arb.WithdrawDuration:F2}s");

                GUILayout.BeginHorizontal();
                GUILayout.Label($"連按容許時間: {arb.ReleaseGraceTime:F2}s", GUILayout.Width(135));
                arb.ReleaseGraceTime = GUILayout.HorizontalSlider(arb.ReleaseGraceTime, 0.1f, 1.0f);
                GUILayout.EndHorizontal();

                float currentThreshold = arb.GetEffectiveEscapeThreshold();
                GUILayout.BeginHorizontal();
                GUILayout.Label($"掙脫骨架門檻: {currentThreshold:F2}m", GUILayout.Width(135));
                arb.EscapeDistanceThreshold = GUILayout.HorizontalSlider(arb.EscapeDistanceThreshold, 1.0f, 3.0f);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label($"不可控制區間: {arb.Settings.PrankReactionDuration:F2}s", GUILayout.Width(135));
                arb.Settings.PrankReactionDuration = GUILayout.HorizontalSlider(arb.Settings.PrankReactionDuration, 0.2f, 2.0f);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label($"被抓走路速度: {arb.Settings.CaughtWalkSpeed:F2}m/s", GUILayout.Width(135));
                arb.Settings.CaughtWalkSpeed = GUILayout.HorizontalSlider(arb.Settings.CaughtWalkSpeed, 1.0f, 4.0f);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label($"抓者跟隨速度: {arb.Settings.CatchingFollowSpeed:F2}m/s", GUILayout.Width(135));
                arb.Settings.CatchingFollowSpeed = GUILayout.HorizontalSlider(arb.Settings.CatchingFollowSpeed, 0.5f, 2.5f);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label($"跳躍彈退位移: {arb.Settings.JumpEscapeDisplacement:F2}m", GUILayout.Width(135));
                arb.Settings.JumpEscapeDisplacement = GUILayout.HorizontalSlider(arb.Settings.JumpEscapeDisplacement, 0.2f, 1.5f);
                GUILayout.EndHorizontal();
            }

            if (_playerParticipant != null)
            {
                bool hold = _playerParticipant.CurrentIntent.InteractHold;
                string latch = _playerParticipant.RequiresInteractRelease ? " <color=orange>(需先放開再按)</color>" : "";
                GUILayout.Label($"<b>Player 1:</b> 姿態 [{_playerParticipant.State}] | 按住互動: {(hold ? "<color=green>YES</color>" : "NO")}{latch} | 按下次數: {_playerParticipant.InteractPressCount}");
            }

            if (_ai != null && _aiParticipant != null)
            {
                bool hold = _aiParticipant.CurrentIntent.InteractHold;
                string latch = _aiParticipant.RequiresInteractRelease ? " <color=orange>(需先放開再按)</color>" : "";
                GUILayout.Label($"<b>AI 對手:</b> 模式 [{_ai.Mode}] | 姿態 [{_aiParticipant.State}] | 按住互動: {(hold ? "<color=green>YES</color>" : "NO")}{latch} | 按下次數: {_aiParticipant.InteractPressCount}");
            }
        }

        private void DrawHandshakeScenarioGUI()
        {
            var hud = ObjectUtility.FindObject<WhiteRoomHUD>(false);
            GUILayout.Label("<b>情境與 AI 可配置測試設定：</b>");

            if (_ai != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"AI 伸手延遲: {_ai.ReachDelay:F2}s", GUILayout.Width(135));
                _ai.ReachDelay = GUILayout.HorizontalSlider(_ai.ReachDelay, 0f, 3.0f);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                string holdLabel = _ai.HoldDuration <= 0f ? "AI 按住時長: 持續按住" : $"AI 按住時長: {_ai.HoldDuration:F2}s";
                GUILayout.Label(holdLabel, GUILayout.Width(135));
                _ai.HoldDuration = GUILayout.HorizontalSlider(_ai.HoldDuration, -1f, 3.0f);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label($"AI 握緊放開: {_ai.EarlyReleaseGripTime:F2}s", GUILayout.Width(135));
                _ai.EarlyReleaseGripTime = GUILayout.HorizontalSlider(_ai.EarlyReleaseGripTime, 0.05f, 0.45f);
                GUILayout.EndHorizontal();

                _ai.WalkIn = GUILayout.Toggle(_ai.WalkIn, $"AI 主動走近玩家 (停止距離 {_ai.StopDistance:F2}m)");
            }

            GUILayout.Space(4);
            if (GUILayout.Button("1. [AI 發起 ➔ 玩家走近 ➔ 握手成功]") && _ai != null)
            {
                _ai.Mode = HandshakeAiMode.AiInitiator;
                CombatEntity.ResetAllArenaEntities();
                hud?.LogAction("【情境 1】AI 主動伸手等待，請按住 E 並以 WASD 走近使右手接觸至握手成功。");
            }
            if (GUILayout.Button("1b. [回歸: 玩家發起 ➔ AI 走近回應 ➔ 握手成功]") && _ai != null)
            {
                _ai.Mode = HandshakeAiMode.AiResponder;
                _ai.WalkIn = true;
                CombatEntity.ResetAllArenaEntities();
                hud?.LogAction("【情境 1b】請原地按住 E 伸手；AI 將主動走近並伸手回應，接觸後握手成功。");
            }
            if (GUILayout.Button("1c. [AI 發起後取消 (伸手未碰到即放開)]") && _ai != null)
            {
                _ai.Mode = HandshakeAiMode.AiCancel;
                CombatEntity.ResetAllArenaEntities();
                hud?.LogAction("【情境 1c】AI 伸手等待 1.2s 未碰到後主動放開收手取消。");
            }
            if (GUILayout.Button("2. [玩家惡作劇 AI (玩家於握緊中途放開 E)]") && _ai != null)
            {
                _ai.Mode = HandshakeAiMode.AiInitiator;
                CombatEntity.ResetAllArenaEntities();
                hud?.LogAction("【情境 2】走近接觸握緊後，請在 0.50s 內放開 E；判定為玩家惡作劇 AI。");
            }
            if (GUILayout.Button("2b. [握緊結束後才放開 (握緊成功進入握住後放開)]") && _ai != null)
            {
                _ai.Mode = HandshakeAiMode.AiReleaseAfterGrip;
                CombatEntity.ResetAllArenaEntities();
                hud?.LogAction("【情境 2b】握緊成功進入握住後放開，記錄實際放開時間戳記。");
            }
            if (GUILayout.Button("3. [AI 惡作劇玩家 (AI 於握緊中途放開)]") && _ai != null)
            {
                _ai.Mode = HandshakeAiMode.AiEarlyRelease;
                CombatEntity.ResetAllArenaEntities();
                hud?.LogAction("【情境 3】走近接觸握緊後，請持續按住 E；AI 將於握緊期間提前抽手，判定為 AI 惡作劇玩家。");
            }
            if (GUILayout.Button("4. [雙方同時收手 (雙方均在握緊結束前放開)]") && _ai != null)
            {
                _ai.Mode = HandshakeAiMode.AiEarlyRelease;
                _ai.EarlyReleaseGripTime = 0.25f;
                CombatEntity.ResetAllArenaEntities();
                hud?.LogAction("【情境 4】走近接觸握緊後，玩家與 AI 均在 0.50s 握緊結束前放開；判定為雙方同時收手。");
            }
            if (GUILayout.Button("5. [比力氣: AI 連按獲勝 (AI 勝)]") && _ai != null)
            {
                _ai.Mode = HandshakeAiMode.ContestWin;
                CombatEntity.ResetAllArenaEntities();
                hud?.LogAction("【情境 5】握住期間 AI 快速連按 5 次並放開；玩家不按或少按，判定 AI 比力氣獲勝。");
            }
            if (GUILayout.Button("6. [比力氣: 玩家連按獲勝 (玩家勝)]") && _ai != null)
            {
                _ai.Mode = HandshakeAiMode.ContestLose;
                CombatEntity.ResetAllArenaEntities();
                hud?.LogAction("【情境 6】握住期間請快速連按 E 多次後放開；AI 不按，判定玩家比力氣獲勝。");
            }
            if (GUILayout.Button("7. [比力氣: 差距在門檻內 (平手/慢退)]") && _ai != null)
            {
                _ai.Mode = HandshakeAiMode.ContestTie;
                CombatEntity.ResetAllArenaEntities();
                hud?.LogAction("【情境 7】握住期間玩家與 AI 各按 2 次後放開；差距在門檻內，判定為平手慢退。");
            }
            if (GUILayout.Button("8. [比力氣: 0 比 0 (雙方皆未連按)]") && _ai != null)
            {
                _ai.Mode = HandshakeAiMode.ContestZeroZero;
                CombatEntity.ResetAllArenaEntities();
                hud?.LogAction("【情境 8】握緊成功進入握住後，雙方皆不連按並直接放開；判定為 0 比 0 搖手收手。");
            }
            if (GUILayout.Button("9. [抓住與掙脫: 玩家抓 AI ➔ AI 彈跳掙脫]") && _ai != null)
            {
                _ai.Mode = HandshakeAiMode.AiEscape;
                CombatEntity.ResetAllArenaEntities();
                hud?.LogAction("【情境 9】握住後 AI 放開 E，玩家持續按住 E 形成抓住；AI 往後彈跳拉開距離超過骨架門檻判定掙脫。");
            }
            if (GUILayout.Button("10. [抓住與掙脫: AI 抓玩家 ➔ 玩家再按住回握住]") && _ai != null)
            {
                _ai.Mode = HandshakeAiMode.AiCatchPlayer;
                CombatEntity.ResetAllArenaEntities();
                hud?.LogAction("【情境 10】握住後玩家放開 E 超過 0.3s 被 AI 抓住；玩家再次按住 E 即回歸雙方握住（連按次數保留）。");
            }
        }

        private void DrawHandshakeHelpGUI()
        {
            GUILayout.Label("<b>操作與互動指南 (Stage 2b 第 3 批：比力氣與拉扯)：</b>");
            GUILayout.Label("• <b>[互動鍵 (預設 E，可改鍵)]</b>：站立按住伸手 ➔ 等待循環；接觸進入握緊（0.50s）");
            GUILayout.Label("• <b>[握住與比力氣]</b>：握緊後進入握住，維持緊握姿勢；連按 E 計數；放開容許時間 0.30s，雙方放開時裁決勝負/門檻內/0比0");
            GUILayout.Label("• <b>[抓住與掙脫]</b>：一方放開逾時、另一方按住 ➔ 形成抓住；被抓方正常步速退/可跳躍彈退，抓方慢步跟；身體距離 > 骨架門檻判定掙脫；被抓方再按住回握住（次數累計）；抓方亦放開直接裁決比力氣");
            GUILayout.Label("• <b>[重新按下才發起]</b>：所有正式結果裁決後若持續按住 E，須先放開再按下才再次伸手");
            GUILayout.Label("• <b>[WASD / Space / R]</b>：移動、跳躍與全場快速重設（共用 CombatEntity）");
        }

        private static void SafeDestroy(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
    }
}
