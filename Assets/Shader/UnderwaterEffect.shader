/*
 * 此游戏的部分代码实现参考了 DeepSeek-R1 AI 助手的建议。
 * 引用格式（APA 7th）:
 *   DeepSeek. (2024). DeepSeek-R1: An AI assistant by DeepSeek. 
 *   Retrieved from https://deepseek.com
 */
Shader "Custom/UnderwaterEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ClearWaterColor ("Clear Water Color", Color) = (0.1, 0.3, 0.6, 0.5)
        _PollutedWaterColor ("Polluted Water Color", Color) = (0.1, 0.3, 0.1, 0.9)
        _PollutionFactor ("Pollution Factor", Range(0, 1)) = 0
        
        [Header(Wave Settings)]
        _WaveSpeed("Wave Speed", Range(0, 1)) = 0.1
        _WaveStrength("Wave Strength", Range(0, 1)) = 0.018
        _WaveScale("Wave Scale", Range(0.1, 10)) = 1.8
        _WaveFrequency("Wave Frequency", Range(1, 30)) = 8.4
        _WaveDistortion("Wave Distortion", Range(0, 1)) = 0.48
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _ClearWaterColor;
            float4 _PollutedWaterColor;
            float _PollutionFactor;
            
            float _WaveSpeed;
            float _WaveStrength;
            float _WaveScale;
            float _WaveFrequency;
            float _WaveDistortion;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            float2 waveOffset(float2 uv)
            {
                float waveTime = _Time.y * _WaveSpeed;
                float2 waveUV = uv * _WaveScale;
                
                float wave1 = sin(waveUV.x * _WaveFrequency + waveTime);
                float wave2 = cos(waveUV.y * _WaveFrequency * 0.8 + waveTime * 1.2);
                float wave3 = sin(waveUV.x * _WaveFrequency * 1.5 - waveTime * 0.7);
                float wave4 = cos(waveUV.y * _WaveFrequency * 1.2 + waveTime * 1.5);
                
                float2 offset = float2(
                    (wave1 + wave3) * 0.5,
                    (wave2 + wave4) * 0.5
                );
                
                return offset * _WaveStrength * 0.1;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                
                // 直接使用最大扭曲效果（增强20%后的值）
                float2 waveOff = waveOffset(screenUV);
                
                // 应用增强20%的扭曲效果
                fixed4 sceneColor = tex2D(_MainTex, i.uv + waveOff * _WaveDistortion * 1.2);
                
                // 水色混合（保持原逻辑）
                fixed4 baseWaterColor = lerp(
                    _ClearWaterColor * float4(1.0, 1.0, 1.5, 1.0),
                    _PollutedWaterColor,
                    _PollutionFactor * 0.8f
                );
                
                float blendFactor = lerp(0.4, 0.9, _PollutionFactor);
                fixed4 underwaterColor = lerp(sceneColor, baseWaterColor, blendFactor);
                
                float3 targetTint = lerp(
                    float3(0.1, 0.3, 0.8),
                    float3(0.1, 0.3, 0.1),
                    _PollutionFactor
                );
                underwaterColor.rgb = lerp(underwaterColor.rgb, targetTint, blendFactor * 0.7f);
                
                underwaterColor.rgb *= lerp(1.0, 0.85f, _PollutionFactor * 0.7f);
                underwaterColor.g *= lerp(1.0, 1.1f, _PollutionFactor * 0.8f);
                
                float2 uv = i.uv * 2.0 - 1.0;
                underwaterColor.rgb *= 1.0 - dot(uv, uv) * 0.15f * _PollutionFactor;
                
                float contrast = lerp(1.0, 1.2f, _PollutionFactor * 0.3f);
                underwaterColor.rgb = (underwaterColor.rgb - 0.5f) * contrast + 0.5f;
                
                return underwaterColor;
            }
            ENDCG
        }
    }
}