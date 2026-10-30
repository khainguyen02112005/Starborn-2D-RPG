Shader "Story/Ending3SignalDeletion"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [PerRendererData] _RendererColor ("Renderer tint", Color) = (1,1,1,1)
        [PerRendererData] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("External alpha enabled", Float) = 0
        _Clock ("Story time", Float) = 0
        _StartAt ("Object signal failure", Float) = 0
        _Duration ("Ribbon destruction duration", Float) = 2.85
        _Seed ("Object seed", Float) = 1
        _Chunked ("Terrain patches", Float) = 0
        _ShiftUV ("Allow sprite displacement", Float) = 1
        _SourceRect ("Sprite UV rectangle", Vector) = (0,0,1,1)
        _ObjectRect ("Whole object world rectangle", Vector) = (0,0,1,1)
        _LineCount ("Horizontal ribbons", Float) = 16
        _Progress ("Existing fight erasure", Float) = 0
        _PulseCount ("Existing fight holes", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" "IgnoreProjector"="True" }
        Cull Off Lighting Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex, _AlphaTex;
            float4 _Color,_RendererColor,_Flip,_SourceRect,_ObjectRect;
            float _Clock,_StartAt,_Duration,_Seed,_Chunked,_ShiftUV,_EnableExternalAlpha,_Progress,_PulseCount,_LineCount;
            float4 _ErasePulses[32];
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float localTime(float2 world) { return _Clock-_StartAt-_Chunked*hash(floor(world/float2(4,3))+_Seed)*.9; }
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float2 world:TEXCOORD1; float4 color:COLOR; };
            v2f vert(appdata v)
            {
                v.vertex.xy*=_Flip.xy;float4 world=mul(unity_ObjectToWorld,v.vertex);
                v2f o;o.vertex=UnityWorldToClipPos(world.xyz);o.uv=v.uv;o.world=world.xy;o.color=v.color*_Color*_RendererColor;return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float time=localTime(i.world);float phase=time/max(.01,_Duration);
                if(phase>=1)clip(-1);
                float2 objectUV=(i.world-_ObjectRect.xy)/_ObjectRect.zw;
                float2 ribbonUV=lerp(objectUV,frac(i.world/float2(4,3)),_Chunked);
                float row=floor(ribbonUV.y*_LineCount);
                float patchSeed=_Seed+_Chunked*hash(floor(i.world/float2(4,3)))*37;
                // A stable deadline per horizontal ribbon, with a small delay
                // for its two-to-four coarse segments. Never random pixel dust.
                float deadline=.16+.72*hash(float2(row,patchSeed));
                float segment=floor(ribbonUV.x*4);
                deadline+=.08*hash(float2(row*7+segment,patchSeed+19));
                clip(deadline-phase);
                float before=deadline-phase;
                float failing=step(0,phase)*(1-step(.2,before));
                float tick=floor(time*13);
                float spark=step(.48,hash(float2(row+patchSeed,tick)));
                float flash=failing*max(spark,1-smoothstep(0,.035,before));
                float width=_SourceRect.z-_SourceRect.x;
                float shift=(hash(float2(row+patchSeed,tick+9))-.5)*width*.09*flash*_ShiftUV;
                float2 uv=i.uv;uv.x=clamp(uv.x+shift,_SourceRect.x+.00001,_SourceRect.z-.00001);
                fixed4 c=tex2D(_MainTex,uv)*i.color;
                c.a*=lerp(1,tex2D(_AlphaTex,uv).r,_EnableExternalAlpha);
                // Only the failing ribbons stutter WHITE. Surviving picture
                // keeps its exact colours/opacity; missing ribbons stay missing.
                c.rgb=lerp(c.rgb,float3(1,1,1),flash);
                // Retain pre-existing cursor-fight holes without changing ending 2.
                if(_Progress>0||_PulseCount>.5)
                {
                    float2 px=floor(i.world*24);float n=hash(px);float old=lerp(n,hash(floor(px/6)),.58)-_Progress*1.04;
                    [loop]for(int p=0;p<32;p++){if(p>=(int)_PulseCount)break;old=min(old,(distance((px+.5)/24,_ErasePulses[p].xy)-_ErasePulses[p].z+(n-.5)*.22)*2);}clip(old-.0001);
                }
                return c;
            }
            ENDCG
        }
    }
}
