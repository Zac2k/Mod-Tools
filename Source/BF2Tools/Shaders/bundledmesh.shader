Shader "BF2/bundledmesh.fx/Opaque" 
{
    Properties
    {
        [noscaleoffset]_MainTex ("Albedo (RGBA)", 2D) = "white" {}
        [noscaleoffset][Normal]_BumpMap ("Normalmap", 2D) = "bump" {}
        [noscaleoffset]_Wreck ("Wreck (RGB)", 2D) = "gray" {}

        _Metallic ("Metallic", Range(0,1)) = 0
        _Glossiness ("Smoothness", Range(0,1)) = 0.5 

         
        [Toggle] _hasBump("Alpha ", Float) = 0
        [Toggle] _hasWreck("Detail ", Float) = 0
        [Toggle] _hasAlpha("Dirt", Float) = 0
        [Toggle] _hasEnvMap("Crack", Float) = 0
        [Toggle] _hasBumpAlpha("CrackN", Float) = 0

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
        sampler2D _BumpMap;
        sampler2D _Wreck;

        struct Input
        {
            fixed2 uv_MainTex;
        };

        half _Metallic;
        half _Glossiness;

        bool _hasBump;
        bool _hasWreck;
        bool _hasAlpha;
        bool _hasEnvMap;
        bool _hasBumpAlpha;
        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
        //UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            
        /*
        sampler2D _MainTex; - texture0
        sampler2D _BumpMap; - texture1
        sampler2D _Wreck; - texture2
        */

        fixed2 uv = IN.uv_MainTex;

                // textures
        fixed4 colormap    = tex2D(_MainTex, uv);
        half4 normalmap  = tex2D(_BumpMap, uv);
        half4 wreckmap    = tex2D(_Wreck, uv);

        fixed spec = 1;
        

                // diffuse
                fixed4 frag = colormap;

                 // alpha
                if (_hasBumpAlpha) {
                frag.a = normalmap.a;
                }
            
                // wreck
                if (_hasWreck) {
                 frag.rgb *= wreckmap.rgb;
                }

                 // normal
                if (_hasBump) {
                o.Normal =  UnpackNormal(normalmap);
                }

                 // specular
                if (_hasAlpha) {
                if (_hasBump) {
                spec *= normalmap.a;
                }
                } else {
                spec *= colormap.a;
                }

                 // glass
                if (_hasAlpha) {
                if (_hasEnvMap) {
                    o.Metallic = colormap.a;
                }
                }

                // envmap
                if (_hasEnvMap) {
                    o.Metallic = colormap.a;
                }

                

            o.Albedo = frag;

            // Metallic and smoothness come from slider variables
            o.Smoothness =spec*_Glossiness*unity_ColorSpaceDouble.r;
        }

        
        ENDCG
    }
    FallBack "Diffuse"
}



