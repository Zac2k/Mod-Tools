Shader "Custom/BlendShader" {
    Properties {
        _MainTex ("Base (RGBA)", 2D) = "white" { }
        _BlendTex ("Blend (RGBA)", 2D) = "white" { }
        _MaskTex ("Mask (RGBA)", 2D) = "white" { }
        _EmitTex ("Mask (RGB)", 2D) = "black" { }
        _TileAmount ("Tile Amount", Range(0, 10)) = 1
        _MaskChann ("MaskChannel", int) = 1
        _PBR ("Is PBR (0 = false)", int) = 0
    }

    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 100

        CGPROGRAM
        #pragma surface surf Lambert

        struct Input {
            float2 uv_MainTex;
        };

        sampler2D _MainTex;
        sampler2D _BlendTex;
        sampler2D _MaskTex;
        sampler2D _EmitTex;

        fixed _TileAmount;
        fixed _MaskChann;
        fixed _PBR;

        void surf (Input IN, inout SurfaceOutput o) {

            fixed4 baseColor = tex2D(_MainTex, IN.uv_MainTex);
            fixed4 blendColor = tex2D(_BlendTex, IN.uv_MainTex* _TileAmount);
            fixed4 maskColor = tex2D(_MaskTex, IN.uv_MainTex);
            fixed4 EmitColor = tex2D(_EmitTex, IN.uv_MainTex* _TileAmount);

            fixed Mask=maskColor.r;

            fixed4 blendedColor;
            if(_MaskChann==2)Mask=maskColor.g;else
            if(_MaskChann==3)Mask=maskColor.b;else
            if(_MaskChann==4)Mask=maskColor.a;

            //= (baseColor*(Mask))+(blendColor*(1-Mask));
            if(_PBR==0){
            blendedColor = (baseColor*(Mask))+(blendColor*(1-Mask));

            }else{
                blendedColor.r=baseColor.r;
                blendedColor.g=(baseColor.g*(Mask))+(blendColor.g*(1-Mask));
            fixed Emit=lerp(-1.5f, 6, 0.299f * EmitColor.r + 0.587f * EmitColor.g + 0.114f * EmitColor.b);
            if(Emit<0)Emit=0;
            blendColor.b=Emit;
            blendColor.a=baseColor.a;
            }


            o.Albedo = blendedColor.rgb;
            o.Alpha = blendedColor.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
