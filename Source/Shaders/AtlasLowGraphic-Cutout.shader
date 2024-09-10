Shader "ZicZac/LowGraphic/Atlas/Cutout"
{
    Properties
    {
        [noscaleoffset]_MainTex ("Albedo (RGBA)", 2D) = "white" {}
        _Offset ("Offset", Vector) = (0, 0, 1, 1)
        _Cutoff ("Alpha cutoff", Range(0,1)) = 0.3
    }
    SubShader
    {
        Tags {"Queue"="AlphaTest" "IgnoreProjector"="True"}
        Cull Off
        LOD 400

        CGPROGRAM
        #pragma surface surf Lambert alphatest:_Cutoff interpolateview approxview nolppv nometa nodynlightmap exclude_path:deferred exclude_path:prepass
        #pragma target 2.0

        sampler2D _MainTex;
        half4 _Offset;

        #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // Additional per-instance properties
        UNITY_INSTANCING_BUFFER_END(Props)

        struct Input
        {
            half2 uv_MainTex;
        };

        void surf (Input IN, inout SurfaceOutput o)
        {
            // Adjust UV coordinates with the atlas offset
            half2 uv = frac(IN.uv_MainTex);
            uv = fmod(uv, _Offset.zw);
            uv += _Offset.xy;

            // Sample the main texture
            fixed4 tex = tex2D(_MainTex, uv);
            o.Albedo = tex.rgb;
            o.Alpha = tex.a;
        }
        ENDCG
    }
    FallBack "Mobile/VertexLit"
}
