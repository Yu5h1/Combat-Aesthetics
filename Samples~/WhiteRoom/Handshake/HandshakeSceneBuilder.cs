using System.Collections.Generic;
using UnityEngine;
using Yu5h1Lib;
using Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom;

namespace Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom.Handshake
{
    /// <summary>
    /// Bootstraps the Stage 2b handshake scene by reusing the shared <see cref="WhiteRoomSceneBuilder"/> environment,
    /// camera, and <see cref="WhiteRoomHUD"/>, and spawning the humanoid player and AI opponent.
    /// </summary>
    public class HandshakeSceneBuilder : MonoBehaviour
    {
        [SerializeField] private bool _autoSetupOnAwake = true;

        [Header("Humanoid Character & Clips (Imported via LIMB-ANIM-1)")]
        [SerializeField] private GameObject _characterModelPrefab;
        [SerializeField] private Avatar _humanoidAvatar;
        [SerializeField] private RuntimeAnimatorController _animatorController;
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
        public HandshakeAiController AiController => _ai;
        public HandshakeParticipant AiParticipant => _aiParticipant;

        private void Awake()
        {
            if (_autoSetupOnAwake)
            {
                SetupScene();
            }
        }

        /// <summary>
        /// Sets up the shared WhiteRoom environment, arbitrator, HUD, and humanoid participants.
        /// </summary>
        public void SetupScene()
        {
            WhiteRoomSceneBuilder.EnsureEnvironment();
            SetupArbitrator();
            SpawnParticipants();
            ConfigureSharedHUD();
        }

        private void SetupArbitrator()
        {
            HandshakeArbitrator arb = HandshakeArbitrator.Instance;
            if (arb == null)
            {
                GameObject arbGo = new GameObject("HandshakeArbitrator");
                arb = arbGo.AddComponent<HandshakeArbitrator>();
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
                    cam.SetOrbit(0f, 12f, 3.6f);
                    cam.SetShoulderOffset(new Vector3(-0.45f, 0f, 0f));
                }
            }

            var arb = HandshakeArbitrator.Instance;
            if (arb != null)
            {
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
            contactProxyGo.transform.SetParent(parent, false);
            if (handVisual != null)
            {
                contactProxyGo.transform.position = handVisual.position;
                contactProxyGo.transform.rotation = handVisual.rotation;
            }

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
            hud.CustomScenarioName = "Handshake_Stage2b_Batch2";
            hud.CustomStatusGUI = DrawHandshakeStatusGUI;
            hud.CustomScenarioGUI = DrawHandshakeScenarioGUI;
            hud.CustomHelpGUI = DrawHandshakeHelpGUI;
            hud.RefreshBindings();
            hud.LogAction("【WhiteRoom 握手互動系統就緒 (第 2 批)】可直接使用 WASD 與互動鍵操作，或調整左側 AI 設定。");
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
                else if (arb.LastOutcome == HandshakeOutcome.Cancelled)
                {
                    outcomeColor = "#ffaa44";
                    outcomeText = "取消 (Cancelled)";
                }

                GUILayout.Label($"<b>判定狀態:</b> <color=yellow>[{arb.SessionState}]</color> | <b>最新結果:</b> <color={outcomeColor}>[{outcomeText}]</color>");
                GUILayout.Label($"<b>片段時長:</b> 握緊 {arb.GripDuration:F2}s | 搖手 {arb.ShakeDuration:F2}s | 伸手 {arb.ReachDuration:F2}s | 收手 {arb.WithdrawDuration:F2}s");
                string reactionStatus = (arb.LaughClip != null && arb.AngryClip != null && arb.ClapLaughClip != null)
                    ? "已配置人形反應片段"
                    : "<color=grey>反應片段槽位留空 (動畫線第 2 批製作中，HUD 顯示並以收手回站姿)</color>";
                GUILayout.Label($"<b>反應動作:</b> {reactionStatus}");
            }

            if (_playerParticipant != null)
            {
                bool hold = _playerParticipant.CurrentIntent.InteractHold;
                string latch = _playerParticipant.RequiresInteractRelease ? " <color=orange>(需先放開再按)</color>" : "";
                GUILayout.Label($"<b>Player 1:</b> 姿態 [{_playerParticipant.State}] | 按住互動: {(hold ? "<color=green>YES</color>" : "NO")}{latch}");
            }

            if (_ai != null && _aiParticipant != null)
            {
                bool hold = _aiParticipant.CurrentIntent.InteractHold;
                string latch = _aiParticipant.RequiresInteractRelease ? " <color=orange>(需先放開再按)</color>" : "";
                GUILayout.Label($"<b>AI 對手:</b> 模式 [{_ai.Mode}] | 姿態 [{_aiParticipant.State}] | 按住互動: {(hold ? "<color=green>YES</color>" : "NO")}{latch}");
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
            if (GUILayout.Button("2. [玩家惡作劇 AI (玩家於握緊中途放開 E)]") && _ai != null)
            {
                _ai.Mode = HandshakeAiMode.AiInitiator;
                CombatEntity.ResetAllArenaEntities();
                hud?.LogAction("【情境 2】走近接觸握緊後，請在 0.50s 內放開 E；判定為玩家惡作劇 AI。");
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
            if (GUILayout.Button("5. [握緊放開 vs 握緊結束後放開 (惡作劇判定)]") && _ai != null)
            {
                _ai.Mode = HandshakeAiMode.AiReleaseAfterGrip;
                _ai.ReleaseAfterGripDelay = 0.15f;
                CombatEntity.ResetAllArenaEntities();
                hud?.LogAction("【情境 5】玩家於握緊中途放開，AI 堅持至握緊結束後才放開；判定嚴格維持為玩家惡作劇 AI。");
            }
            if (GUILayout.Button("6. [取消收手 (伸手後放開)]") && _ai != null)
            {
                _ai.Mode = HandshakeAiMode.AiCancel;
                CombatEntity.ResetAllArenaEntities();
                hud?.LogAction("【情境 6】伸手後於接觸前放開，收手取消回站姿。");
            }
        }

        private void DrawHandshakeHelpGUI()
        {
            GUILayout.Label("<b>操作與互動指南 (Stage 2b 第 2 批：惡作劇與同時收手)：</b>");
            GUILayout.Label("• <b>[互動鍵 (預設 E，可改鍵)]</b>：站立按住伸手 ➔ 等待循環；接觸前放開收手取消");
            GUILayout.Label("• <b>[握緊裁決規則]</b>：接觸進入握緊（0.50s）：雙方按住 ➔ 成功；單方放開 ➔ 惡作劇；雙方放開 ➔ 同時收手");
            GUILayout.Label("• <b>[放開的手立刻抽回]</b>：握緊期間放開者立刻播放收手抽手，判定持續至握緊結束裁定");
            GUILayout.Label("• <b>[重新按下才發起]</b>：成功、惡作劇、同時收手後若持續按著 E，須先放開再按下才再次伸手");
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
