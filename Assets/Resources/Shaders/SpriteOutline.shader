// 도트 스프라이트 윤곽선 (1.8.7~): 투명한 칸 중 옆 칸이 칠해진 곳을 윤곽선 색으로 칠함
// Sprites/Default 와 같은 방식(정점 색 · 반전 · 사전 곱 알파)이라 피격 번쩍임 · 투명도 효과는 그대로
Shader "SoulSaver/SpriteOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline", Color) = (0.06,0.04,0.09,0.9)
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment Frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            float4 _MainTex_TexelSize;
            fixed4 _OutlineColor;

            fixed4 Frag (v2f IN) : SV_Target
            {
                // 판정은 그림 자체의 알파로 (무적 깜빡임처럼 반투명해져도 몸이 윤곽선 색으로 바뀌지 않게)
                fixed4 tex = SampleSpriteTexture(IN.texcoord);
                fixed4 c = tex * IN.color;
                if (tex.a < 0.5)
                {
                    float2 t = _MainTex_TexelSize.xy;
                    float a = max(max(SampleSpriteTexture(IN.texcoord + float2(t.x, 0)).a, SampleSpriteTexture(IN.texcoord - float2(t.x, 0)).a),
                                  max(SampleSpriteTexture(IN.texcoord + float2(0, t.y)).a, SampleSpriteTexture(IN.texcoord - float2(0, t.y)).a));
                    if (a > 0.5)
                    {
                        fixed4 o = _OutlineColor;
                        o.a *= IN.color.a;
                        o.rgb *= o.a;
                        return o;
                    }
                }
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
