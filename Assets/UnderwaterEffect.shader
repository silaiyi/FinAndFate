Shader "Custom/UnderwaterEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ClearWaterColor ("Clear Water Color", Color) = (0.1, 0.3, 0.6, 0.5)
        _PollutedWaterColor ("Polluted Water Color", Color) = (0.1, 0.3, 0.1, 0.9)
        _PollutionFactor ("Pollution Factor", Range(0, 1)) = 0
        
        // 新增波纹效果参数（不使用纹理）
        [Header(Wave Settings)]
        _WaveSpeed ("Wave Speed", Range(0, 1)) = 0.1
        _WaveStrength ("Wave Strength", Range(0, 0.1)) = 0.01
        _WaveScale ("Wave Scale", Range(0.1, 10)) = 1.0
        _WaveFrequency ("Wave Frequency", Range(1, 20)) = 5.0
        _WaveDistortion ("Wave Distortion", Range(0, 1)) = 0.5
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
                float4 screenPos : TEXCOORD1; // 新增屏幕位置
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _ClearWaterColor;
            float4 _PollutedWaterColor;
            float _PollutionFactor;
            
            // 波纹效果变量（不使用纹理）
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
                o.screenPos = ComputeScreenPos(o.vertex); // 计算屏幕位置
                return o;
            }

            // 波纹函数 - 使用数学函数生成波纹
            float2 waveOffset(float2 uv)
            {
                // 基础波纹 - 使用正弦波和余弦波组合
                float waveTime = _Time.y * _WaveSpeed;
                float2 waveUV = uv * _WaveScale;
                
                // 创建多方向波纹
                float wave1 = sin(waveUV.x * _WaveFrequency + waveTime);
                float wave2 = cos(waveUV.y * _WaveFrequency * 0.8 + waveTime * 1.2);
                float wave3 = sin(waveUV.x * _WaveFrequency * 1.5 - waveTime * 0.7);
                float wave4 = cos(waveUV.y * _WaveFrequency * 1.2 + waveTime * 1.5);
                
                // 组合波纹
                float2 offset;
                offset.x = (wave1 + wave3) * 0.5;
                offset.y = (wave2 + wave4) * 0.5;
                
                // 应用强度
                return offset * _WaveStrength * 0.1;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 计算屏幕UV坐标（用于波纹效果）
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                
                // 生成波纹偏移
                float2 waveOff = waveOffset(screenUV);  // 变量名改为 waveOff
                
                // 污染越高，波纹越弱
                waveOffset *= (1.0 - _PollutionFactor * 0.5);
                
                // 应用波纹到主纹理采样
                float2 distortedUV = i.uv + waveOffset * _WaveDistortion;
                fixed4 sceneColor = tex2D(_MainTex, distortedUV);
                
                // 计算水体基础颜色
                fixed4 baseWaterColor = lerp(_ClearWaterColor, _PollutedWaterColor, _PollutionFactor * 0.8f);
                
                // 应用水体颜色混合
                float blendFactor = smoothstep(0.2, 0.7, _PollutionFactor);
                fixed4 underwaterColor = lerp(sceneColor, baseWaterColor, blendFactor * 0.6f);
                
                // 应用浑浊色调
                float3 darkGreenTint = float3(0.1, 0.25, 0.1);
                underwaterColor.rgb = lerp(
                    underwaterColor.rgb, 
                    darkGreenTint, 
                    blendFactor * 0.5f
                );
                
                // 减少亮度降低程度
                underwaterColor.rgb *= lerp(1.0, 0.85f, blendFactor);
                
                // 减少绿色通道增强
                underwaterColor.g *= lerp(1.0, 1.1f, blendFactor);
                
                // 减少暗角效果
                float2 uv = i.uv * 2.0 - 1.0;
                float vignette = 1.0 - dot(uv, uv) * 0.15f * blendFactor;
                underwaterColor.rgb *= vignette;
                
                // 保持场景对比度
                float contrast = lerp(1.0, 1.2f, blendFactor * 0.3f);
                underwaterColor.rgb = (underwaterColor.rgb - 0.5f) * contrast + 0.5f;
                
                return underwaterColor;
            }
            ENDCG
        }
    }
}