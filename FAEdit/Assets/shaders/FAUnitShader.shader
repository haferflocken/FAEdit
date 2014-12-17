Shader "Custom/FAUnitShader" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_BumpMap ("Normals", 2D) = "bump" {}
		_SpecTeam ("SpecTeam", 2D) = "white" {}
		_TeamColor ("Team Color", Color) = (1.0, 0.0, 0.0, 1.0)
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf BlinnPhong

		sampler2D _MainTex;		// Albedo and alpha
		sampler2D _BumpMap;		// Normals
		sampler2D _SpecTeam;	// Reflection, specularity/gloss, bloom glow, team colors
		float4 _TeamColor;

		struct Input {
			float2 uv_MainTex;
			float2 uv2_BumpMap;
			float2 uv_SpecTeam;
		};

		void surf(Input IN, inout SurfaceOutput o) {
			half4 mainTex = tex2D(_MainTex, IN.uv_MainTex);
			half4 bumpMap = tex2D(_BumpMap, IN.uv2_BumpMap);
			half4 specTeam = tex2D(_SpecTeam, IN.uv_SpecTeam);

			o.Albedo = lerp(mainTex.rgb, _TeamColor.rgb, specTeam.a);
			o.Normal = UnpackNormal(bumpMap);
			o.Emission = half3(specTeam.b, specTeam.b, specTeam.b);
			o.Specular = specTeam.g;
			o.Gloss = specTeam.g;
			o.Alpha = mainTex.a;

			// TODO(jwerner) Reflectiveness.
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
