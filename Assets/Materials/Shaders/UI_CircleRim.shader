Shader "UI/CircleRim"
{
    Properties { _MainTex ("Texture", 2D) = "white" {} _Color ("Rim Color", Color) = (0.1,0.12,0.15,1) _Inner ("Inner Radius", Range(0.4,0.5)) = 0.482 }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color; float _Inner;
            struct appdata_t { float4 vertex : POSITION; float2 texcoord : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            v2f vert (appdata_t v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.texcoord; o.color = v.color * _Color; return o; }
            fixed4 frag (v2f i) : SV_Target
            {
                float r = length(i.uv - float2(0.5,0.5));
                float aa = max(fwidth(r), 0.0001);
                float outer = 1.0 - smoothstep(0.5 - aa, 0.5 + aa, r);
                float inner = smoothstep(_Inner - aa, _Inner + aa, r);
                fixed4 c = i.color;
                c.a *= outer * inner;
                return c;
            }
            ENDCG
        }
    }
}