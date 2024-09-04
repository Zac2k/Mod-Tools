
Shader "Hidden/CubeRT"
{
    Properties
    {
        _CubeMap("Cubemap", CUBE) = "" {}
        _FaceIndex("Face Index (0-5)", Range(0, 5)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            samplerCUBE _CubeMap;
            int _FaceIndex;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float3 direction;

                switch (_FaceIndex)
                {
                    case 0: // +X
                        direction = float3(1, -i.uv.y * 2 + 1, -i.uv.x * 2 + 1);
                        break;
                    case 1: // -X
                        direction = float3(-1, -i.uv.y * 2 + 1, i.uv.x * 2 - 1);
                        break;
                    case 2: // +Y
                        direction = float3(i.uv.x * 2 - 1, 1, i.uv.y * 2 - 1);
                        break;
                    case 3: // -Y
                        direction = float3(i.uv.x * 2 - 1, -1, -i.uv.y * 2 + 1);
                        break;
                    case 4: // +Z
                        direction = float3(i.uv.x * 2 - 1, -i.uv.y * 2 + 1, 1);
                        break;
                    case 5: // -Z
                        direction = float3(-i.uv.x * 2 + 1, -i.uv.y * 2 + 1, -1);
                        break;
                }

                float4 albedo = texCUBE(_CubeMap, direction);
                return albedo;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}