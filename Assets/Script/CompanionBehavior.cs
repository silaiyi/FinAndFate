using UnityEngine;

public class CompanionBehavior : MonoBehaviour
{
    [Header("Visual Effects")]
    public ParticleSystem reunionEffect;
    public Light glowLight;
    public float pulseSpeed = 1f;
    public float minIntensity = 1f;
    public float maxIntensity = 3f;
    
    private float originalIntensity;
    
    void Start()
    {
        if (glowLight != null)
        {
            originalIntensity = glowLight.intensity;
        }
    }
    
    void Update()
    {
        // 简单的脉动效果
        if (glowLight != null)
        {
            float intensity = Mathf.Lerp(minIntensity, maxIntensity, 
                Mathf.PingPong(Time.time * pulseSpeed, 1));
            glowLight.intensity = originalIntensity * intensity;
        }
    }
    
    public void PlayReunionEffect()
    {
        if (reunionEffect != null)
        {
            reunionEffect.Play();
        }
    }
}