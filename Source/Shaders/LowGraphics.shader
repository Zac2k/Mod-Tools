Shader "ZicZac/LowGraphic/Opaque"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        _SpecColor ("Spec Color", Color) = (1,1,1,1)
        _Emission ("Emissive Color", Color) = (0,0,0,0)
        [PowerSlider(5.0)] _Shininess ("Shininess", Range (0.01, 1)) = 0.7
        [noscaleoffset]_MainTex ("Base (RGB)", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 400

        // Non-lightmapped Pass
        Pass
        {
            Tags { "LightMode" = "Vertex" }

            Material
            {
                Diffuse [_Color]
                Ambient [_AmbientMult]
                Shininess [_Shininess]
                Specular [_SpecColor]
                Emission [_Emission]
            }
            Lighting On
            SeparateSpecular On

            SetTexture [_MainTex]
            {
                constantColor (1,1,1,1)
                Combine texture * primary DOUBLE, constant  // UNITY_OPAQUE_ALPHA_FFP
            }
        }

        // Lightmapped Pass
        Pass
        {
            Tags { "LIGHTMODE" = "VertexLM" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag noforwardadd interpolateview approxview nometa nodynlightmap exclude_path:deferred exclude_path:prepass noshadow
            #pragma target 2.0
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #pragma multi_compile_fog
            #define USING_FOG (defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2))

            float4 _MainTex_ST;

            struct appdata
            {
                float3 pos : POSITION;
                float3 uv1 : TEXCOORD1;
                float3 uv0 : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                fixed fog : TEXCOORD2;
                float4 pos : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
            UNITY_DEFINE_INSTANCED_PROP(fixed, _Shininess)
            UNITY_INSTANCING_BUFFER_END(Props)

            v2f vert(appdata IN)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.uv0 = IN.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
                o.uv1 = IN.uv0.xy * _MainTex_ST.xy + _MainTex_ST.zw;

                float3 eyePos = UnityObjectToViewPos(float4(IN.pos, 1));
                float fogCoord = length(eyePos.xyz);
                UNITY_CALC_FOG_FACTOR_RAW(fogCoord);
                o.fog = saturate(unityFogFactor);

                o.pos = UnityObjectToClipPos(IN.pos);
                return o;
            }

            sampler2D _MainTex;

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 col, tex;

                half4 bakedColorTex = UNITY_SAMPLE_TEX2D(unity_Lightmap, IN.uv0.xy);
                col.rgb = DecodeLightmap(bakedColorTex);

                tex = tex2D(_MainTex, IN.uv1.xy);
                col.rgb = tex.rgb * col.rgb;
                col.a = 1;

                col.rgb = lerp(unity_FogColor.rgb, col.rgb, IN.fog);

                return col;
            }
            ENDCG
        }

        // Shadow Caster Pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct v2f
            {
                V2F_SHADOW_CASTER;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
}
