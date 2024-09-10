Shader "ZicZac/LowGraphic/Atlas/Opaque"
{
    Properties
    {
        [noscaleoffset]_MainTex ("Albedo (RGBA)", 2D) = "white" {}
        _Offset ("Offset", Vector) = (0, 0, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 400

        CGPROGRAM
        #pragma surface surf Lambert interpolateview approxview nolppv nometa nodynlightmap exclude_path:deferred exclude_path:prepass
        #pragma target 2.0
        
        sampler2D _MainTex;

        half4 _Offset;
        struct Input {
            half2 uv_MainTex;
        };
        #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // Additional per-instance properties
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutput o)
        {
            // Calculate UV coordinates based on the atlas offset
            half2 uv = frac(IN.uv_MainTex);
            uv = fmod(uv, _Offset.zw);
            uv += _Offset.xy;

            // Sample the main texture
            fixed4 tex = tex2D(_MainTex, uv);
            o.Albedo = tex.rgb;
        }
        ENDCG
    }
    FallBack "Mobile/VertexLit"
}
