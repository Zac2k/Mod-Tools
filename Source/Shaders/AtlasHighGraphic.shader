Shader "ZicZac/HighGraphic/Atlas/Opaque"
{
    Properties
    {
        _MainTex ("Albedo (RGBA)", 2D) = "white" {}
        [noscaleoffset]_PBR ("PBR (RGBA)", 2D) = "white" {}
        _Offset ("Offset", Vector) = (0, 0, 1, 1)
        [noscaleoffset][Normal]_BumpMap ("Normalmap", 2D) = "bump" {}
    }
    SubShader
    {
        Tags {"RenderType"="Opaque"}
        LOD 512

        CGPROGRAM
        // Physically based Standard lighting model, with shadows enabled on all light types
        #pragma surface surf Standard interpolateview approxview nolppv nometa nodynlightmap exclude_path:deferred exclude_path:prepass

        // Use shader model 3.0 target for better lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _PBR;
        sampler2D _BumpMap;

        half4 _Offset;
        struct Input
        {
            half2 uv_MainTex;
        };

        // Instancing support
        #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // Additional per-instance properties
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Adjust UV coordinates with the atlas offset
            half2 uv = frac(IN.uv_MainTex);
            uv = fmod(uv, _Offset.zw);
            uv += _Offset.xy;

            // Sample textures with precision adjustments
            fixed4 c = tex2D(_MainTex, uv);
            fixed4 pbr = tex2D(_PBR, uv);
            
            // Assign the texture values to the surface output
            o.Albedo = c.rgb;
            o.Normal = UnpackNormal(tex2D(_BumpMap, uv));
            o.Occlusion = pbr.r;
            o.Metallic = pbr.g;
            o.Smoothness = pbr.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
