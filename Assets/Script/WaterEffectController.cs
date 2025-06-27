using UnityEngine;

[RequireComponent(typeof(Camera))]
public class WaterEffectController : MonoBehaviour
{
    [Header("Water Settings")]
    public float waterHeight = 0f;
    public Color clearWaterColor = new Color(0.1f, 0.3f, 0.6f, 0.7f);
    public Color pollutedWaterColor = new Color(0.1f, 0.3f, 0.1f, 0.9f);
    public Color fogColorAtScore0 = new Color(0.1f, 0.3f, 0.4f);
    public Color fogColorAtScore10 = new Color(0.1f, 0.3f, 0.15f);
    public Material underwaterEffectMaterial;

    [Header("Visibility Settings")]
    //public float visibilityAtScore0 = 200f;
    //public float visibilityAtScore10 = 80f;
    public float maxVisibility = 200f;  // 最大能见度
    public float minVisibility = 80f;   // 最小能见度
    public int maxPollutionLevel = 5;   // 雾效最大等级

    private SwimmingController swimmingController;
    private Camera mainCamera;
    private bool isUnderwater;
    private float pollutionFactor;

    void Start()
    {
        mainCamera = GetComponent<Camera>();
        swimmingController = FindObjectOfType<SwimmingController>();
        // 移除对 score 的引用
    }

    void Update()
    {
        if (swimmingController == null) return;
        
        isUnderwater = swimmingController.transform.position.y < waterHeight;
        UpdateFogSettings();
        
        if (underwaterEffectMaterial != null)
        {
            underwaterEffectMaterial.SetFloat("_PollutionFactor", pollutionFactor);
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, 
            isUnderwater && underwaterEffectMaterial != null ? 
            underwaterEffectMaterial : null);
    }

    // WaterEffectController.cs
    void UpdateFogSettings()
    {
        // 计算污染等级（0-5之间）
        int pollutionLevel = Mathf.Min(maxPollutionLevel, swimmingController.sewageScore);
        
        // 计算雾效强度（0-1之间）
        float fogIntensity = (float)pollutionLevel / maxPollutionLevel;
        
        // 计算能见度（污染等级越高，能见度越低）
        float visibility = Mathf.Lerp(maxVisibility, minVisibility, fogIntensity);
        
        // 设置雾效参数
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = visibility * 0.7f;
        RenderSettings.fogEndDistance = visibility;
        
        // 设置雾效颜色
        RenderSettings.fogColor = Color.Lerp(
            fogColorAtScore0, 
            fogColorAtScore10, 
            fogIntensity
        );
        
        // 更新水下材质效果（限制最大强度）
        if (underwaterEffectMaterial != null)
        {
            // 使用平方根曲线减缓高污染效果
            float materialFactor = Mathf.Sqrt(fogIntensity);
            underwaterEffectMaterial.SetFloat("_PollutionFactor", materialFactor);
        }
    }

    private void HandlePollutionChanged(SwimmingController.PollutionScores newScores)
    {
        // 使用污水分數
        //pollutionFactor = Mathf.Clamp01(newScores.sewage / 10f);
        UpdateFogSettings();
    }

    private void OnEnable()
    {
        SwimmingController.OnPollutionChanged += HandlePollutionChanged;
        
        // 立即应用当前污染效果
        if (SwimmingController.Instance != null)
        {
            HandlePollutionChanged(SwimmingController.Instance.GetCurrentPollutionScores());
        }
    }

    private void OnDisable()
    {
        SwimmingController.OnPollutionChanged -= HandlePollutionChanged;
    }
}