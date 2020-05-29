Shader "Render Depth" {
    Properties{
    }
    SubShader{
        Tags { "RenderType" = "Opaque" }
        LOD 200

        Pass {
            Lighting Off Fog { Mode Off }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma fragmentoption ARB_precision_hint_fastest

            struct a2v {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                half dist : TEXCOORD0;
            };

            v2f vert(a2v v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dist = mul(UNITY_MATRIX_IT_MV, v.vertex).z;
                return o;
            }

            fixed4 frag(v2f i) : COLOR {
                return fixed4(i.dist, i.dist, i.dist, 1);
            }
            ENDCG
        }
    }
        FallBack Off
}