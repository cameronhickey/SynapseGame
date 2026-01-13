Shader "UI/MetallicCard"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _TopColor ("Top Color", Color) = (0.15, 0.15, 0.6, 1)
        _BottomColor ("Bottom Color", Color) = (0.05, 0.05, 0.3, 1)
        _HighlightColor ("Highlight Color", Color) = (0.4, 0.4, 1, 1)
        _HighlightPos ("Highlight Position", Range(-0.5, 1.5)) = 0.5
        _HighlightWidth ("Highlight Width", Range(0.05, 0.5)) = 0.15
        _HighlightIntensity ("Highlight Intensity", Range(0, 2)) = 0.8
        _EdgeGlow ("Edge Glow", Range(0, 1)) = 0
        _EdgeGlowColor ("Edge Glow Color", Color) = (0.5, 0.5, 1, 1)
        _Bevel ("Bevel Amount", Range(0, 0.1)) = 0.02
        
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TopColor;
            fixed4 _BottomColor;
            fixed4 _HighlightColor;
            float _HighlightPos;
            float _HighlightWidth;
            float _HighlightIntensity;
            float _EdgeGlow;
            fixed4 _EdgeGlowColor;
            float _Bevel;
            float4 _ClipRect;
            float4 _MainTex_ST;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                
                // Base gradient (top to bottom)
                fixed4 baseColor = lerp(_BottomColor, _TopColor, uv.y);
                
                // Diagonal highlight sweep
                float diag = (uv.x + uv.y) * 0.5;
                float highlightDist = abs(diag - _HighlightPos);
                float highlight = 1.0 - saturate(highlightDist / _HighlightWidth);
                highlight = highlight * highlight * _HighlightIntensity;
                baseColor.rgb += _HighlightColor.rgb * highlight;
                
                // Bevel effect (lighter at top-left edges, darker at bottom-right)
                float bevelLight = smoothstep(0, _Bevel, uv.x) * smoothstep(0, _Bevel, 1.0 - uv.y);
                float bevelDark = smoothstep(0, _Bevel, 1.0 - uv.x) * smoothstep(0, _Bevel, uv.y);
                baseColor.rgb += 0.1 * (1.0 - bevelLight);
                baseColor.rgb -= 0.08 * (1.0 - bevelDark);
                
                // Edge glow
                float edgeDist = min(min(uv.x, 1.0 - uv.x), min(uv.y, 1.0 - uv.y));
                float edgeGlow = (1.0 - smoothstep(0, 0.05, edgeDist)) * _EdgeGlow;
                baseColor.rgb += _EdgeGlowColor.rgb * edgeGlow;
                
                // Subtle noise/grain for metallic texture
                float noise = frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
                baseColor.rgb += (noise - 0.5) * 0.02;
                
                // Apply vertex color and alpha
                baseColor *= IN.color;
                
                // UI clipping
                baseColor.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                
                return baseColor;
            }
            ENDCG
        }
    }
}
