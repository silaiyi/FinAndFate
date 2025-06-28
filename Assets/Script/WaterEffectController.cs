using UnityEngine;
using UnityEngine.SceneManagement;

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
    public float maxVisibility = 200f;  // 最大能见度
    public float minVisibility = 80f;   // 最小能见度
    public int maxPollutionLevel = 7;   // 雾效最大等级

    private SwimmingController swimmingController;
    private Camera mainCamera;
    private bool isUnderwater;
    private bool isInDangerZone;
    private float dangerZonePollutionFactor = 1.0f; // 危險區最大污染等級
    
    // 添加单例模式以便访问
    public static WaterEffectController Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // 确保跨场景时不销毁
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        mainCamera = GetComponent<Camera>();
        swimmingController = FindObjectOfType<SwimmingController>();
        
        // 初始设置
        UpdateFogSettings();
    }

    void Update()
    {
        if (swimmingController == null) 
        {
            // 如果找不到游泳控制器，尝试重新查找
            swimmingController = FindObjectOfType<SwimmingController>();
            return;
        }
        
        // 只有在当前场景是Level2时才应用危险区效果
        bool isLevel2 = SceneManager.GetActiveScene().name == "Level2";
        
        // 更新水下状态
        isUnderwater = swimmingController.transform.position.y < waterHeight;
        
        // 只在Level2场景应用危险区效果
        if (isLevel2)
        {
            UpdateFogSettings();
        }
        else
        {
            // 非Level2场景使用基本污染效果
            ApplyBasePollutionEffect();
        }
        
        // 更新材质效果
        if (underwaterEffectMaterial != null)
        {
            float pollutionFactor = CalculatePollutionFactor();
            underwaterEffectMaterial.SetFloat("_PollutionFactor", pollutionFactor);
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, 
            isUnderwater && underwaterEffectMaterial != null ? 
            underwaterEffectMaterial : null);
    }

    // 计算污染因子
    private float CalculatePollutionFactor()
    {
        if (swimmingController == null) return 0f;
        
        // 计算污染等级（0-1之间）
        int pollutionLevel = Mathf.Min(maxPollutionLevel, swimmingController.sewageScore);
        
        // 如果在危险区，直接使用最大污染等级
        if (isInDangerZone)
        {
            pollutionLevel = maxPollutionLevel;
        }
        
        // 计算雾效强度（0-1之间）
        return (float)pollutionLevel / maxPollutionLevel;
    }

    // 更新雾效设置
    void UpdateFogSettings()
    {
        float pollutionFactor = CalculatePollutionFactor();
        ApplyFogSettings(pollutionFactor);
    }

    // 应用基础污染效果（非Level2场景使用）
    void ApplyBasePollutionEffect()
    {
        if (swimmingController == null) return;
        
        // 计算基础污染等级（0-1之间）
        int pollutionLevel = Mathf.Min(maxPollutionLevel, swimmingController.sewageScore);
        float pollutionFactor = (float)pollutionLevel / maxPollutionLevel;
        
        ApplyFogSettings(pollutionFactor);
    }

    // 应用雾效设置
    void ApplyFogSettings(float pollutionFactor)
    {
        // 计算能见度（污染等级越高，能见度越低）
        float visibility = Mathf.Lerp(maxVisibility, minVisibility, pollutionFactor);
        
        // 设置雾效参数
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = visibility * 0.7f;
        RenderSettings.fogEndDistance = visibility;
        
        // 设置雾效颜色
        RenderSettings.fogColor = Color.Lerp(
            fogColorAtScore0, 
            fogColorAtScore10, 
            pollutionFactor
        );
    }

    public void SetDangerZoneState(bool inDanger)
    {
        // 只有在Level2场景才应用危险区效果
        if (SceneManager.GetActiveScene().name != "Level2") return;
        
        // 如果状态没有变化，则直接返回
        if (isInDangerZone == inDanger) return;
        
        isInDangerZone = inDanger;
        UpdateFogSettings();
        
        // 确保水下材质不为空
        if (underwaterEffectMaterial != null)
        {
            if (inDanger)
            {
                // 切换到危险区材质
                underwaterEffectMaterial.SetColor("_TintColor", new Color(0.3f, 0.1f, 0.1f, 0.8f));
                underwaterEffectMaterial.SetFloat("_Distortion", 0.5f);
            }
            else
            {
                // 恢复正常材质
                underwaterEffectMaterial.SetColor("_TintColor", clearWaterColor);
                underwaterEffectMaterial.SetFloat("_Distortion", 0f);
            }
        }
    }

    private void HandlePollutionChanged(SwimmingController.PollutionScores newScores)
    {
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
    
    // 添加场景切换监听
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 重置危险区状态
        isInDangerZone = false;
        
        // 重置材质效果
        if (underwaterEffectMaterial != null)
        {
            underwaterEffectMaterial.SetColor("_TintColor", clearWaterColor);
            underwaterEffectMaterial.SetFloat("_Distortion", 0f);
        }
        
        // 更新雾效设置
        UpdateFogSettings();
    }
    
    private void OnDestroy()
    {
        // 取消场景加载监听
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}