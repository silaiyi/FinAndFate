using UnityEngine;

public class CoralParticleController : MonoBehaviour
{
    [Header("Particle References")]
    public ParticleSystem bubbles;
    public ParticleSystem fragments;
    public ParticleSystem dust;
    
    [Header("Effect Settings")]
    [Range(0.1f, 2f)] public float bubbleIntensity = 1f;
    [Range(0.1f, 2f)] public float fragmentIntensity = 1f;
    [Range(0.1f, 2f)] public float dustIntensity = 1f;
    
    void Start()
    {
        // 自动获取子对象的粒子系统
        if (bubbles == null) bubbles = transform.Find("Bubbles")?.GetComponent<ParticleSystem>();
        if (fragments == null) fragments = transform.Find("Fragments")?.GetComponent<ParticleSystem>();
        if (dust == null) dust = transform.Find("Dust")?.GetComponent<ParticleSystem>();
    }
    
    public void PlayEffects(Vector3 position, Material coralMaterial)
    {
        transform.position = position;
        
        // 配置碎片材质
        if (fragments != null && coralMaterial != null)
        {
            var renderer = fragments.GetComponent<ParticleSystemRenderer>();
            renderer.material = coralMaterial;
        }
        
        // 播放所有粒子系统
        PlayParticleSystem(bubbles, bubbleIntensity);
        PlayParticleSystem(fragments, fragmentIntensity);
        PlayParticleSystem(dust, dustIntensity);
        
        // 自动销毁
        Destroy(gameObject, 5f);
    }
    
    private void PlayParticleSystem(ParticleSystem ps, float intensity)
    {
        if (ps == null) return;
        
        // 调整发射率
        var emission = ps.emission;
        emission.rateOverTimeMultiplier = intensity;
        
        // 调整速度
        var main = ps.main;
        main.startSpeedMultiplier = intensity;
        
        // 调整大小
        main.startSizeMultiplier = intensity;
        
        // 播放粒子系统
        ps.Play();
    }
}