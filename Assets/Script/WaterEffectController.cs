using UnityEngine;

[RequireComponent(typeof(Camera))]
public class WaterEffectController : MonoBehaviour
{
    [Header("Water Height Settings")]
    public float waterHeight = 0f;
    public float waterSurfaceHeight = 0f;

    [Header("Pollution Settings")]
    public float maxViewDistance = 200f;
    public float visibilityAtScore0 = 100f;
    public float visibilityAtScore10 = 50f;  // 提高污染度10时的能见度

    [Header("Color Settings")]
    public Color clearWaterColor = new Color(0.1f, 0.3f, 0.6f, 0.7f);
    public Color pollutedWaterColor = new Color(0.1f, 0.3f, 0.1f, 0.9f); // 深绿色

    [Header("Fog Color Settings")]
    public Color fogColorAtScore0 = new Color(0.1f, 0.3f, 0.4f);
    public Color fogColorAtScore10 = new Color(0.1f, 0.3f, 0.15f); // 深绿色

    [Header("Underwater Effect Settings")]
    public Material underwaterEffectMaterial;

    private SwimmingController swimmingController;
    private Camera mainCamera;
    private bool isUnderwater;
    private float pollutionFactor;
    private float targetPollutionFactor;
    private float pollutionChangeSpeed = 0.5f; // 污染因子变化速度

    void Start()
    {
        mainCamera = GetComponent<Camera>();
        swimmingController = FindObjectOfType<SwimmingController>();

        if (swimmingController == null)
        {
            Debug.LogError("SwimmingController not found in scene.");
            return;
        }

        // 确保有水下效果材质
        if (underwaterEffectMaterial == null)
        {
            CreateUnderwaterEffectMaterial();
        }

        // 初始化材质参数
        underwaterEffectMaterial.SetColor("_ClearWaterColor", clearWaterColor);
        underwaterEffectMaterial.SetColor("_PollutedWaterColor", pollutedWaterColor);
        pollutionFactor = Mathf.Clamp01(swimmingController.score / 10f);
        targetPollutionFactor = pollutionFactor; // 初始化目标值
    }

    void Update()
    {
        if (swimmingController == null) return;

        // 获取当前污染分数并计算污染因子
        pollutionFactor = Mathf.Lerp(pollutionFactor, targetPollutionFactor, pollutionChangeSpeed * Time.deltaTime);

        // 更新水下状态
        isUnderwater = swimmingController.transform.position.y < waterHeight;

        // 更新雾效设置
        UpdateFogSettings();

        // 传递污染因子给着色器
        if (underwaterEffectMaterial != null)
        {
            underwaterEffectMaterial.SetFloat("_PollutionFactor", pollutionFactor);
        }
    }
    public void UpdatePollution(int newScore)
    {
        targetPollutionFactor = Mathf.Clamp01(newScore / 10f);
        UpdateFogSettings();
        
        if (underwaterEffectMaterial != null)
        {
            underwaterEffectMaterial.SetFloat("_PollutionFactor", pollutionFactor);
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (isUnderwater && underwaterEffectMaterial != null)
        {
            Graphics.Blit(source, destination, underwaterEffectMaterial);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }

    void UpdateFogSettings()
    {
        // 使用更平滑的能见度曲线
        float visibilityFactor = Mathf.Pow(pollutionFactor, 0.4f); // 更平缓的过渡

        // 确保最低能见度不低于60m
        float minVisibility = Mathf.Max(visibilityAtScore10, 60f);
        float fogStart = Mathf.Lerp(visibilityAtScore0, minVisibility, visibilityFactor);

        // 应用曲线平滑
        fogStart = Mathf.Lerp(visibilityAtScore0, fogStart, pollutionFactor);
        float fogEnd = fogStart * 1.5f;

        // 设置雾效
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = fogStart;
        RenderSettings.fogEndDistance = fogEnd;

        // 减少雾效颜色变化强度
        Color fogColor = Color.Lerp(fogColorAtScore0, fogColorAtScore10, pollutionFactor * 0.6f);

        // 减少颜色加深程度
        fogColor = DarkenColor(fogColor, pollutionFactor * 0.15f);
        RenderSettings.fogColor = fogColor;

        // 调试信息
        Debug.Log($"污染等级: {pollutionFactor * 10f:F1} | 能见度: {fogStart:F1}-{fogEnd:F1}m");
    }

    // 加深颜色
    private Color DarkenColor(Color color, float amount)
    {
        return new Color(
            color.r * (1 - amount),
            color.g * (1 - amount),
            color.b * (1 - amount),
            color.a
        );
    }

    void CreateUnderwaterEffectMaterial()
    {
        // 尝试加载着色器
        Shader underwaterShader = Shader.Find("Custom/UnderwaterEffect");

        if (underwaterShader == null)
        {
            Debug.LogError("Underwater effect shader not found. Please create a shader named 'Custom/UnderwaterEffect'.");
            return;
        }

        underwaterEffectMaterial = new Material(underwaterShader)
        {
            name = "UnderwaterEffectMaterial",
            hideFlags = HideFlags.DontSave
        };
    }

    void OnDestroy()
    {
        // 清理动态创建的材质
        if (underwaterEffectMaterial != null && Application.isPlaying)
        {
            Destroy(underwaterEffectMaterial);
        }
    }
}