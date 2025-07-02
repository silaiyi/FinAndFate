/*
 * 此游戏的部分代码实现参考了 DeepSeek-R1 AI 助手的建议。
 * 引用格式（APA 7th）:
 *   DeepSeek. (2024). DeepSeek-R1: An AI assistant by DeepSeek. 
 *   Retrieved from https://deepseek.com
 */
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
    [Header("Distortion Settings")] // 新增扭曲设置区块
    public float baseDistortion = 0.12f;          // 基础场景扭曲强度
    public float dangerZoneDistortion = 0.72f;     // 危险区扭曲强度（增强20%）
    public float pollutionDistortionFactor = 0.36f; // 污染对扭曲的影响系数

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
            
            // 計算最終扭曲值 (增強20%後)
            float distortion = isInDangerZone ? 
                dangerZoneDistortion : 
                baseDistortion;
                
            // 應用污染影響 (係數也增強20%)
            distortion += pollutionFactor * pollutionDistortionFactor;
            
            // 確保扭曲值在合理範圍內
            distortion = Mathf.Clamp(distortion, 0.1f, 1.0f);
            
            underwaterEffectMaterial.SetFloat("_WaveDistortion", distortion); // 注意屬性名變化
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

        // 获取当前场景名称
        string currentScene = SceneManager.GetActiveScene().name;

        // 计算污染等级（0-1之间）
        int pollutionLevel = Mathf.Min(maxPollutionLevel, swimmingController.sewageScore);
        float pollutionFactor = (float)pollutionLevel / maxPollutionLevel;

        // 如果不是危险区，污染因子减半（包括第一关和第三关）
        if (!isInDangerZone || currentScene != "Level2")
        {
            pollutionFactor *= 0.5f; // 安全区污染效果减半
        }

        return pollutionFactor;
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
        // 确保能见度不低于100f
        float visibility = Mathf.Lerp(maxVisibility, minVisibility, pollutionFactor);
        visibility = Mathf.Max(visibility, minVisibility); // 强制最低能见度为100f

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
                // 切换到危险区材质（使用新的dangerZoneDistortion值）
                underwaterEffectMaterial.SetColor("_TintColor", new Color(0.3f, 0.1f, 0.1f, 0.8f));
                // 注意：Distortion现在在Update中动态设置，这里不再需要设置
            }
            else
            {
                // 恢复正常材质
                underwaterEffectMaterial.SetColor("_TintColor", clearWaterColor);
                // 注意：Distortion现在在Update中动态设置，这里不再需要设置
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
            // 注意這裡屬性名改為_WaveDistortion
            underwaterEffectMaterial.SetFloat("_WaveDistortion", 0f);
        }

        // 更新雾效设置
        UpdateFogSettings();
    }

    private void OnDestroy()
    {
        // 取消场景加载监听
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    public void UpdateFromPlayerPrefs()
    {
        // 从 PlayerPrefs 获取污染分数
        int carbon = PlayerPrefs.GetInt("CarbonScore", 0);
        int trash = PlayerPrefs.GetInt("TrashScore", 0);
        int fishing = PlayerPrefs.GetInt("FishingScore", 0);
        int sewage = PlayerPrefs.GetInt("SewageScore", 0);
        
        // 创建临时分数对象
        SwimmingController.PollutionScores scores = new SwimmingController.PollutionScores
        {
            carbon = carbon,
            trash = trash,
            fishing = fishing,
            sewage = sewage
        };
        
        // 应用污染效果
        HandlePollutionChanged(scores);
        
        // 确保水下效果激活
        isUnderwater = true;
        
        Debug.Log("Updated water effect from PlayerPrefs: " +
                $"Carbon={carbon}, Trash={trash}, Fishing={fishing}, Sewage={sewage}");
    }
}