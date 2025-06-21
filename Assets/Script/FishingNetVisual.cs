using UnityEngine;

public class FishingNetVisual : MonoBehaviour
{
    [Header("Net Settings")]
    public float netRadius = 5f;
    public float netHeight = 10f;
    public float netOffset = 2.5f;
    
    [Header("Visual Components")]
    public LineRenderer netOutline;
    public ParticleSystem netParticles;
    
    [Header("Animation Settings")]
    public float pulseSpeed = 1.5f;
    public float pulseIntensity = 0.2f;
    public float swaySpeed = 0.8f;
    public float swayAmount = 0.3f;
    
    [Header("Material Settings")]
    public Material netMaterial;
    public Color baseColor = new Color(0.2f, 0.5f, 1.0f, 0.3f);
    public Color edgeColor = Color.white;
    public float glowIntensity = 1.5f;
    
    private Mesh netMesh; // 添加这行声明
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private float pulsePhase;
    private float swayPhase;
    
    void Start()
    {
        InitializeNetMesh();
        ConfigureParticleSystem();
        ConfigureOutline();
        ConfigureMaterial();
    }
    
    void InitializeNetMesh()
    {
        // 创建简单的圆柱体作为渔网
        GameObject netCylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Mesh cylinderMesh = netCylinder.GetComponent<MeshFilter>().mesh;
        Destroy(netCylinder);
        
        // 创建新的网格实例
        netMesh = new Mesh();
        netMesh.vertices = cylinderMesh.vertices;
        netMesh.triangles = cylinderMesh.triangles;
        netMesh.uv = cylinderMesh.uv;
        netMesh.normals = cylinderMesh.normals;
        
        // 缩放网格到合适大小
        Vector3[] vertices = netMesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].x *= netRadius;
            vertices[i].z *= netRadius;
            vertices[i].y = (vertices[i].y - 0.5f) * netHeight;
        }
        netMesh.vertices = vertices;
        netMesh.RecalculateBounds();
        netMesh.RecalculateNormals();
        
        // 添加网格组件
        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = netMesh;
        
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material = netMaterial;
    }
    
    void ConfigureMaterial()
    {
        if (netMaterial != null)
        {
            // 使用更安全的属性设置方式
            if (netMaterial.HasProperty("_Color"))
            {
                netMaterial.SetColor("_Color", baseColor);
            }
            if (netMaterial.HasProperty("_MainColor"))
            {
                netMaterial.SetColor("_MainColor", baseColor);
            }
            if (netMaterial.HasProperty("_EdgeColor"))
            {
                netMaterial.SetColor("_EdgeColor", edgeColor);
            }
            if (netMaterial.HasProperty("_GlowIntensity"))
            {
                netMaterial.SetFloat("_GlowIntensity", glowIntensity);
            }
        }
    }
    
    void ConfigureParticleSystem()
    {
        if (netParticles == null) return;
        
        var main = netParticles.main;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = 1.5f;
        main.startSpeed = 0.5f;
        main.startSize = 0.1f;
        
        var emission = netParticles.emission;
        emission.rateOverTime = 30f;
        
        var shape = netParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.radius = netRadius * 0.8f;
        shape.angle = 0f;
        shape.length = netHeight * 0.9f;
    }
    
    void ConfigureOutline()
    {
        if (netOutline == null) return;
        
        // 创建渔网轮廓
        int segments = 32;
        netOutline.positionCount = segments;
        netOutline.loop = true;
        netOutline.useWorldSpace = false;
        netOutline.widthMultiplier = 0.05f;
        netOutline.material = new Material(Shader.Find("Sprites/Default"));
        netOutline.startColor = Color.white;
        netOutline.endColor = Color.white;
        
        // 设置轮廓位置
        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2 / segments;
            float x = Mathf.Cos(angle) * netRadius;
            float z = Mathf.Sin(angle) * netRadius;
            netOutline.SetPosition(i, new Vector3(x, 0, z));
        }
    }
    
    void Update()
    {
        UpdatePosition();
        
        // 更新动画参数
        pulsePhase += Time.deltaTime * pulseSpeed;
        swayPhase += Time.deltaTime * swaySpeed;
        
        // 应用动画效果
        ApplyPulseAnimation();
        ApplySwayAnimation();
    }
    
    void UpdatePosition()
    {
        // 将渔网定位在船尾
        transform.localPosition = new Vector3(0, 0, -netOffset);
    }
    
    void ApplyPulseAnimation()
    {
        // 应用脉冲动画
        float pulse = Mathf.Sin(pulsePhase) * pulseIntensity;
        transform.localScale = Vector3.one * (1 + pulse * 0.1f);
    }
    
    void ApplySwayAnimation()
    {
        // 应用摇摆动画
        float sway = Mathf.Sin(swayPhase) * swayAmount;
        transform.localRotation = Quaternion.Euler(sway * 5f, 0, sway * 3f);
    }
}