Shader "ZicZac/Emission/High"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Emission ("Emission", Range(0,2)) = 1
         _PBR ("PBR (RGB)", 2D) = "white" {}
        _Metallic ("Metallic", Range(0,1)) = 1
        _Glossiness ("Smoothness", Range(-1,1)) = 1
         _BumpMap ("Normalmap", 2D) = "bump" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"} 
        
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard alpha:fade noforwardadd interpolateview approxview nolppv nometa nodynlightmap exclude_path:deferred exclude_path:prepass novertexlights noshadow

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _PBR;
        sampler2D _BumpMap;

        struct Input
        {
            fixed2 uv_MainTex;
				fixed3 viewDir;
        };
\
        fixed4 _Color;
        half _Emission;
         half _Metallic;
        half _Glossiness;
        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            fixed4 pbr = tex2D (_PBR, IN.uv_MainTex);
            o.Albedo = c.rgb;
            o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
            o.Occlusion = pbr.r;
            o.Metallic = pbr.g*_Metallic;
            o.Smoothness =pbr.a*_Glossiness;
            o.Emission=c.rgb*_Emission;
            o.Alpha=c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
