using UnityEngine;
using System;
using System.IO;

public class DDSImporter
{
	public static void Load(GameObject gameObject, FileInfo albedoFile, FileInfo normalsTSFile, FileInfo specTeamFile)
	{
		SkinnedMeshRenderer renderer = gameObject.GetComponent<SkinnedMeshRenderer>();
		if (renderer == null)
		{
			Debug.LogError("Textures cannot be loaded onto a GameObject with no SkinnedMeshRenderer.");
			return;
		}

		// Load the raw albedo.
		Debug.Log("Loading Albedo: " + albedoFile.Name);
		Texture2D albedo = LoadDDSFile(albedoFile);
		if (albedo == null)
		{
			Debug.LogError("Failed to load albedo texture.");
			return;
		}
		Debug.Log("Albedo dimensions: " + albedo.width + ", " + albedo.height);

		// Load the raw normals.
		Debug.Log("Loading Normals: " + normalsTSFile.Name);
		Texture2D normalsTS = LoadDDSFile(normalsTSFile);
		if (normalsTS == null)
		{
			Debug.LogError("Failed to load normalsTS texture.");
			return;
		}
		Debug.Log("NormalsTS dimensions: " + normalsTS.width + ", " + normalsTS.height);

		// Load the raw specteam.
		Debug.Log("Loading SpecTeam: " + specTeamFile.Name);
		Texture2D specTeam = LoadDDSFile(specTeamFile);
		if (specTeam == null)
		{
			Debug.LogError("Failed to load specTeam texture.");
			return;
		}
		Debug.Log("SpecTeam dimensions: " + specTeam.width + ", " + specTeam.height);

		// Transform the normal map to a proper normal map.
		Texture2D normals = GrayscaleToNormals(normalsTS, albedo.width, albedo.height);

		// Copy the green channel of the specteam to the alpha channel of the albedo.
		/*Color[] greenToAlpha = new Color[albedo.width * albedo.height];
		float fAlbedoWidth = (float)albedo.width;
		float fAlbedoHeight = (float)albedo.height;
		for (int y = 0; y < albedo.height; ++y)
		{
			float fY = (float)y;
			for (int x = 0; x < albedo.width; ++x)
			{
				float fX = (float)x;

				Color pixel = albedo.GetPixel(x, y);
				Color specTeamPixel = specTeam.GetPixelBilinear(fX / fAlbedoWidth, fY / fAlbedoHeight);
				Color adjustedPixel = new Color(pixel.r, pixel.g, pixel.b, specTeamPixel.g);
				greenToAlpha[x + y * albedo.width] = adjustedPixel;
			}
		}
		Texture2D albedoWithSpecTeamGreen = new Texture2D(albedo.width, albedo.height, TextureFormat.ARGB32, false);
		albedoWithSpecTeamGreen.SetPixels(greenToAlpha);
		albedoWithSpecTeamGreen.Apply();*/

		// Assign the loaded textures to a material and apply it.
		Material material = new Material(Shader.Find("Custom/FAUnitShader"));
		material.SetTexture("_MainTex", albedo);
		//material.SetTexture("_BumpMap", normals); // TODO(jwerner) 
		material.SetTexture("_SpecTeam", specTeam);

		renderer.material = material;
	}

	private static Texture2D LoadDDSFile(FileInfo ddsFile)
	{
		using(FileStream inputStream = ddsFile.OpenRead())
		{
			byte[] ddsBytes = new byte[inputStream.Length];
			inputStream.Read(ddsBytes, 0, ddsBytes.Length);
			return LoadTextureDXT(ddsBytes);
		}
	}

	// http://answers.unity3d.com/questions/555984/can-you-load-dds-textures-during-runtime.htmlhttp://answers.unity3d.com/questions/555984/can-you-load-dds-textures-during-runtime.html
	private static Texture2D LoadTextureDXT(byte[] ddsBytes)
	{
		byte ddsSizeCheck = ddsBytes[4];
		if (ddsSizeCheck != 124)
		{
			Debug.LogError("Invalid DDS DXTn texture. Unable to read");  // This header byte should be 124 for DDS image files.
			return null;
		}

		char[] rawTextureFormat = { (char)ddsBytes[84], (char)ddsBytes[85], (char)ddsBytes[86], (char)ddsBytes[87] };
		string stringTextureFormat = new string(rawTextureFormat);
		TextureFormat textureFormat;
		switch (stringTextureFormat)
		{
		case "DXT1":
			textureFormat = TextureFormat.DXT1;
			break;
		case "DXT3":
		case "DXT5":
			textureFormat = TextureFormat.DXT5;
			break;
		default:
			Debug.LogError("Invalid DDS FOURCC code: " + stringTextureFormat + ". Only DXT1 and DXT5 formats are supported by this method.");
			return null;
		}
		
		int height = ddsBytes[13] * 256 + ddsBytes[12];
		int width = ddsBytes[17] * 256 + ddsBytes[16];
		
		int DDS_HEADER_SIZE = 128;
		byte[] dxtBytes = new byte[ddsBytes.Length - DDS_HEADER_SIZE];
		Buffer.BlockCopy(ddsBytes, DDS_HEADER_SIZE, dxtBytes, 0, ddsBytes.Length - DDS_HEADER_SIZE);
		
		Texture2D texture = new Texture2D(width, height, textureFormat, false);
		texture.LoadRawTextureData(dxtBytes);
		texture.Apply();
		
		return texture;
	}

	// http://answers.unity3d.com/questions/723993/how-does-unity-create-normal-maps-from-grayscale.html
	private static Texture2D GrayscaleToNormals(Texture2D bumpMap, int newWidth, int newHeight)
	{
		Texture2D texture = new Texture2D(newWidth, newHeight, TextureFormat.ARGB32, false);
		float fNewWidth = (float)newWidth;
		float fNewHeight = (float)newHeight;

		for (int y = 0; y < newWidth; ++y)
		{
			float fY = (float)y;
			for (int x = 0; x < newHeight; ++x)
			{
				float fX = (float)x;
				float fPixelX = fX / fNewWidth;
				float fPixelY = fY / fNewHeight;

				float hCenter = bumpMap.GetPixelBilinear(fPixelX, fPixelY).grayscale;
				float hUp = bumpMap.GetPixelBilinear(fPixelX, (fY - 1.0f) / fNewHeight).grayscale;
				float hRight = bumpMap.GetPixelBilinear((fX + 1.0f) / fNewWidth, fPixelY).grayscale;

				float dY = Mathf.Abs(hCenter - hUp);
				float dX = Mathf.Abs(hCenter - hRight);

				float length = Mathf.Sqrt(dY * dY + dX * dX + 1);
				float invLength = 1.0f / length;

				float red = dX * invLength;
				float green = 1.0f - dY * invLength;
				float blue = (invLength + 1.0f) * 0.5f;
				float alpha = 1.0f;

				/*Color pixel = bumpMap.GetPixelBilinear(fX / fNewWidth, fY / fNewHeight);
				float red = pixel.a;
				float green = 1.0f - pixel.g;
				float blue =  1.0f;
				float alpha = 1.0f;*/

				texture.SetPixel(x, y, new Color(red, green, blue, alpha));
			}
		}

		texture.Apply();
		return texture;
	}
}
