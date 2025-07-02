/*
 * 此游戏的部分代码实现参考了 DeepSeek-R1 AI 助手的建议。
 * 引用格式（APA 7th）:
 *   DeepSeek. (2024). DeepSeek-R1: An AI assistant by DeepSeek. 
 *   Retrieved from https://deepseek.com
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetLOD : MonoBehaviour
{
    public MeshRenderer netRenderer;
    public ParticleSystem particles;
    public float maxVisibleDistance = 50f;
    
    private Transform player;
    
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }
    
    void Update()
    {
        if (player == null) return;
        
        float distance = Vector3.Distance(transform.position, player.position);
        bool visible = distance < maxVisibleDistance;
        
        if (netRenderer != null) netRenderer.enabled = visible;
        if (particles != null)
        {
            if (visible && !particles.isPlaying) particles.Play();
            if (!visible && particles.isPlaying) particles.Stop();
        }
    }
}
