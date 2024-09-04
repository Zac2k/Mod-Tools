Shader "ZicZac/MidGraphics/Terrain"
{
    Properties
    {
        [noscaleoffset]_Splat ("Splat (RGBA)", 2D) = "white" {}
        _Scales ("Scales", Vector) = (1, 1, 1, 1)

        [noscaleoffset]_Tex1 ("Tex1 (RGBA)", 2D) = "white" {}
        [noscaleoffset][Normal]_BumpMap1 ("Normalmap1", 2D) = "bump" {}

        [noscaleoffset]_Tex2 ("Tex2 (RGBA)", 2D) = "white" {}
        [noscaleoffset][Normal]_BumpMap2 ("Normalmap2", 2D) = "bump" {}

        [noscaleoffset]_Tex3 ("Tex3 (RGBA)", 2D) = "white" {}
        [noscaleoffset][Normal]_BumpMap3 ("Normalmap3", 2D) = "bump" {}

        [noscaleoffset]_Tex4 ("Tex4 (RGBA)", 2D) = "white" {}
        [noscaleoffset][Normal]_BumpMap4 ("Normalmap4", 2D) = "bump" {} 
    }
    SubShader {
    Tags { "RenderType"="Opaque" }
    LOD 150

    CGPROGRAM
    #pragma target 2.0
    #pragma surface surf BlinnPhong noforwardadd interpolateview approxview nolppv nometa nodynlightmap exclude_path:deferred exclude_path:prepass
    
        sampler2D _Splat;

        sampler2D _BumpMap1;
        sampler2D _BumpMap2;
        sampler2D _BumpMap3;
        sampler2D _BumpMap4;

        sampler2D _Tex1;
        sampler2D _Tex2;
        sampler2D _Tex3;
        sampler2D _Tex4;

        //fixed _Glossiness;
        //fixed4 _Color;

        fixed4 _Scales;


struct Input {
            float2 uv_Splat;
};



void surf (Input IN, inout SurfaceOutput o) {
           fixed4 Splat = tex2D (_Splat, IN.uv_Splat);

            fixed4 c1 = tex2D (_Tex1, IN.uv_Splat*_Scales.x);
            fixed4 c2 = tex2D (_Tex2, IN.uv_Splat*_Scales.y);
            fixed4 c3 = tex2D (_Tex3, IN.uv_Splat*_Scales.z);
            fixed4 c4 = tex2D (_Tex4, IN.uv_Splat*_Scales.w);
            

            o.Albedo =
            (c1.rgb*Splat.r)+
            (c2.rgb*Splat.g)+
            (c3.rgb*Splat.b)+
            (c4.rgb*Splat.a);
            
              	float4 nrm = 
                Splat.r * tex2D(_BumpMap1, IN.uv_Splat*_Scales.x)+
		        Splat.g * tex2D(_BumpMap2, IN.uv_Splat*_Scales.y)+
		        Splat.b * tex2D(_BumpMap3, IN.uv_Splat*_Scales.z)+
		        Splat.a * tex2D(_BumpMap4, IN.uv_Splat*_Scales.w);

            o.Alpha =
            (c1.a*Splat.r)+
            (c2.a*Splat.g)+
            (c3.a*Splat.b)+
            (c4.a*Splat.a); 
            
            o.Gloss = o.Alpha;
            o.Specular = o.Alpha;
            o.Normal = UnpackNormal(nrm);

}
ENDCG
    }




FallBack "Diffuse"
}
