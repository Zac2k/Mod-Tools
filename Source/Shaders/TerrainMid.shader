Shader "ZicZac/MidGraphics/Terrain"
{
    Properties
    {
        [NoScaleOffset] _Splat ("Splat (RGBA)", 2D) = "white" {}
        _Scales ("Scales", Vector) = (1, 1, 1, 1)

        [NoScaleOffset] _Tex1 ("Tex1 (RGBA)", 2D) = "white" {}
        [NoScaleOffset] [Normal] _BumpMap1 ("Normalmap1", 2D) = "bump" {}

        [NoScaleOffset] _Tex2 ("Tex2 (RGBA)", 2D) = "white" {}
        [NoScaleOffset] [Normal] _BumpMap2 ("Normalmap2", 2D) = "bump" {}

        [NoScaleOffset] _Tex3 ("Tex3 (RGBA)", 2D) = "white" {}
        [NoScaleOffset] [Normal] _BumpMap3 ("Normalmap3", 2D) = "bump" {}

        [NoScaleOffset] _Tex4 ("Tex4 (RGBA)", 2D) = "white" {}
        [NoScaleOffset] [Normal] _BumpMap4 ("Normalmap4", 2D) = "bump" {} 
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 150

        CGPROGRAM
        #pragma surface surf BlinnPhong noforwardadd interpolateview approxview nolppv nometa nodynlightmap exclude_path:deferred exclude_path:prepass
        #pragma target 2.0

        sampler2D _Splat;

        sampler2D _BumpMap1;
        sampler2D _BumpMap2;
        sampler2D _BumpMap3;
        sampler2D _BumpMap4;

        sampler2D _Tex1;
        sampler2D _Tex2;
        sampler2D _Tex3;
        sampler2D _Tex4;

        fixed4 _Scales;

        struct Input
        {
            float2 uv_Splat;
        };

        void surf(Input IN, inout SurfaceOutput o)
        {
            fixed4 Splat = tex2D(_Splat, IN.uv_Splat);

            // Sample the textures using scaled UVs
            float2 uv1 = IN.uv_Splat * _Scales.x;
            float2 uv2 = IN.uv_Splat * _Scales.y;
            float2 uv3 = IN.uv_Splat * _Scales.z;
            float2 uv4 = IN.uv_Splat * _Scales.w;

            fixed4 c1 = tex2D(_Tex1, uv1);
            fixed4 c2 = tex2D(_Tex2, uv2);
            fixed4 c3 = tex2D(_Tex3, uv3);
            fixed4 c4 = tex2D(_Tex4, uv4);

            // Combine albedo using the splat map
            o.Albedo = c1.rgb * Splat.r + c2.rgb * Splat.g + c3.rgb * Splat.b + c4.rgb * Splat.a;

            // Combine normal maps using the splat map
            half3 n1 = UnpackNormal(tex2D(_BumpMap1, uv1));
            half3 n2 = UnpackNormal(tex2D(_BumpMap2, uv2));
            half3 n3 = UnpackNormal(tex2D(_BumpMap3, uv3));
            half3 n4 = UnpackNormal(tex2D(_BumpMap4, uv4));

            o.Normal = normalize(n1 * Splat.r + n2 * Splat.g + n3 * Splat.b + n4 * Splat.a);

            // Set alpha, gloss, and specular based on the alpha channels of textures
            half alpha = c1.a * Splat.r + c2.a * Splat.g + c3.a * Splat.b + c4.a * Splat.a;
            o.Alpha = alpha;
            o.Gloss = alpha;
            o.Specular = alpha;
        }
        ENDCG
    }

    FallBack "Diffuse"
}
