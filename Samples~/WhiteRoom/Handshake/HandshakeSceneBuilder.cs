using System;
using System.Collections.Generic;
using UnityEngine;
using Yu5h1Lib;
using Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom;

namespace Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom.Handshake
{
    /// <summary>
    /// Test scenarios the Inspector buttons can start by switching the AI test mode and resetting the arena.
    /// </summary>
    public enum HandshakeTestScenario
    {
        AiInitiatorSuccess,
        AiResponderSuccess,
        AiCancel,
        PlayerPrank,
        ReleaseAfterGrip,
        AiPrank,
        BothWithdraw,
        ContestAiWin,
        ContestPlayerWin,
        ContestTie,
        ContestZeroZero,
        PlayerCatchesAiEscape,
        AiCatchesPlayer
    }

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

        [Header("Participant Prefabs (Manual Scene Authoring)")]
        [SerializeField] private GameObject _playerPrefab;
        [SerializeField] private GameObject _aiPrefab;

        [Header("Handshake Settings Asset (Manual Authoring & Persistence)")]
        [SerializeField, Inline] private HandshakeSettings _settings;

        [Header("Scene Object References (Direct Scene Authoring)")]
        [SerializeField] private WhiteRoomCharacter _player;
        [SerializeField] private HandshakeAiController _ai;
        private WhiteRoomCamera _camera;

        private readonly List<GameObject> _spawnedEntities = new List<GameObject>();
        private HandshakeParticipant _playerParticipant;
        private HandshakeParticipant _aiParticipant;

        public WhiteRoomCharacter Player => _player;
        public HandshakeParticipant PlayerParticipant => _playerParticipant;
        public HandshakeAiController AI => _ai;
        public HandshakeParticipant AiParticipant => _aiParticipant;
        public WhiteRoomCamera Camera => _camera;
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
        }

        /// <summary>
        /// Configures the facing player and AI participants and binds the shared camera through <see cref="WhiteRoomSceneBuilder.EnsureCamera"/>. Uses scene prefab instances when present.
        /// </summary>
        public void SpawnParticipants()
        {
            if (_player == null) _player = ObjectUtility.FindObject<WhiteRoomCharacter>(false);
            if (_ai == null) _ai = ObjectUtility.FindObject<HandshakeAiController>(false);

            HandshakeVisualFeedback playerFeedback = null;
            HandshakeVisualFeedback aiFeedback = null;

            if (_player != null && _ai != null)
            {
                _playerParticipant = _player.GetComponent<HandshakeParticipant>();
                _aiParticipant = _ai.GetComponent<HandshakeParticipant>();
                playerFeedback = _player.GetComponentInChildren<HandshakeVisualFeedback>();
                aiFeedback = _ai.GetComponentInChildren<HandshakeVisualFeedback>();

                EnsureParticipantProxy(_playerParticipant, playerFeedback);
                EnsureParticipantProxy(_aiParticipant, aiFeedback);
            }
            else
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

                if (_playerPrefab != null && _aiPrefab != null)
                {
                    GameObject pGo = Instantiate(_playerPrefab, new Vector3(0f, 0f, -1.2f), Quaternion.identity);
                    _player = pGo.GetComponent<WhiteRoomCharacter>();
                    _playerParticipant = pGo.GetComponent<HandshakeParticipant>();
                    playerFeedback = pGo.GetComponentInChildren<HandshakeVisualFeedback>();
                    _spawnedEntities.Add(pGo);

                    GameObject aiGo = Instantiate(_aiPrefab, new Vector3(0f, 0f, 1.2f), Quaternion.Euler(0f, 180f, 0f));
                    _ai = aiGo.GetComponent<HandshakeAiController>();
                    _aiParticipant = aiGo.GetComponent<HandshakeParticipant>();
                    aiFeedback = aiGo.GetComponentInChildren<HandshakeVisualFeedback>();
                    _spawnedEntities.Add(aiGo);
                }
                else
                {
                    _player = SpawnPlayer(
                        "Player1",
                        0,
                        new Vector3(0f, 0f, -1.2f),
                        Quaternion.identity,
                        new Color(0.75f, 0.88f, 1.0f),
                        out _playerParticipant,
                        out playerFeedback
                    );

                    _ai = SpawnAI(
                        "AI_Opponent",
                        1,
                        new Vector3(0f, 0f, 1.2f),
                        Quaternion.Euler(0f, 180f, 0f),
                        new Color(0.96f, 0.78f, 0.78f),
                        out _aiParticipant,
                        out aiFeedback
                    );
                }
            }

            if (playerFeedback != null) playerFeedback.Opponent = _aiParticipant;
            if (aiFeedback != null) aiFeedback.Opponent = _playerParticipant;

            if (_player != null)
            {
                _camera = WhiteRoomSceneBuilder.EnsureCamera(_player.transform);
            }

            var arb = HandshakeArbitrator.Instance;
            if (arb != null)
            {
                if (_settings != null)
                {
                    arb.ConfigureSettings(_settings);
                }
                arb.RegisterParticipant(_playerParticipant);
                arb.RegisterParticipant(_aiParticipant);
            }
        }

        private void EnsureParticipantProxy(HandshakeParticipant participant, HandshakeVisualFeedback feedback)
        {
            if (participant == null) return;
            if (participant.ContactBridge == null)
            {
                participant.ContactBridge = participant.GetComponentInChildren<HandContactBridge>();
                if (participant.ContactBridge != null)
                {
                    participant.ContactBridge.Initialize(participant);
                }
            }
            if (participant.RightHandTransform == null)
            {
                Transform wrist = participant.transform.Find("HumanoidModel/root/pelvis/spine/chest/shoulder_r/elbow_r/wrist_r/fist_r");
                if (wrist == null) wrist = participant.transform.Find("HumanoidModel/root/pelvis/spine/chest/shoulder_r/elbow_r/wrist_r");
                if (wrist != null) participant.RightHandTransform = wrist;
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
            hud.RefreshBindings();

            hud.LogAction("【WhiteRoom 握手互動系統就緒 (第 3 批：比力氣與拉扯)】單一 HUD 運作，可使用 WASD 與互動鍵操作。");
        }

        /// <summary>
        /// Resets every arena entity, the same action as the R key.
        /// </summary>
        public void QuickReset()
        {
            CombatEntity.ResetAllArenaEntities();
        }

        /// <summary>
        /// Switches the AI test mode for the scenario, resets the arena and logs the player steps that reproduce the outcome.
        /// Called by the Inspector buttons of <c>HandshakeSceneBuilderEditor</c>.
        /// </summary>
        public void StartTestScenario(HandshakeTestScenario scenario)
        {
            if (_ai == null) return;
            var hud = ObjectUtility.FindObject<WhiteRoomHUD>(false);
            string guide;

            switch (scenario)
            {
                case HandshakeTestScenario.AiInitiatorSuccess:
                    _ai.Mode = HandshakeAiMode.AiInitiator;
                    guide = "【情境 1】AI 主動伸手等待，請按住 E 並以 WASD 走近使右手接觸至握手成功。";
                    break;
                case HandshakeTestScenario.AiResponderSuccess:
                    _ai.Mode = HandshakeAiMode.AiResponder;
                    _ai.WalkIn = true;
                    guide = "【情境 1b】請原地按住 E 伸手；AI 將主動走近並伸手回應，接觸後握手成功。";
                    break;
                case HandshakeTestScenario.AiCancel:
                    _ai.Mode = HandshakeAiMode.AiCancel;
                    guide = "【情境 1c】AI 伸手等待 1.2s 未碰到後主動放開收手取消。";
                    break;
                case HandshakeTestScenario.PlayerPrank:
                    _ai.Mode = HandshakeAiMode.AiInitiator;
                    guide = "【情境 2】走近接觸握緊後，請在 0.50s 內放開 E；判定為玩家惡作劇 AI。";
                    break;
                case HandshakeTestScenario.ReleaseAfterGrip:
                    _ai.Mode = HandshakeAiMode.AiReleaseAfterGrip;
                    guide = "【情境 2b】握緊成功進入握住後放開，記錄實際放開時間戳記。";
                    break;
                case HandshakeTestScenario.AiPrank:
                    _ai.Mode = HandshakeAiMode.AiEarlyRelease;
                    guide = "【情境 3】走近接觸握緊後，請持續按住 E；AI 將於握緊期間提前抽手，判定為 AI 惡作劇玩家。";
                    break;
                case HandshakeTestScenario.BothWithdraw:
                    _ai.Mode = HandshakeAiMode.AiEarlyRelease;
                    _ai.EarlyReleaseGripTime = 0.25f;
                    guide = "【情境 4】走近接觸握緊後，玩家與 AI 均在 0.50s 握緊結束前放開；判定為雙方同時收手。";
                    break;
                case HandshakeTestScenario.ContestAiWin:
                    _ai.Mode = HandshakeAiMode.ContestWin;
                    guide = "【情境 5】握住期間 AI 快速連按 5 次並放開；玩家不按或少按，判定 AI 比力氣獲勝。";
                    break;
                case HandshakeTestScenario.ContestPlayerWin:
                    _ai.Mode = HandshakeAiMode.ContestLose;
                    guide = "【情境 6】握住期間請快速連按 E 多次後放開；AI 不按，判定玩家比力氣獲勝。";
                    break;
                case HandshakeTestScenario.ContestTie:
                    _ai.Mode = HandshakeAiMode.ContestTie;
                    guide = "【情境 7】握住期間玩家與 AI 各按 2 次後放開；差距在門檻內，判定為平手慢退。";
                    break;
                case HandshakeTestScenario.ContestZeroZero:
                    _ai.Mode = HandshakeAiMode.ContestZeroZero;
                    guide = "【情境 8】握緊成功進入握住後，雙方皆不連按並直接放開；判定為 0 比 0 搖手收手。";
                    break;
                case HandshakeTestScenario.PlayerCatchesAiEscape:
                    _ai.Mode = HandshakeAiMode.AiEscape;
                    guide = "【情境 9】握住後 AI 放開 E，玩家持續按住 E 形成抓住；AI 往後彈跳拉開距離超過骨架門檻判定掙脫。";
                    break;
                case HandshakeTestScenario.AiCatchesPlayer:
                    _ai.Mode = HandshakeAiMode.AiCatchPlayer;
                    guide = "【情境 10】握住後玩家放開 E 超過 0.3s 被 AI 抓住；玩家再次按住 E 即回歸雙方握住（連按次數保留）。";
                    break;
                default:
                    return;
            }

            CombatEntity.ResetAllArenaEntities();
            hud?.LogAction(guide);
        }

        private static void SafeDestroy(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
    }
}
