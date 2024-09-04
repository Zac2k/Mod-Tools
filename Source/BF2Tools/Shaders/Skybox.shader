Shader "BF2/Skybox" {
    Properties {
       // _Tint ("Tint Color", Color) = (.5, .5, .5, .5)
        //[Gamma] _Exposure ("Exposure", Range(0, 8)) = 1.0
        _Rotation ("Rotation", Range(0, 360)) = 0
        [NoScaleOffset] _MainTex ("Spherical (HDR)", 2D) = "grey" {}
        //[NoScaleOffset] _CloudTex ("Cloud Texture", 2D) = "white" {}
        //_CloudTint ("Cloud Tint Color", Color) = (1, 1, 1, 1)
        //_CloudScale("Cloud Scale", Int) = 1
        //_CloudSpeed ("Cloud Speed", Range(0, 1)) = 0.1
    }

    SubShader {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            sampler2D _MainTex;
           // sampler2D _CloudTex;
            //float4 _MainTex_TexelSize;
            half4 _MainTex_HDR;
            //half4 _Tint;
            //half4 _CloudTint;
            //half _Exposure;
            float _Rotation;
            //float _CloudSpeed;
            //float _CloudScale;

            struct appdata_t {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float3 texcoord : TEXCOORD0;
                float3 cloudTexcoord : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float3 RotateAroundYInDegrees(float3 vertex, float degrees) {
                float alpha = degrees * UNITY_PI / 180.0;
                float sina, cosa;
                sincos(alpha, sina, cosa);
                float2x2 m = float2x2(cosa, -sina, sina, cosa);
                return float3(mul(m, vertex.xz), vertex.y).xzy;
            }

            v2f vert(appdata_t v) {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 rotated = RotateAroundYInDegrees(v.vertex, _Rotation);
                o.vertex = UnityObjectToClipPos(rotated);
                o.texcoord = v.vertex.xyz;
                o.cloudTexcoord = v.vertex.xyz;
                return o;
            }

            inline float2 ToHemisphereCoords(float3 coords) {
                float3 normalizedCoords = normalize(coords);
                float latitude = acos(normalizedCoords.y) / UNITY_PI;
                float longitude = atan2(normalizedCoords.z, normalizedCoords.x) / (2.0f * UNITY_PI);
                return float2(0.5f - longitude, latitude);
            }

            /*inline float2 ToTopPlaneCoords(float3 coords) {
                        // Determine the primary axis of the normal
            float3 absn = abs(coords);
            float3 absdir = absn > float3(max(absn.y,absn.z), max(absn.x,absn.z), max(absn.x,absn.y)) ? 1 : 0;
            // Convert the normal to a local face texture coord [-1,+1], note that tcAndLen.z==dot(coords,absdir)
            // and thus its sign tells us whether the normal is pointing positive or negative
            float3 tcAndLen = mul(absdir, float3x3(coords.zyx, coords.xzy, float3(-coords.xy,coords.z)));
            tcAndLen.xy /= tcAndLen.z;
            // Flip-flop faces for proper orientation and normalize to [-0.5,+0.5]
            bool2 positiveAndVCross = float2(tcAndLen.z, -1) > 0;
            tcAndLen.xy *= (positiveAndVCross[0] ? absdir.yx : (positiveAndVCross[1] ? float2(absdir[2],0) : float2(0,absdir[2]))) - 0.5;
            // Clamp values which are close to the face edges to avoid bleeding/seams (ie. enforce clamp texture wrap mode)
            //tcAndLen.xy = clamp(tcAndLen.xy, edgeSize.xy, edgeSize.zw);
            return tcAndLen.xy;
            }*/

            fixed4 frag(v2f i) : SV_Target {
                float2 skyboxTC = ToHemisphereCoords(i.texcoord);

                // Flip horizontally by inverting the x coordinate
                skyboxTC.x = 1.0f - skyboxTC.x;

                if (skyboxTC.y > 0.5f) {
                    // Map the top hemisphere to the top half of the image
                    skyboxTC.y = 2.0f * (skyboxTC.y - 0.5f);
                } else {
                    // Map the mirrored bottom hemisphere to the bottom half of the image
                    skyboxTC.y = 2.0f * (0.5f - skyboxTC.y);
                }

                // Sample the main texture
                half4 skyTex = tex2D(_MainTex, skyboxTC);
                half3 skyColor = DecodeHDR(skyTex, _MainTex_HDR);
                skyColor = skyColor;// * unity_ColorSpaceDouble.rgb;
                //skyColor *= _Exposure;

                // Calculate cloud texture coordinates using box mapping
                //float2 cloudTC = ToTopPlaneCoords(i.cloudTexcoord)*_CloudScale;
                //cloudTC.x += _CloudSpeed * _Time;

                // Sample the cloud texture
                //half4 cloudTex = tex2D(_CloudTex, cloudTC);
                //half3 cloudColor = cloudTex.rgb * _CloudTint.rgb;

                // Blend the cloud layer with the sky layer
                // finalColor = lerp(skyColor, cloudColor, cloudTex.a);
                half3 finalColor = skyColor;

                return half4(finalColor, 1);
            }
            ENDCG
        }
    }

    Fallback Off
}
