Shader "UI/CircleMinimap"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _BorderWidth ("Border Width (UV)", Range(0, 0.25)) = 0.07
        _GradTop ("Border Top (Yellow)", Color) = (1, 0.9, 0, 1)
        _GradMid ("Border Middle (Orange)", Color) = (1, 0.45, 0, 1)
        _GradBottom ("Border Bottom (Red)", Color) = (1, 0, 0, 1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }

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
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _MainTex_ST;
            float _BorderWidth;
            fixed4 _GradTop;
            fixed4 _GradMid;
            fixed4 _GradBottom;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color : COLOR;
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 d = i.texcoord - float2(0.5, 0.5);
                float r = length(d);
                float aa = max(fwidth(r) * 2.0, 0.0065);

                // Outer circle alpha (anti-aliased)
                float outer = 1.0 - smoothstep(0.5 - aa, 0.5 + aa, r);

                fixed4 c = tex2D(_MainTex, i.texcoord) * i.color;

                // Border ring: yellow (top) -> orange (middle) -> red (bottom)
                float innerEdge = 0.5 - _BorderWidth;
                float ring = smoothstep(innerEdge - aa, innerEdge + aa, r) * outer;

                float s = saturate(0.5 - d.y);
                fixed3 grad = s < 0.5
                    ? lerp(_GradTop.rgb, _GradMid.rgb, s * 2.0)
                    : lerp(_GradMid.rgb, _GradBottom.rgb, (s - 0.5) * 2.0);

                c.rgb = lerp(c.rgb, grad, ring);
                c.a = lerp(c.a, 1.0, ring);
                c.a *= outer;
                return c;
            }
            ENDCG
        }
    }
}
