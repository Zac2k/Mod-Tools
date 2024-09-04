// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "ZicZac/Water/High" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1) // The base color of the material
        _MainTex ("Albedo (RGB)", 2D) = "white" {} // The main texture, providing the albedo (color)
        _Metallic ("Metallic", Range(0,1)) = 1 // The metallic value of the material
        _Glossiness ("Smoothness", Range(-1,1)) = 1 // The smoothness (glossiness) value
        [noscaleoffset]_BumpMap ("Normalmap", 2D) = "bump" {} // The first normal map for surface details
        _BumpMap2 ("Normalmap2", 2D) = "bump" {} // The second normal map for additional details
        _ScrollSpeed ("Scroll Speed", Range(-10,10)) = 1 // The speed at which the bump maps scroll
    }

    SubShader {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" } // Set rendering order and other tags
        LOD 512 // Level of detail for the shader
        Cull Off // Disable backface culling, rendering both sides of the polygons

        CGPROGRAM
        #pragma surface surf Standard alpha:fade // Use Standard lighting model with alpha blending
        #pragma target 3.0 // Target shader model 3.0 for advanced features

        // Texture samplers for the shader
        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _BumpMap2;

        struct Input {
            float2 uv_MainTex; // UV coordinates for the main texture
            float2 uv_BumpMap; // UV coordinates for the first normal map
            float2 uv2_BumpMap2; // UV coordinates for the second normal map
        };

        fixed _Metallic; // Metallic property from material
        fixed _Glossiness; // Glossiness property from material
        fixed _ScrollSpeed; // Scrolling speed for the bump maps
        float4 _Color; // Base color of the material

        void surf (Input IN, inout SurfaceOutputStandard o) {
            // Scroll the UV coordinates of the bump maps using time and scroll speed
            float2 scrolledUV1 = IN.uv_BumpMap + (_ScrollSpeed * _Time.y);
            float2 scrolledUV2 = IN.uv2_BumpMap2 - (_ScrollSpeed * _Time.y);

            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;

            // Combine and unpack the scrolled normal maps
            float3 normal1 = UnpackNormal(tex2D(_BumpMap, scrolledUV1))*0.5;
            float3 normal2 = UnpackNormal(tex2D(_BumpMap2, scrolledUV2))*0.5;
            o.Normal = normalize(normal1 + normal2);

            // Set metallic and smoothness values
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;

            // Alpha is determined by texture and color alpha
            o.Alpha = c.a * _Color.a;
        }
        ENDCG
    }

    FallBack "Diffuse" // Fallback to a simpler diffuse shader if unsupported
}
