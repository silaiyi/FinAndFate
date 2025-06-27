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