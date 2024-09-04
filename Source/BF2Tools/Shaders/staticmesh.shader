Shader "BF2/staticmesh.fx/Opaque"
{
    Properties
    {
        [noscaleoffset]_MainTex ("Albedo (RGBA)", 2D) = "white" {}
        [noscaleoffset]_Detail ("Detail (RGB)", 2D) = "gray" {}
        [noscaleoffset]_Dirt ("Dirt (RGB)", 2D) = "gray" {}
        [noscaleoffset]_Crack ("Crack (RGB)", 2D) = "gray" {}
        [noscaleoffset]_DetailNRM ("Detail Normal", 2D) = "bump" {}
        [noscaleoffset]_CrackNRM ("Crack Normal", 2D) = "bump" {}

        _Metallic ("Metallic", Range(0,1)) = 0
        _Glossiness ("Smoothness", Range(0,1)) = 0.5 

         
        [Toggle] _hasAlpha("Alpha ", Float) = 0
        [Toggle] _hasDetail("Detail ", Float) = 0
        [Toggle] _hasDirt("Dirt", Float) = 0
        [Toggle] _hasCrack("Crack", Float) = 0
        [Toggle] _hasCrackN("CrackN", Float) = 0
        [Toggle] _hasDetailN("DetailN", Float) = 0

        

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
        sampler2D _Detail;
        sampler2D _DetailNRM;
        sampler2D _Dirt;
        sampler2D _Crack;
        sampler2D _CrackNRM;

        struct Input
        {
            fixed2 uv_MainTex;
    half2 uv3_Detail;
    half2 uv4_Dirt;
    half2 uv5_Crack;
        };

        half _Metallic;
        half _Glossiness;

        bool _hasAlpha;
        bool _hasDetail;
        bool _hasDirt;
        bool _hasCrack;
        bool _hasCrackN;
        bool _hasDetailN;
        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
        //UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            //fixed4 c = tex2D (_MainTex, IN.uv_MainTex) ;
            //half4 d = tex2D(_Detail, IN.uv3_Detail);
            fixed4 frag =fixed4(1,1,1,1);
            
        /*
        sampler2D _MainTex; - texture0
        sampler2D _Detail; - texture1
        sampler2D _Dirt; - texture2
        sampler2D _Crack; - texture3
        sampler2D _DetailNRM; - texture4
        sampler2D _CrackNRM; - texture5
        */
        fixed2 uv0 = IN.uv_MainTex;
        half2 uv1 = IN.uv3_Detail;
        half2 uv2 = IN.uv4_Dirt;
        half2 uv3 = IN.uv5_Crack;

                // textures
        fixed4 basemap    = tex2D(_MainTex, uv0);
        half4 detailmap  = tex2D(_Detail, uv1);
        half4 dirtmap    = tex2D(_Dirt, uv2);
        half4 crackmap   = tex2D(_Crack, uv2);
        half4 detailmapN;
        half4 crackmapN;

        fixed spec = 1;
        
             if (_hasDirt) {
                if (_hasCrack) {
                detailmapN = tex2D(_DetailNRM, uv1);
                crackmapN = tex2D(_CrackNRM, uv2);
                } else {
                detailmapN = tex2D(_Crack, uv1);
                }
                } else {
                if (_hasCrack) {
                detailmapN = tex2D(_Crack, uv1);
                crackmap = tex2D(_Dirt, uv2);
                crackmapN = tex2D(_DetailNRM, uv2);
                } else {
                detailmapN = tex2D(_DetailNRM, uv1);
                }
                }

                // diffuse
                frag *= basemap;
                if (_hasDetail) {
                frag *= detailmap;
                }

                 // alpha
                if (_hasAlpha) {
                frag.a = detailmap.a;
                }
            
                // crack
                if (_hasCrack) {
                 frag.rgb = lerp(frag.rgb, crackmap.rgb, crackmap.a);
                }

                 // dirt
                if (_hasDirt) {
                frag.rgb *= dirtmap.rgb;
                spec *= dirtmap.r*dirtmap.g*dirtmap.b;
                }

                 // specular
                if (_hasAlpha) {
                if (_hasDetailN) {
                spec *= detailmapN.a;
                }
                } else {
                spec *= detailmap.a;
                }


                 // normal
                if (_hasDetailN&&!_hasDirt) {
                half4 n =detailmapN; // detail normal
                if (_hasCrack) {
                n = lerp(n, crackmapN, crackmap.a); // crack normal
                }  
                o.Normal =  UnpackNormal(n);
                }

            o.Albedo = frag;

            // Metallic and smoothness come from slider variables
            o.Smoothness =spec*_Glossiness*unity_ColorSpaceDouble.r;
        }

        
        ENDCG
    }
    FallBack "Diffuse"
}



