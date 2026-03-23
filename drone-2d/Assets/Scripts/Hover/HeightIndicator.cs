using UnityEngine;

public class HeightIndicator : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] DroneHoverAgent hoverAgent;

    [Header("Visual")]
    [Tooltip("Semi-transparent plane at target height (between fail low / fail high).")]
    public bool showIndicator = true;
    [Tooltip("World size of the quad (width & height of the plane).")]
    public float quadSize = 4f;
    public Color color = new Color(0.25f, 0.85f, 1f, 0.45f);

    GameObject quad;

    void Awake()
    {
        if (hoverAgent == null)
            hoverAgent = GetComponent<DroneHoverAgent>();
    }

    void OnEnable()
    {
        if (showIndicator && hoverAgent != null && quad == null)
            CreateQuad();
    }

    void OnDisable()
    {
        if (quad != null)
        {
            Destroy(quad);
            quad = null;
        }
    }

    void CreateQuad()
    {
        quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "TargetHeightIndicator";
        Destroy(quad.GetComponent<Collider>());

        var mr = quad.GetComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Unlit/Transparent"));
        mat.color = color;
        mr.material = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        quad.transform.localScale = Vector3.one * quadSize;
    }

    void LateUpdate()
    {
        if (quad == null || hoverAgent == null)
            return;

        float yWorld = Mathf.Clamp(hoverAgent.ResolvedTargetHeight, hoverAgent.failHeightLow, hoverAgent.failHeight);
        quad.transform.position = new Vector3(
            hoverAgent.transform.position.x,
            yWorld,
            hoverAgent.transform.position.z);
    }
}
