/*
 * 此游戏的部分代码实现参考了 DeepSeek-R1 AI 助手的建议。
 * 引用格式（APA 7th）:
 *   DeepSeek. (2024). DeepSeek-R1: An AI assistant by DeepSeek. 
 *   Retrieved from https://deepseek.com
 */
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class StencilBufferRenderer : MonoBehaviour
{
    void OnEnable()
    {
        // 确保模板缓冲可用
        GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;
    }

    void OnPreRender()
    {
        // 设置模板缓冲
        Shader.SetGlobalInt("_StencilRef", 1);
        Shader.SetGlobalInt("_StencilComp", (int)CompareFunction.Always);
    }
}