using System.Collections.Generic;
using UnityEngine;
using Yu5h1Lib;

namespace Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom
{
    /// <summary>
    /// Predefined combat arena scenarios for the WhiteRoom sample.
    /// </summary>
    public enum ArenaScenario
    {
        OnePlayerVsOneAI = 0,
        OnePlayerVsTwoAI = 1,
        TwoPlayerVsTwoAI = 2,
        TwoPlayerPvP = 3
    }

    /// <summary>
    /// Builds and manages the shared WhiteRoom environment, lighting, third-person camera, HUD, and combat entities.
    /// </summary>
    public class WhiteRoomSceneBuilder : MonoBehaviour
    {
        [SerializeField] private bool _autoSetupOnAwake = true;
        [SerializeField] private ArenaScenario _initialScenario = ArenaScenario.OnePlayerVsOneAI;

        private ArenaScenario _currentScenario = ArenaScenario.OnePlayerVsOneAI;
        public ArenaScenario CurrentScenario => _currentScenario;

        private readonly List<GameObject> _spawnedEntities = new List<GameObject>();

        private void Awake()
        {
            if (_autoSetupOnAwake)
            {
                SetupEnvironment();
                SetupHUD();
                ApplyScenario(_initialScenario);
            }
        }

        /// <summary>
        /// Ensures the 50m x 50m white floor and directional light exist in the current scene.
        /// </summary>
        public void SetupEnvironment()
        {
            EnsureEnvironment();
        }

        /// <summary>
        /// Static helper that creates the shared WhiteRoom floor and directional light if not already present.
        /// </summary>
        public static void EnsureEnvironment()
        {
            if (GameObject.Find("WhiteRoom_Floor") == null)
            {
                GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "WhiteRoom_Floor";
                floor.transform.position = Vector3.zero;
                floor.transform.localScale = new Vector3(5f, 1f, 5f);

                Material floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Diffuse"));
                floorMat.color = new Color(0.88f, 0.90f, 0.92f);
                floor.GetComponent<Renderer>().sharedMaterial = floorMat;
            }

            if (ObjectUtility.FindObject<Light>(false) == null)
            {
                GameObject lightGo = new GameObject("Directional Light");
                Light light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.2f;
                light.color = new Color(1f, 0.98f, 0.95f);
                lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }
        }

        /// <summary>
        /// Ensures the shared <see cref="WhiteRoomHUD"/> exists in the current scene.
        /// </summary>
        public WhiteRoomHUD SetupHUD()
        {
            return EnsureHUD();
        }

        /// <summary>
        /// Static helper that locates or creates the shared <see cref="WhiteRoomHUD"/>.
        /// </summary>
        public static WhiteRoomHUD EnsureHUD()
        {
            WhiteRoomHUD hud = ObjectUtility.FindObject<WhiteRoomHUD>(false);
            if (hud == null)
            {
                GameObject hudGo = new GameObject("WhiteRoomHUD");
                hud = hudGo.AddComponent<WhiteRoomHUD>();
            }
            return hud;
        }

        /// <summary>
        /// Static helper that attaches or updates the shared <see cref="WhiteRoomCamera"/> to follow the target transform.
        /// </summary>
        public static WhiteRoomCamera EnsureCamera(Transform targetTransform)
        {
            if (targetTransform == null) return null;

            WhiteRoomCamera cam = ObjectUtility.FindObject<WhiteRoomCamera>(false);
            if (cam == null)
            {
                Camera mainCam = Camera.main;
                GameObject camGo = mainCam != null ? mainCam.gameObject : new GameObject("Main Camera", typeof(Camera));
                if (mainCam == null)
                {
                    camGo.tag = "MainCamera";
                    camGo.AddComponent<AudioListener>();
                }
                cam = camGo.AddComponent<WhiteRoomCamera>();
            }
            cam.Target = targetTransform;
            return cam;
        }

        /// <summary>
        /// Spawns the entities for the specified <see cref="ArenaScenario"/> and binds the camera and HUD.
        /// </summary>
        public void ApplyScenario(ArenaScenario scenario)
        {
            _currentScenario = scenario;

            foreach (var go in _spawnedEntities)
            {
                if (go != null) SafeDestroy(go);
            }
            _spawnedEntities.Clear();

            var oldEntities = ObjectUtility.FindObjects<CombatEntity>(false);
            foreach (var ent in oldEntities)
            {
                if (ent != null && ent.gameObject != null)
                {
                    SafeDestroy(ent.gameObject);
                }
            }

            WhiteRoomCharacter p1 = null;

            switch (scenario)
            {
                case ArenaScenario.OnePlayerVsOneAI:
                    p1 = SpawnPlayer("Player1", 0, new Vector3(0, 0, -3.5f), Quaternion.identity, false, new Color(0.1f, 0.7f, 1.0f));
                    SpawnAI("AI_Dummy1", 1, new Vector3(0, 0, 3.5f), Quaternion.Euler(0, 180f, 0), new Color(1f, 0.25f, 0.25f), new Color(0.35f, 0.38f, 0.42f));
                    break;

                case ArenaScenario.OnePlayerVsTwoAI:
                    p1 = SpawnPlayer("Player1", 0, new Vector3(0, 0, -3.5f), Quaternion.identity, false, new Color(0.1f, 0.7f, 1.0f));
                    SpawnAI("AI_Dummy1", 1, new Vector3(-2.2f, 0, 3.5f), Quaternion.Euler(0, 180f, 0), new Color(1f, 0.25f, 0.25f), new Color(0.35f, 0.38f, 0.42f));
                    SpawnAI("AI_Dummy2", 1, new Vector3(2.2f, 0, 3.5f), Quaternion.Euler(0, 180f, 0), new Color(1f, 0.55f, 0.1f), new Color(0.28f, 0.30f, 0.35f));
                    break;

                case ArenaScenario.TwoPlayerVsTwoAI:
                    p1 = SpawnPlayer("Player1", 0, new Vector3(-1.8f, 0, -3.5f), Quaternion.identity, false, new Color(0.1f, 0.7f, 1.0f));
                    SpawnPlayer("Player2", 0, new Vector3(1.8f, 0, -3.5f), Quaternion.identity, true, new Color(0.2f, 0.9f, 0.4f));
                    SpawnAI("AI_Dummy1", 1, new Vector3(-1.8f, 0, 3.5f), Quaternion.Euler(0, 180f, 0), new Color(1f, 0.25f, 0.25f), new Color(0.35f, 0.38f, 0.42f));
                    SpawnAI("AI_Dummy2", 1, new Vector3(1.8f, 0, 3.5f), Quaternion.Euler(0, 180f, 0), new Color(1f, 0.55f, 0.1f), new Color(0.28f, 0.30f, 0.35f));
                    break;

                case ArenaScenario.TwoPlayerPvP:
                    p1 = SpawnPlayer("Player1", 0, new Vector3(0, 0, -3.5f), Quaternion.identity, false, new Color(0.1f, 0.7f, 1.0f));
                    SpawnPlayer("Player2", 1, new Vector3(0, 0, 3.5f), Quaternion.Euler(0, 180f, 0), true, new Color(1f, 0.3f, 0.3f));
                    break;
            }

            if (p1 != null)
            {
                EnsureCamera(p1.transform);
            }

            WhiteRoomHUD hud = ObjectUtility.FindObject<WhiteRoomHUD>(false);
            if (hud != null)
            {
                hud.RefreshBindings();
            }
        }

        private WhiteRoomCharacter SpawnPlayer(string id, int team, Vector3 position, Quaternion rotation, bool isPlayer2, Color visorColor)
        {
            GameObject playerGo = new GameObject(id);
            playerGo.transform.position = position;
            playerGo.transform.rotation = rotation;

            CombatEntity entity = playerGo.AddComponent<CombatEntity>();
            entity.InitializeSettings(id, team, 100f);

            WhiteRoomCharacter player = playerGo.AddComponent<WhiteRoomCharacter>();
            player.SetControlScheme(isPlayer2);

            GameObject visuals = WhiteRoomVisuals.CreateRobotModel(
                playerGo.transform,
                out Transform fist,
                out Renderer visor,
                out Renderer torso
            );

            visor.material.color = visorColor;

            entity.VisualRoot = visuals.transform;
            entity.FistTransform = fist;

            WhiteRoomVisualFeedback feedback = playerGo.AddComponent<WhiteRoomVisualFeedback>();
            feedback.BindVisuals(visuals.transform, fist, visuals.transform.Find("Head"), visor, torso);

            _spawnedEntities.Add(playerGo);
            return player;
        }

        private WhiteRoomDummy SpawnAI(string id, int team, Vector3 position, Quaternion rotation, Color visorColor, Color torsoColor)
        {
            GameObject dummyGo = new GameObject(id);
            dummyGo.transform.position = position;
            dummyGo.transform.rotation = rotation;

            CombatEntity entity = dummyGo.AddComponent<CombatEntity>();
            entity.InitializeSettings(id, team, 100f);

            WhiteRoomDummy dummy = dummyGo.AddComponent<WhiteRoomDummy>();

            GameObject visuals = WhiteRoomVisuals.CreateRobotModel(
                dummyGo.transform,
                out Transform fist,
                out Renderer visor,
                out Renderer torso
            );

            torso.material.color = torsoColor;
            visor.material.color = visorColor;

            entity.VisualRoot = visuals.transform;
            entity.FistTransform = fist;

            WhiteRoomVisualFeedback feedback = dummyGo.AddComponent<WhiteRoomVisualFeedback>();
            feedback.BindVisuals(visuals.transform, fist, visuals.transform.Find("Head"), visor, torso);

            _spawnedEntities.Add(dummyGo);
            return dummy;
        }

        private static void SafeDestroy(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
    }
}
