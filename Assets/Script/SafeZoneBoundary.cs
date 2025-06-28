using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SafeZoneBoundary : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public SafeZoneController safeZone;
    public int segments = 32;
    public float heightOffset = 1f;
    
    void Start()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }
        }
        
        lineRenderer.loop = true;
        lineRenderer.useWorldSpace = false;
        lineRenderer.widthMultiplier = 0.5f;
        
        UpdateBoundary();
    }
    
    void Update()
    {
        // 更新边界位置
        if (safeZone != null)
        {
            transform.position = safeZone.safeZoneCenter.position;
            UpdateBoundary();
        }
    }
    
    void UpdateBoundary()
    {
        if (safeZone == null || lineRenderer == null) return;
        
        Vector3[] points = new Vector3[segments];
        // 使用公共属性替代直接访问私有字段
        float width = safeZone.CurrentWidth / 2f;
        float length = safeZone.length / 2f;
        
        for (int i = 0; i < segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            
            // 创建矩形边界
            if (i < segments / 4)
            {
                // 顶部边
                points[i] = new Vector3(
                    Mathf.Lerp(-width, width, (i % (segments / 4)) / (segments / 4f)),
                    0,
                    length
                );
            }
            else if (i < segments / 2)
            {
                // 右侧边
                points[i] = new Vector3(
                    width,
                    0,
                    Mathf.Lerp(length, -length, (i % (segments / 4)) / (segments / 4f))
                );
            }
            else if (i < segments * 3 / 4)
            {
                // 底部边
                points[i] = new Vector3(
                    Mathf.Lerp(width, -width, (i % (segments / 4)) / (segments / 4f)),
                    0,
                    -length
                );
            }
            else
            {
                // 左侧边
                points[i] = new Vector3(
                    -width,
                    0,
                    Mathf.Lerp(-length, length, (i % (segments / 4)) / (segments / 4f))
                );
            }
            
            points[i].y = heightOffset;
        }
        
        lineRenderer.positionCount = segments;
        lineRenderer.SetPositions(points);
    }
    
    public void SetBoundaryColor(Color color)
    {
        if (lineRenderer != null)
        {
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
        }
    }
}