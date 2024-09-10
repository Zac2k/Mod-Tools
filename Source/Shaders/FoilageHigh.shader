Shader "ZicZac/FoilageHigh" {
	Properties {
		_Color ("Main Color", Color) = (0.5,0.5,0.5,1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_Ramp ("Toon Ramp (RGB)", 2D) = "gray" {} 
		_Cutoff ("Alpha cutoff", Range(0,1)) = 0.3
	}

	SubShader {
		Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="Grass"}
		LOD 200
		Cull Off

		CGPROGRAM
		#pragma surface surf ToonRamp alphatest:_Cutoff  noforwardadd interpolateview approxview nolppv nometa nodynlightmap exclude_path:deferred exclude_path:prepass novertexlights
		#pragma target 2.0

		// Textures and colors
		sampler2D _MainTex;
		sampler2D _Ramp;
		fixed4 _Color;

		// Input structure for surface shader
		struct Input {
			fixed2 uv_MainTex : TEXCOORD0;
		};

		// Custom toon ramp lighting function
		#pragma lighting ToonRamp exclude_path:prepass
		inline half4 LightingToonRamp (SurfaceOutput s, half3 lightDir, half atten) {
			#ifndef USING_DIRECTIONAL_LIGHT
			lightDir = normalize(lightDir);
			#endif

			// Calculate lighting intensity and fetch from ramp texture
			half d = dot (s.Normal, lightDir) * 0.5 + 0.5;
			half3 ramp = tex2D (_Ramp, fixed2(d, d)).rgb;

			// Apply lighting and return the final color
			half4 c;
			c.rgb = s.Albedo * _LightColor0.rgb * ramp * (atten * 2);
			c.a = 0;
			return c;
		}

		// Surface shader function
		void surf (Input IN, inout SurfaceOutput o) {
			half4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
	} 

	// Fallback to another shader if this one isn't supported
	FallBack "Nature/TreeCreatorLeaves"
}
