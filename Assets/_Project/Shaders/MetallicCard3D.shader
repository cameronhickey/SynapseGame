Shader "Custom/MetallicCard3D"
{
    Properties
    {
        _Color ("Base Color", Color) = (0.75, 0.75, 0.8, 1)
        _Metallic ("Metallic", Range(0,1)) = 0.9
        _Smoothness ("Smoothness", Range(0,1)) = 0.8
        _FresnelPower ("Fresnel Power", Range(1,5)) = 2.5
        _FresnelColor ("Fresnel Color", Color) = (1, 1, 1, 1)
        _ShimmerSpeed ("Shimmer Speed", Range(0,5)) = 1.0
        _ShimmerIntensity ("Shimmer Intensity", Range(0,1)) = 0.3
        _CornerRadius ("Corner Radius", Range(0,0.5)) = 0.08
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        struct Input
        {
            float3 viewDir;
            float3 worldPos;
            float3 worldNormal;
            float2 uv_MainTex;
        };

        fixed4 _Color;
        half _Metallic;
        half _Smoothness;
        half _FresnelPower;
        fixed4 _FresnelColor;
        half _ShimmerSpeed;
        half _ShimmerIntensity;
        half _CornerRadius;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Base color
            fixed4 c = _Color;
            
            // Fresnel effect for edge highlights
            float fresnel = pow(1.0 - saturate(dot(IN.viewDir, IN.worldNormal)), _FresnelPower);
            c.rgb += _FresnelColor.rgb * fresnel * 0.5;
            
            // Animated shimmer effect
            float shimmerPhase = _Time.y * _ShimmerSpeed;
            float shimmer = sin(IN.worldPos.x * 10 + IN.worldPos.y * 10 + shimmerPhase) * 0.5 + 0.5;
            shimmer = shimmer * shimmer; // Make it more subtle
            c.rgb += shimmer * _ShimmerIntensity * _FresnelColor.rgb;
            
            // Subtle color variation based on viewing angle
            float angleVar = dot(IN.viewDir, float3(0, 1, 0)) * 0.1;
            c.rgb += angleVar;
            
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Standard"
}
