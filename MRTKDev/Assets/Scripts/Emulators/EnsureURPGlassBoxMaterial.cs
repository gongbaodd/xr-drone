using UnityEngine;
using UnityEngine.Rendering;

namespace YueUltimateDronePhysics
{
    /// <summary>
    /// Magenta/pink in URP means the error shader (missing or incompatible shader).
    /// This reassigns <c>Universal Render Pipeline/Lit</c> in transparent mode when needed.
    /// See: https://discussions.unity.com/t/what-i-am-missing-pink-transparent-materials/943311
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshRenderer))]
    [ExecuteAlways]
    public sealed class EnsureURPGlassBoxMaterial : MonoBehaviour
    {
        private const string UrpLit = "Universal Render Pipeline/Lit";

        [SerializeField] private Color glassTint = new Color(0.65f, 0.82f, 0.95f, 0.22f);

        private void OnEnable()
        {
            TryRepair();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            TryRepair();
        }
#endif

        private void TryRepair()
        {
            var mr = GetComponent<MeshRenderer>();
            if (mr == null)
                return;

            var mat = mr.sharedMaterial;
            if (mat == null)
                return;

            if (mat.shader != null && mat.shader.name.Contains("Universal Render Pipeline/Lit"))
            {
                mat.SetColor("_BaseColor", glassTint);
                mat.SetColor("_Color", glassTint);
                return;
            }

            var lit = Shader.Find(UrpLit);
            if (lit == null)
            {
                Debug.LogWarning(
                    $"{nameof(EnsureURPGlassBoxMaterial)}: Shader not found '{UrpLit}'. Assign a URP asset in Project Settings → Graphics (and Quality).",
                    this);
                return;
            }

            mat.shader = lit;
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_AlphaClip", 0f);
            mat.SetInt("_SrcBlend", (int)BlendMode.One);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_SrcBlendAlpha", (int)BlendMode.One);
            mat.SetInt("_DstBlendAlpha", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.SetFloat("_WorkflowMode", 1f);
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.SetColor("_BaseColor", glassTint);
            mat.SetColor("_Color", glassTint);
        }
    }
}
