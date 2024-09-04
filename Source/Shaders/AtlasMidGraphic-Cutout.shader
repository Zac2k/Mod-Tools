Shader "ZicZac/MidGraphic/Atlas/Cutout"
{
    Properties
    {
        [noscaleoffset]_MainTex ("Albedo (RGBA)", 2D) = "white" {}
        _Offset ("Offset", Vector) = (0, 0, 1, 1)
        [noscaleoffset][Normal]_BumpMap ("Normalmap", 2D) = "bump" {}
        _Cutoff ("Alpha cutoff", Range(0,1)) = 0.3
    }
SubShader {
    Tags {"Queue"="AlphaTest" "IgnoreProjector"="True"}
    Cull Off
    LOD 400

    CGPROGRAM
    #pragma surface surf BlinnPhong alphatest:_Cutoff interpolateview approxview nolppv nometa nodynlightmap exclude_path:deferred exclude_path:prepass
    #pragma target 2.0
    
sampler2D _MainTex;
//sampler2D _PBR;
sampler2D _BumpMap;
fixed4 _Color;
fixed4 _Offset;

struct Input {
    fixed2 uv_MainTex;
};

        void surf (Input IN, inout SurfaceOutput o)
        {
            
            fixed2 uv=frac(IN.uv_MainTex);
            uv%=_Offset.zw;
            uv+=_Offset.xy;

            fixed4 tex = tex2D(_MainTex,uv);
            o.Normal = UnpackNormal(tex2D(_BumpMap,uv));
            o.Albedo = tex.rgb ;
            o.Gloss = tex.a;
            o.Specular = tex.a;
            o.Alpha = tex.a;
        }
        ENDCG
    }
    FallBack "Diffuse" 
}