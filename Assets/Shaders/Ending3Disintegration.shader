Shader "Story/Ending3Disintegration"
{
    Properties
    {
        [PerRendererData] _MainTex ("Native surface", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [PerRendererData] _RendererColor ("Renderer Tint", Color) = (1,1,1,1)
        [PerRendererData] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
        _Completion ("Completion", Range(0,1)) = 0
        _Radius ("Erosion front", Float) = -3.4
        _Origin ("Origin", Vector) = (0,0,0,0)
        _Debris ("Detached surface fragment", Float) = 0
        _Progress ("Existing cursor-battle erasure", Float) = 0
        _PulseCount ("Existing holes", Float) = 0
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
            float4 _Color, _RendererColor, _Flip, _Origin;
            float _Completion, _Radius, _Debris, _Progress, _PulseCount, _EnableExternalAlpha;
            float4 _ErasePulses[32];
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float2 world:TEXCOORD1; float4 color:COLOR; };
            v2f vert(appdata v)
            {
                v.vertex.xy *= _Flip.xy;
                v2f o; o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;
                o.world=mul(unity_ObjectToWorld,v.vertex).xy;o.color=v.color*_Color*_RendererColor;return o;
            }
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float grain(float2 p)
            {
                float2 cell=floor(p), f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(hash(cell),hash(cell+float2(1,0)),f.x),
                    lerp(hash(cell+float2(0,1)),hash(cell+1),f.x),f.y);
            }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c=tex2D(_MainTex,i.uv)*i.color;
                c.a*=lerp(1,tex2D(_AlphaTex,i.uv).r,_EnableExternalAlpha);
                clip(c.a-.003);
                if(_Debris>.5) return c;
                // Carry existing cursor-fight holes into ending 3, without restoring
                // scenery or changing the shader used by ending 2 itself.
                if(_Progress>0 || _PulseCount>.5)
                {
                    float2 px=floor(i.world*24);float n=hash(px);
                    float old=lerp(n,hash(floor(px/6)),.58)-_Progress*1.04;
                    [loop] for(int p=0;p<32;p++)
                    {
                        if(p>=(int)_PulseCount) break;
                        old=min(old,(distance((px+.5)/24,_ErasePulses[p].xy)-_ErasePulses[p].z+(n-.5)*.22)*2);
                    }
                    clip(old-.0001);
                }
                if(_Completion<=.0001) return c;
                float2 pixel=floor(i.world*32)/32;
                float2 d=pixel-_Origin.xy;
                // A connected, irregular front. Noise is anchored to the surface,
                // not animated every frame: eroded pixels never flicker back in.
                // Broad material-sized islands peel at different times. This breaks
                // the perfect-circle silhouette while keeping temporal coherence.
                float rough=(grain(pixel*.27)-.5)*5.2+(grain(pixel*1.6)-.5)*.8;
                float front=length(d*float2(1,1.08))-_Radius+rough;
                float chips=hash(floor(i.world*32));
                float remaining=front+(chips-.5)*.28;
                remaining=min(remaining,1-_Completion-chips*.055);
                clip(remaining-.00001);
                // Subtle drying/ash at the breaking edge; retain native colours.
                float edge=1-smoothstep(.02,.42,front);
                float grey=dot(c.rgb,float3(.3,.59,.11));
                c.rgb=lerp(c.rgb,lerp(c.rgb,float3(grey,grey,grey),.38)*.79,edge*.65);
                return c;
            }
            ENDCG
        }
    }
}
