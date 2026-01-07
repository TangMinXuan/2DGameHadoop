Shader "Hadoop/IrisWipeUI"
{
    Properties
    {
        _Center ("Center", Vector) = (0.5,0.5,0,0)
        _Radius ("Radius", Float) = 1.0
        _Softness ("Softness", Float) = 0.02
        _Color ("Color", Color) = (0,0,0,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Center; // xy used
                float _Radius;
                float _Softness;
                float4 _Color;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                // Screen-position UV in 0-1
                float2 uv = IN.uv;

                // delta from center
                float2 delta = uv - _Center.xy;

                // aspect correction using ScreenParams (x=width, y=height)
                float aspect = _ScreenParams.x / _ScreenParams.y;
                delta.x *= aspect;

                float dist = length(delta);

                // smoothstep for soft edge
                float alpha = smoothstep(_Radius, _Radius + _Softness, dist);

                float4 col = float4(_Color.rgb, alpha * _Color.a);
                return col;
            }
            ENDHLSL
        }
    }
}
