Shader "Custom/FishingNet"
{
    Properties
    {
        _MainColor("Main Color", Color) = (0.5, 0.8, 1.0, 0.3)
        _EdgeColor("Edge Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _PatternScale("Pattern Scale", Float) = 5.0
        _EdgeWidth("Edge Width", Range(0, 0.1)) = 0.02
        _GlowIntensity("Glow Intensity", Range(0, 5)) = 1.0
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float3 viewDir : TEXCOORD3;
            };
            
            float4 _MainColor;
            float4 _EdgeColor;
            float _PatternScale;
            float _EdgeWidth;
            float _GlowIntensity;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.uv = v.uv * _PatternScale;
                o.viewDir = normalize(WorldSpaceViewDir(v.vertex));
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // 修正的网格图案计算
                float2 fractional = frac(i.uv);
                float2 centered = fractional - 0.5;
                float gridValue = max(abs(centered.x), abs(centered.y));
                
                // 边缘检测
                float edge = 1.0 - smoothstep(0.0, _EdgeWidth, gridValue);
                
                // 菲涅尔效果
                float fresnel = pow(1.0 - saturate(dot(i.normal, i.viewDir)), 2.0);
                
                // 组合颜色
                fixed4 color = _MainColor;
                color.rgb = lerp(color.rgb, _EdgeColor.rgb, edge);
                color.a = _MainColor.a * (0.3 + fresnel * 0.7);
                
                // 添加发光效果
                color.rgb *= (1.0 + fresnel * _GlowIntensity);
                
                return color;
            }
            ENDCG
        }
    }
}