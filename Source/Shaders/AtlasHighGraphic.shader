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
        Tags {"RenderType"="Opaque" }

        LOD 512

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard interpolateview approxview nolppv nometa nodynlightmap exclude_path:deferred exclude_path:prepass

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _PBR;
        sampler2D _BumpMap;

        struct Input
        {
            fixed2 uv_MainTex;
        };

        fixed4 _Offset;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        // min is a 2d vector that limits the mip in x and y axes. (1,1) will be the lowest resolution mip, (0,0) is no limit and will use highest res mip


        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            
            fixed2 uv=frac(IN.uv_MainTex);
            uv%=_Offset.zw;
            uv+=_Offset.xy;

            fixed4 c = tex2D (_MainTex, uv);
            fixed4 pbr = tex2D (_PBR, uv);
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


