Shader "Custom/NetParticle"
{
    Properties
    {
        _MainTex("Particle Texture", 2D) = "white" {}
        _TintColor("Tint Color", Color) = (0.5, 0.8, 1.0, 1.0)
        _GlowIntensity("Glow Intensity", Range(0, 5)) = 2.0
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha One
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
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _TintColor;
            float _GlowIntensity;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.viewDir = normalize(WorldSpaceViewDir(v.vertex));
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                fixed4 color = tex * i.color * _TintColor;
                
                // 添加发光效果
                color.rgb *= _GlowIntensity;
                
                return color;
            }
            ENDCG
        }
    }
}