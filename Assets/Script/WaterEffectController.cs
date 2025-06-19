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
    public float visibilityAtScore0 = 100f;
    public float visibilityAtScore10 = 50f;

    private SwimmingController swimmingController;
    private Camera mainCamera;
    private bool isUnderwater;
    private float pollutionFactor;

    void Start()
    {
        mainCamera = GetComponent<Camera>();
        swimmingController = FindObjectOfType<SwimmingController>();
        if (swimmingController != null) 
            pollutionFactor = Mathf.Clamp01(swimmingController.score / 10f);
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

    void UpdateFogSettings()
    {
        float visibilityFactor = Mathf.Pow(pollutionFactor, 0.4f);
        float fogStart = Mathf.Lerp(visibilityAtScore0, visibilityAtScore10, visibilityFactor);
        float fogEnd = fogStart * 1.5f;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = fogStart;
        RenderSettings.fogEndDistance = fogEnd;
        RenderSettings.fogColor = Color.Lerp(fogColorAtScore0, fogColorAtScore10, pollutionFactor * 0.6f);
    }

    private void HandlePollutionChanged(int newScore)
    {
        pollutionFactor = Mathf.Clamp01(newScore / 10f);
        UpdateFogSettings();
    }

    private void OnEnable() => SwimmingController.OnPollutionChanged += HandlePollutionChanged;
    private void OnDisable() => SwimmingController.OnPollutionChanged -= HandlePollutionChanged;
}