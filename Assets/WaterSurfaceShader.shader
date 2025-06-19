Shader "Custom/WaterSurfaceShader"
{
    Properties
    {
        _Color ("Main Color", Color) = (0.2, 0.4, 0.7, 0.6)
        _RippleTex ("Ripple Texture", 2D) = "white" {}
        _RippleSpeed ("Ripple Speed", Float) = 0.5
        _RippleScale ("Ripple Scale", Float) = 1.0
        _RippleOffset ("Ripple Offset", Float) = 0.0
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        LOD 200
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        
        CGPROGRAM
        #pragma surface surf Lambert alpha
        
        struct Input
        {
            float2 uv_RippleTex;
        };
        
        fixed4 _Color;
        sampler2D _RippleTex;
        float _RippleSpeed;
        float _RippleScale;
        float _RippleOffset;
        
        void surf (Input IN, inout SurfaceOutput o)
        {
            // 计算涟漪UV
            float2 rippleUV = IN.uv_RippleTex * _RippleScale;
            rippleUV.x += _RippleOffset;
            rippleUV.y += _RippleOffset * 0.5;
            
            // 采样涟漪纹理
            fixed4 ripple = tex2D(_RippleTex, rippleUV);
            
            // 应用颜色和透明度
            o.Albedo = _Color.rgb;
            o.Alpha = _Color.a * (0.8 + ripple.r * 0.2);
        }
        ENDCG
    }
    FallBack "Diffuse"
}