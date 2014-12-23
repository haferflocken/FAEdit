using UnityEngine;
using System;
using System.IO;

public class DDSImporter
{
	public enum LoadStages
	{
		Init = 0,
		Albedo = 1,
		Normals = 2,
		SpecTeam = 3,
		Count = 4,
	}

	// There are many textures to load, so they are loaded in stages by a coroutine.
	// This isn't just a coroutine itself because it would make the control flow unclear.
	public static bool LoadStage(LoadStages stage, GameObject gameObject, LODFiles lod)
	{
		SkinnedMeshRenderer renderer = gameObject.GetComponent<SkinnedMeshRenderer>();
		if (renderer == null)
		{
			Debug.LogError("Textures cannot be loaded onto a GameObject with no SkinnedMeshRenderer.");
			return false;
		}

		switch (stage)
		{
		case LoadStages.Init:
			return LoadInit(gameObject, renderer, lod);
		case LoadStages.Albedo:
			return LoadAlbedo(gameObject, renderer, lod);
		case LoadStages.Normals:
			return LoadNormals(gameObject, renderer, lod);
		case LoadStages.SpecTeam:
			return LoadSpecTeam(gameObject, renderer, lod);
		}

		Debug.LogError("Invalid DDSImporter load stage.");
		return false;
	}

	private static bool LoadInit(GameObject gameObject, SkinnedMeshRenderer renderer, LODFiles lod)
	{
		renderer.material = ShaderNameToMaterial(lod.shaderName);
		return true;
	}

	private static bool LoadAlbedo(GameObject gameObject, SkinnedMeshRenderer renderer, LODFiles lod)
	{
		Debug.Log("Loading Albedo: " + lod.albedo.Name);
		Texture2D albedo = LoadDDSFile(lod.albedo);
		if (albedo == null)
		{
			Debug.LogError("Failed to load albedo texture.");
			return false;
		}
		renderer.material.SetTexture("_MainTex", albedo);
		return true;
	}

	private static bool LoadNormals(GameObject gameObject, SkinnedMeshRenderer renderer, LODFiles lod)
	{
		Debug.Log("Loading Normals: " + lod.normalsTS.Name);
		Texture2D normalsTS = LoadDDSFile(lod.normalsTS);
		if (normalsTS == null)
		{
			Debug.LogError("Failed to load normalsTS texture.");
			return false;
		}

		renderer.material.SetTexture("_BumpMap", normalsTS);
		renderer.material.SetFloat("_BumpWidth", normalsTS.width);
		renderer.material.SetFloat("_BumpHeight", normalsTS.height);
		return true;
	}

	private static bool LoadSpecTeam(GameObject gameObject, SkinnedMeshRenderer renderer, LODFiles lod)
	{
		Debug.Log("Loading SpecTeam: " + lod.specTeam.Name);
		Texture2D specTeam = LoadDDSFile(lod.specTeam);
		if (specTeam == null)
		{
			Debug.LogError("Failed to load specTeam texture.");
			return false;
		}
		renderer.material.SetTexture("_SpecTeam", specTeam);
		return true;
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

		Debug.Log("Loaded " + stringTextureFormat + " [" + width + ", " + height + "]");
		
		return texture;
	}

	// Shader names can configure materials, so shader name specific material setup goes here.
	private static Material ShaderNameToMaterial(string shaderName)
	{
		Material mat = new Material(Shader.Find("Custom/FAUnitShader"));
		if (shaderName == "Seraphim")
		{
			mat.EnableKeyword("TEAMCOLOR_ALBEDO");
		}
		return mat;
	}
}
