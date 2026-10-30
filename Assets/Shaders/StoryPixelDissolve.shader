Shader "Story/WorldPixelDissolve"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [PerRendererData] _RendererColor ("Renderer Tint", Color) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
        _Progress ("World Erased", Range(0,1)) = 0
        _PulseCount ("Pulse Count", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off Lighting Off ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex, _AlphaTex;
            float4 _Color, _RendererColor;
            float _Progress, _PulseCount, _EnableExternalAlpha;
            float4 _ErasePulses[32];
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float2 world:TEXCOORD1; float4 color:COLOR; };
            v2f vert(appdata v)
            {
                v2f o; o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;
                o.world=mul(unity_ObjectToWorld,v.vertex).xy;o.color=v.color*_Color*_RendererColor;return o;
            }
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c=tex2D(_MainTex,i.uv)*i.color;
                c.a*=lerp(1,tex2D(_AlphaTex,i.uv).r,_EnableExternalAlpha);
                if(_Progress<=0 && _PulseCount<.5) return c;
                float2 pixel=floor(i.world*24.0);
                float noise=hash(pixel);
                float coarse=hash(floor(pixel/6.0));
                float remaining=lerp(noise,coarse,.58)-_Progress*1.04;
                [loop] for(int p=0;p<32;p++)
                {
                    if(p>=(int)_PulseCount) break;
                    float radius=_ErasePulses[p].z;
                    float d=distance((pixel+.5)/24.0,_ErasePulses[p].xy);
                    remaining=min(remaining,(d-radius+(noise-.5)*.22)*2);
                }
                clip(remaining-.0001);
                // A narrow chipped edge, not a full-screen purple overlay.
                float edge=(1-smoothstep(.005,.065,remaining))*step(.001,_Progress+_PulseCount);
                c.rgb=lerp(c.rgb,float3(.47,.28,.54),edge*.55);
                return c;
            }
            ENDCG
        }
    }
}
