Shader "RPG/SPUM Signal Fragments"
{
    Properties
    {
        [PerRendererData] _MainTex ("SPUM sprite", 2D) = "white" {}
        _Dissolve ("Pixel dissolution", Range(0,1)) = 0
        _Phase ("Animation time", Float) = 0
        _Flicker ("Signal flicker", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off Lighting Off ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Dissolve, _Phase, _Flicker;
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o; }
            fixed4 frag(v2f i) : SV_Target
            {
                float row = floor(i.uv.y / max(.0001,_MainTex_TexelSize.y*3));
                float pulse = hash(float2(row,floor(_Phase*14)));
                float2 uv = i.uv;
                uv.x += pulse>.82 ? (pulse-.82)*.03*_Flicker : 0;
                fixed4 color = tex2D(_MainTex,uv)*i.color;
                float2 cell = floor(i.uv / max(_MainTex_TexelSize.xy*2,float2(.0001,.0001)));
                color.a *= step(_Dissolve*.999,hash(cell));
                return color;
            }
            ENDCG
        }
    }
}
