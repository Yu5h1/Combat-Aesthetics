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
            hud.CustomScenarioName = "Handshake_Stage2b_Batch1";
            hud.CustomStatusGUI = DrawHandshakeStatusGUI;
            hud.CustomScenarioGUI = DrawHandshakeScenarioGUI;
            hud.CustomHelpGUI = DrawHandshakeHelpGUI;
            hud.RefreshBindings();
            hud.LogAction("【WhiteRoom 握手互動系統就緒】可直接使用 WASD 與互動鍵操作，或調整左側 AI 設定。");
        }

        private void DrawHandshakeStatusGUI()
        {
            var arb = HandshakeArbitrator.Instance;
            if (arb != null)
            {
                GUILayout.Label($"<b>判定狀態:</b> <color=yellow>[{arb.SessionState}]</color> | <b>最新結果:</b> <color=cyan>[{arb.LastOutcome}]</color>");
                GUILayout.Label($"<b>片段時長:</b> 握緊 {arb.GripDuration:F2}s | 搖手 {arb.ShakeDuration:F2}s | 伸手 {arb.ReachDuration:F2}s | 收手 {arb.WithdrawDuration:F2}s");
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

                _ai.WalkIn = GUILayout.Toggle(_ai.WalkIn, $"AI 主動走近玩家 (停止距離 {_ai.StopDistance:F2}m)");
            }

            GUILayout.Space(4);
            if (GUILayout.Button("1. [AI 發起 ➔ 玩家走近回應 (成功)]") && _ai != null)
            {
                _ai.Mode = HandshakeAiMode.AiInitiator;
                CombatEntity.ResetAllArenaEntities();
                hud?.LogAction("【情境 1】AI 主動伸手等待，請按住 E 並以 WASD 走近使右手接觸。");
            }
            if (GUILayout.Button("2. [玩家發起 ➔ AI 走近回應 (對調)]") && _ai != null)
            {
                _ai.Mode = HandshakeAiMode.AiResponder;
                CombatEntity.ResetAllArenaEntities();
                hud?.LogAction("【情境 2】請原地按住 E 伸手，AI 將伸手並主動走近回應。");
            }
            if (GUILayout.Button("3. [玩家發起 ➔ 放開取消 / AI 取消]") && _ai != null)
            {
                _ai.Mode = HandshakeAiMode.AiCancel;
                CombatEntity.ResetAllArenaEntities();
                hud?.LogAction("【情境 3】AI 伸手後按設定時長放開取消；玩家亦可隨時按 E 再放開取消。");
            }
            if (GUILayout.Button("4. [握緊期間放開 (第 2 批的情況)]") && _ai != null)
            {
                _ai.Mode = HandshakeAiMode.AiEarlyRelease;
                CombatEntity.ResetAllArenaEntities();
                hud?.LogAction("【情境 4】雙手接觸進入握緊後 AI 提前放開，雙方直接收手並標明第 2 批的情況。");
            }
        }

        private void DrawHandshakeHelpGUI()
        {
            GUILayout.Label("<b>操作與互動指南 (Stage 2b 第 1 批)：</b>");
            GUILayout.Label("• <b>[互動鍵 (預設 E，可改鍵)]</b>：站立按住伸手 ➔ 等待循環；放開收手取消");
            GUILayout.Label("• <b>[重新按下才發起]</b>：完成或收手回站姿時若仍按著，須先放開再按下才再次伸手");
            GUILayout.Label("• <b>[WASD / Space]</b>：8 方向移動（伸手時慢步 2.2m/s 走近）與跳躍");
            GUILayout.Label("• <b>[右手碰撞體接觸]</b>：雙方右手 Trigger 實際接觸才算碰到 ➔ 握緊 ➔ 成功搖一下");
            GUILayout.Label("• <b>[R 鍵]</b>：全場快速重設（共用 CombatEntity.ResetAllArenaEntities）");
        }

        private static void SafeDestroy(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
    }
}
