using UnityEngine;
using System.Collections.Generic;

public class OutlineManager : MonoBehaviour
{
    public static OutlineManager Instance { get; private set; }

    [Header("Outline Settings")]
    public Color coralOutlineColor = new Color(0.8f, 0.2f, 0.2f, 1f);
    public Color rockOutlineColor = new Color(0.4f, 0.4f, 0.6f, 1f);
    public Color predatorOutlineColor = new Color(1f, 0f, 0f, 1f);
    public float outlineWidth = 0.05f;

    private Dictionary<GameObject, Material> outlineMaterials = new Dictionary<GameObject, Material>();
    private Dictionary<GameObject, List<Material>> originalMaterials = new Dictionary<GameObject, List<Material>>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ApplyOutline(GameObject obj, Color outlineColor)
    {
        if (outlineMaterials.ContainsKey(obj)) return;

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        // 保存原始材质
        originalMaterials[obj] = new List<Material>();
        foreach (Renderer renderer in renderers)
        {
            originalMaterials[obj].AddRange(renderer.sharedMaterials);
        }

        // 创建描边材质
        Material outlineMat = new Material(Shader.Find("Custom/Outline"));
        outlineMat.SetColor("_OutlineColor", outlineColor);
        outlineMat.SetFloat("_OutlineWidth", outlineWidth);
        outlineMaterials[obj] = outlineMat;

        // 应用描边材质到所有子对象
        foreach (Renderer renderer in renderers)
        {
            // 创建新的材质数组：原始材质 + 描边材质
            Material[] newMaterials = new Material[renderer.sharedMaterials.Length + 1];
            for (int i = 0; i < renderer.sharedMaterials.Length; i++)
            {
                newMaterials[i] = renderer.sharedMaterials[i];
            }
            newMaterials[renderer.sharedMaterials.Length] = outlineMat;
            
            renderer.materials = newMaterials;
        }
    }

    public void RemoveOutline(GameObject obj)
    {
        if (!outlineMaterials.ContainsKey(obj)) return;

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        // 恢复原始材质
        if (originalMaterials.ContainsKey(obj))
        {
            int index = 0;
            foreach (Renderer renderer in renderers)
            {
                if (index + renderer.sharedMaterials.Length <= originalMaterials[obj].Count)
                {
                    Material[] origMats = new Material[renderer.sharedMaterials.Length];
                    for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                    {
                        origMats[i] = originalMaterials[obj][index++];
                    }
                    renderer.materials = origMats;
                }
            }
            originalMaterials.Remove(obj);
        }

        // 清理资源
        if (outlineMaterials.ContainsKey(obj))
        {
            Destroy(outlineMaterials[obj]);
            outlineMaterials.Remove(obj);
        }
    }

    void OnDestroy()
    {
        // 清理所有描边材质
        foreach (var mat in outlineMaterials.Values)
        {
            if (mat != null) Destroy(mat);
        }
        outlineMaterials.Clear();
        originalMaterials.Clear();
    }
}