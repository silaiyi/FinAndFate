Shader "Custom/BubbleShader" {
    Properties {
        _Color ("Color", Color) = (1,1,1,0.5)
        _Fresnel ("Fresnel", Range(0, 5)) = 1
    }
    SubShader {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        
        CGPROGRAM
        #pragma surface surf Lambert alpha
        
        struct Input {
            float3 viewDir;
        };
        
        fixed4 _Color;
        float _Fresnel;
        
        void surf (Input IN, inout SurfaceOutput o) {
            o.Albedo = _Color.rgb;
            float fresnel = 1.0 - saturate(dot(normalize(IN.viewDir), o.Normal));
            o.Alpha = _Color.a * pow(fresnel, _Fresnel);
        }
        ENDCG
    }
    FallBack "Diffuse"
}