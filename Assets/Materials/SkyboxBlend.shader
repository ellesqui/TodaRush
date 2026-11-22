Shader "TodaRush/Skybox Blend"
{
    // Blends two cubemap skies directly (no black fade). _Blend 0 = Sky A, 1 = Sky B.
    Properties
    {
        _Tex ("Sky A (Cubemap)", Cube) = "" {}
        _Tex2 ("Sky B (Cubemap)", Cube) = "" {}
        _Blend ("Blend", Range(0, 1)) = 0
        _Tint ("Tint Color", Color) = (0.5, 0.5, 0.5, 0.5)
        _Exposure ("Exposure", Range(0, 8)) = 1.0
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            samplerCUBE _Tex;
            samplerCUBE _Tex2;
            half4 _Tex_HDR;
            half4 _Tint;
            half _Exposure;
            half _Blend;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half3 colA = DecodeHDR(texCUBE(_Tex, i.dir), _Tex_HDR);
                half3 colB = DecodeHDR(texCUBE(_Tex2, i.dir), _Tex_HDR);
                half3 col = lerp(colA, colB, _Blend);
                col = col * _Tint.rgb * unity_ColorSpaceDouble.rgb;
                col *= _Exposure;
                return half4(col, 1);
            }
            ENDCG
        }
    }
}
