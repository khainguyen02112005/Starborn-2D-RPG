Shader "UI/CinematicDigitalCorruption"
{
    Properties
    {
        [PerRendererData] _MainTex ("Live signal", 2D) = "black" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _SignalTime ("Signal time", Float) = 0
        _Strength ("Interference", Range(0,1)) = 0
        _Blur ("Signal softness", Float) = 0
        _SignalLoss ("Broken sync", Float) = 0
        _NoiseOnly ("Foreground grain", Float) = 0
        _GlassOnly ("Glass fragments", Float) = 0
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            float _SignalTime, _Strength, _Blur, _SignalLoss, _NoiseOnly, _GlassOnly;
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color*_Color; return o; }
            float signalNoise(float2 p) { return frac(sin(dot(p,float2(93.989,67.345)))*41271.713); }
            float3 picture(float2 uv, float softness)
            {
                float2 px = abs(_MainTex_TexelSize.xy) * softness;
                float3 c = tex2D(_MainTex,saturate(uv)).rgb * .40;
                c += tex2D(_MainTex,saturate(uv+float2(px.x,0))).rgb * .20;
                c += tex2D(_MainTex,saturate(uv-float2(px.x,0))).rgb * .20;
                c += tex2D(_MainTex,saturate(uv+float2(0,px.y))).rgb * .10;
                c += tex2D(_MainTex,saturate(uv-float2(0,px.y))).rgb * .10;
                return c;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                if (_GlassOnly > .5)
                    return i.uv.x < 0 ? i.color : tex2D(_MainTex,i.uv)*i.color;

                float t = _SignalTime;
                float frame = floor(t*30);
                float signalLine = floor(i.uv.y*540);
                float grain = signalNoise(float2(floor(i.uv.x*960)+frame*17,signalLine+frame*37));
                float rolling = exp(-pow((frac(i.uv.y+t*.18)-.5)*25,2));
                float fineLine = .5+.5*sin(i.uv.y*3392.92);
                float dropout = step(.89,signalNoise(float2(floor(i.uv.y*38),floor(t*9))));
                float strength = _Strength;
                if (_NoiseOnly > .5)
                {
                    // Fine luma snow and moving sync bands, never a tiled colour sheet.
                    float alpha = strength*(.09+grain*.14+rolling*.17+dropout*.08);
                    alpha += _SignalLoss*strength*dropout*.2;
                    float level = saturate(.14+grain*.55-fineLine*.12);
                    return fixed4(level,level*1.015,level*1.03,saturate(alpha)*i.color.a);
                }

                float jitter = signalNoise(float2(signalLine,frame))*2-1;
                jitter *= step(.82,abs(jitter)) * .018 * strength;
                float shake = (signalNoise(float2(frame,41))-0.5)*.009*strength;
                float tear = (signalNoise(float2(floor(i.uv.y*38),floor(t*9)))-.5)*dropout*.10*strength;
                float2 uv = i.uv;
                uv.x += jitter+shake+tear+rolling*.012*strength;
                uv.y += sin(t*13)*.0018*strength;
                float drift = (1+sin(i.uv.y*13+t*5))*.0024*strength;
                float3 rgb = picture(uv,_Blur*1.7);
                rgb.r = lerp(rgb.r,picture(uv+float2(drift,0),_Blur).r,.75);
                rgb.b = lerp(rgb.b,picture(uv-float2(drift,0),_Blur).b,.75);
                float luma = dot(rgb,float3(.299,.587,.114));
                rgb = lerp(rgb,luma.xxx,strength*.28);
                rgb *= 1-strength*(.08+fineLine*.09+rolling*.12);
                rgb += (grain-.5)*strength*.09;
                rgb *= 1-_SignalLoss*dropout*strength*.48;
                return fixed4(saturate(rgb)*i.color.rgb,i.color.a);
            }
            ENDCG
        }
    }
}
