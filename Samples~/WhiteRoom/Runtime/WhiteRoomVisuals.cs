using UnityEngine;

namespace Yu5h1Lib.CombatAesthetic.Samples.WhiteRoom
{
    /// <summary>
    /// Drives primitive robot visual feedback (crouch scale, punch extension, visor color) from <see cref="CombatEntity"/> state.
    /// </summary>
    public class WhiteRoomVisualFeedback : MonoBehaviour
    {
        [SerializeField] private CombatEntity _entity;
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private Transform _headTransform;
        [SerializeField] private Transform _fistTransform;
        [SerializeField] private Renderer _visorRenderer;
        [SerializeField] private Renderer _torsoRenderer;

        private Vector3 _fistDefaultLocalPos;
        private Color _defaultVisorColor = new Color(0.1f, 0.7f, 1.0f);
        private Color _defaultTorsoColor = new Color(0.92f, 0.92f, 0.92f);

        private void Awake()
        {
            if (_entity == null) _entity = GetComponent<CombatEntity>();
            if (_fistTransform != null) _fistDefaultLocalPos = _fistTransform.localPosition;
            if (_visorRenderer != null && _visorRenderer.material != null)
                _defaultVisorColor = _visorRenderer.material.color;
            if (_torsoRenderer != null && _torsoRenderer.material != null)
                _defaultTorsoColor = _torsoRenderer.material.color;
        }

        /// <summary>
        /// Binds the visual hierarchy references for state-driven color and transform feedback.
        /// </summary>
        public void BindVisuals(Transform root, Transform fist, Transform head, Renderer visor, Renderer torso)
        {
            _visualRoot = root;
            _fistTransform = fist;
            _headTransform = head;
            _visorRenderer = visor;
            _torsoRenderer = torso;
            if (_fistTransform != null) _fistDefaultLocalPos = _fistTransform.localPosition;
            if (_visorRenderer != null && _visorRenderer.material != null)
                _defaultVisorColor = _visorRenderer.material.color;
            if (_torsoRenderer != null && _torsoRenderer.material != null)
                _defaultTorsoColor = _torsoRenderer.material.color;
        }

        private void Update()
        {
            if (_entity == null || _visualRoot == null) return;

            CombatState state = _entity.State;

            float targetYScale = _entity.IsCrouching ? 0.6f : 1.0f;
            if (state == CombatState.Incapacitated) targetYScale = 0.35f;
            Vector3 scale = _visualRoot.localScale;
            scale.y = Mathf.Lerp(scale.y, targetYScale, 15f * Time.deltaTime);
            _visualRoot.localScale = scale;

            if (_fistTransform != null)
            {
                Vector3 targetFistPos = _fistDefaultLocalPos;
                if (state == CombatState.AttackStartup)
                {
                    targetFistPos = _fistDefaultLocalPos + new Vector3(0, 0.05f, -0.2f);
                }
                else if (state == CombatState.AttackActive)
                {
                    targetFistPos = _fistDefaultLocalPos + new Vector3(0, 0, 0.75f);
                }
                else if (state == CombatState.Guarding || state == CombatState.Parrying)
                {
                    targetFistPos = _fistDefaultLocalPos + new Vector3(-0.25f, 0.1f, 0.25f);
                }

                _fistTransform.localPosition = Vector3.Lerp(_fistTransform.localPosition, targetFistPos, 20f * Time.deltaTime);
            }

            if (_visorRenderer != null && _visorRenderer.material != null)
            {
                Color targetVisorColor = _defaultVisorColor;
                if (state == CombatState.AttackStartup)
                    targetVisorColor = new Color(1f, 0.45f, 0f);
                else if (state == CombatState.AttackActive)
                    targetVisorColor = new Color(1f, 0.1f, 0.1f);
                else if (state == CombatState.Guarding)
                    targetVisorColor = new Color(1f, 0.9f, 0.2f);
                else if (state == CombatState.Parrying)
                    targetVisorColor = new Color(0.2f, 1f, 1f);
                else if (state == CombatState.Stunned)
                    targetVisorColor = new Color(0.4f, 0.4f, 0.9f);
                else if (state == CombatState.Flinching)
                    targetVisorColor = Color.red;
                else if (state == CombatState.Incapacitated)
                    targetVisorColor = new Color(0.15f, 0.15f, 0.15f);

                _visorRenderer.material.color = Color.Lerp(_visorRenderer.material.color, targetVisorColor, 15f * Time.deltaTime);
            }

            if (_torsoRenderer != null && _torsoRenderer.material != null)
            {
                Color targetTorsoColor = _defaultTorsoColor;
                if (state == CombatState.Flinching)
                    targetTorsoColor = new Color(1f, 0.5f, 0.5f);
                else if (state == CombatState.Stunned)
                    targetTorsoColor = new Color(0.6f, 0.7f, 1f);
                else if (state == CombatState.Incapacitated)
                    targetTorsoColor = new Color(0.4f, 0.4f, 0.4f);

                _torsoRenderer.material.color = Color.Lerp(_torsoRenderer.material.color, targetTorsoColor, 10f * Time.deltaTime);
            }
        }
    }

    /// <summary>
    /// Factory methods for creating simplified robot geometry in the WhiteRoom arena.
    /// </summary>
    public static class WhiteRoomVisuals
    {
        /// <summary>
        /// Builds a primitive robot hierarchy under the specified parent transform.
        /// </summary>
        public static GameObject CreateRobotModel(Transform parent, out Transform rightFist, out Renderer visorRenderer, out Renderer torsoRenderer)
        {
            GameObject root = new GameObject("VisualRoot");
            root.transform.SetParent(parent, false);

            Material whiteMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Diffuse"));
            whiteMat.color = new Color(0.92f, 0.92f, 0.92f);

            Material darkMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Diffuse"));
            darkMat.color = new Color(0.2f, 0.22f, 0.25f);

            Material visorMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Diffuse"));
            visorMat.color = new Color(0.1f, 0.7f, 1.0f);

            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            torso.name = "Torso";
            torso.transform.SetParent(root.transform, false);
            torso.transform.localPosition = new Vector3(0, 1.15f, 0);
            torso.transform.localScale = new Vector3(0.55f, 0.7f, 0.35f);
            torsoRenderer = torso.GetComponent<Renderer>();
            torsoRenderer.sharedMaterial = whiteMat;
            Object.DestroyImmediate(torso.GetComponent<Collider>());

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0, 1.7f, 0);
            head.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            head.GetComponent<Renderer>().sharedMaterial = whiteMat;
            Object.DestroyImmediate(head.GetComponent<Collider>());

            GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visor.name = "Visor";
            visor.transform.SetParent(head.transform, false);
            visor.transform.localPosition = new Vector3(0, 0.05f, 0.52f);
            visor.transform.localScale = new Vector3(0.85f, 0.25f, 0.15f);
            visorRenderer = visor.GetComponent<Renderer>();
            visorRenderer.sharedMaterial = visorMat;
            Object.DestroyImmediate(visor.GetComponent<Collider>());

            GameObject leftArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftArm.name = "LeftArm";
            leftArm.transform.SetParent(root.transform, false);
            leftArm.transform.localPosition = new Vector3(-0.4f, 1.1f, 0);
            leftArm.transform.localScale = new Vector3(0.18f, 0.6f, 0.18f);
            leftArm.GetComponent<Renderer>().sharedMaterial = darkMat;
            Object.DestroyImmediate(leftArm.GetComponent<Collider>());

            GameObject rightArmRoot = new GameObject("RightArmRoot");
            rightArmRoot.transform.SetParent(root.transform, false);
            rightArmRoot.transform.localPosition = new Vector3(0.4f, 1.1f, 0);

            GameObject fist = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fist.name = "RightFist";
            fist.transform.SetParent(rightArmRoot.transform, false);
            fist.transform.localPosition = Vector3.zero;
            fist.transform.localScale = new Vector3(0.22f, 0.22f, 0.35f);
            fist.GetComponent<Renderer>().sharedMaterial = darkMat;
            Object.DestroyImmediate(fist.GetComponent<Collider>());

            rightFist = fist.transform;

            GameObject leftLeg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftLeg.name = "LeftLeg";
            leftLeg.transform.SetParent(root.transform, false);
            leftLeg.transform.localPosition = new Vector3(-0.16f, 0.45f, 0);
            leftLeg.transform.localScale = new Vector3(0.2f, 0.8f, 0.2f);
            leftLeg.GetComponent<Renderer>().sharedMaterial = darkMat;
            Object.DestroyImmediate(leftLeg.GetComponent<Collider>());

            GameObject rightLeg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightLeg.name = "RightLeg";
            rightLeg.transform.SetParent(root.transform, false);
            rightLeg.transform.localPosition = new Vector3(0.16f, 0.45f, 0);
            rightLeg.transform.localScale = new Vector3(0.2f, 0.8f, 0.2f);
            rightLeg.GetComponent<Renderer>().sharedMaterial = darkMat;
            Object.DestroyImmediate(rightLeg.GetComponent<Collider>());

            return root;
        }
    }
}
