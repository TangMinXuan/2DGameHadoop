Shader "Custom/Metaball2D"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        _Color ("Water Color", Color) = (0.2, 0.5, 1, 1) // 水的颜色
        _Stroke ("Stroke Width", Range(0, 0.1)) = 0.02   // 描边宽度
        _StrokeColor ("Stroke Color", Color) = (1, 1, 1, 1) // 描边颜色
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha // 开启透明混合

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
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Cutoff;
            float4 _Color;
            float _Stroke;
            float4 _StrokeColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 读取 Render Texture 里的颜色 (其实就是那一堆模糊球的叠加)
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // alpha 通常存储在 r 或者 a 通道里，取决于之前的设置
                // 我们假设越重叠的地方 alpha/亮度 越高
                float alpha = col.a; 

                // 核心逻辑: 阈值切割
                if(alpha < _Cutoff)
                {
                    // 如果亮度不够，直接扔掉 (透明)
                    discard; 
                }
                
                // 可选的高级效果：描边
                // 如果刚好在阈值边缘，就给个描边色
                if(alpha < _Cutoff + _Stroke)
                {
                    return _StrokeColor;
                }

                //剩下的部分全涂成水颜色
                return _Color;
            }
            ENDCG
        }
    }
}