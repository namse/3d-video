Shader "Render Depth" {
    Properties{
        [NoScaleOffset] _MainTex("Texture", 2D) = "white" {}
    }
    SubShader{
        Tags { "RenderType" = "Opaque" }
        Pass {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct v2f {
                float4 pos : SV_POSITION;
                float2 depth : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            v2f vert(appdata_base v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                COMPUTE_EYEDEPTH(o.depth);
                o.uv = v.texcoord;
                return o;
            }

            sampler2D _MainTex;

            fixed4 frag(v2f i) : SV_Target {
                fixed4 color = tex2D(_MainTex, i.uv);
                float depth = DECODE_EYEDEPTH(i.depth);

                color.a = depth;
                return color;
            }
            ENDCG
        }
    }
}