Shader "Custom/FAUnitShader"
{
	Properties
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_BumpMap ("Normals", 2D) = "bump" {}
		_SpecTeam ("SpecTeam", 2D) = "black" {}
		_TeamColor ("Team Color", Color) = (1.0, 0.0, 0.0)
		_Cube ("Cubemap", CUBE) = "" {}
		_BumpWidth ("Normals Width", Float) = 0.0
		_BumpHeight ("Normals Height", Float) = 0.0
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 400
		
		CGPROGRAM
		#pragma surface surf BlinnPhong
		#pragma target 3.0
		#pragma multi_compile TEAMCOLOR_SPECTEAM TEAMCOLOR_ALBEDO

		sampler2D _MainTex;		// Albedo and alpha
		sampler2D _BumpMap;		// Normals
		sampler2D _SpecTeam;	// Reflection, specularity/gloss, bloom glow, team colors
		fixed3 _TeamColor;
		samplerCUBE _Cube;
		float _BumpWidth;
		float _BumpHeight;

		struct Input
		{
			float2 uv_MainTex;
			float2 uv2_BumpMap;
			float3 worldRefl;
			INTERNAL_DATA
		};

		void surf(Input IN, inout SurfaceOutput o)
		{
			half4 mainTex = tex2D(_MainTex, IN.uv_MainTex);
			half4 specTeam = tex2D(_SpecTeam, IN.uv_MainTex);
			
			#ifdef TEAMCOLOR_ALBEDO
				o.Albedo = lerp(mainTex.rgb, _TeamColor, mainTex.a);
			#elif TEAMCOLOR_SPECTEAM
				o.Albedo = lerp(mainTex.rgb, _TeamColor, specTeam.a);
			#endif

			float bumpPixelX = IN.uv2_BumpMap.x * _BumpWidth;
			float bumpPixelY = IN.uv2_BumpMap.y * _BumpHeight;
			
			float2 bumpRightUVs = float2((bumpPixelX + 1) / _BumpWidth, bumpPixelY);
			float2 bumpUpUVs = float2(bumpPixelX, (bumpPixelY - 1) / _BumpHeight);

			half hCenter = tex2D(_BumpMap, IN.uv2_BumpMap).r;
			half hUp = tex2D(_BumpMap, bumpUpUVs).r;
			half hRight = tex2D(_BumpMap, bumpRightUVs).r;

			half dY = hCenter - hUp;
			half dX = hCenter - hRight;
			half length = sqrt(dY * dY + dX * dX + 1);
			half invLength = 1 / length;

			o.Normal = half3(dX * invLength, 1 - dY * invLength, (invLength + 1) * 0.5);

			half4 cubeMap = texCUBE(_Cube, WorldReflectionVector(IN, o.Normal));

			o.Emission = (specTeam.b + cubeMap.rgb * specTeam.r * Luminance(mainTex.rgb)) * 0.5;
			o.Specular = specTeam.r;
			o.Gloss = specTeam.g;
		}
		ENDCG
	} 
	FallBack "Specular"
}
