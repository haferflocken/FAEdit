using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

public class FAUnitComponent : MonoBehaviour
{
	public string _unitID;
	private BPComponent _blueprint;

	void Start()
	{
		StartCoroutine(LoadCoroutine());
	}

	System.Collections.IEnumerator LoadCoroutine()
	{
		// Verify that the directory exists and grab its files.
		string directoryPath = FAGameComponent.Instance.MakeAbsolutePath("/units/" + _unitID);
		DirectoryInfo directory = new DirectoryInfo(directoryPath);
		if (!directory.Exists)
		{
			Debug.LogError(directoryPath + " does not exist.");
			yield break;
		}
		IEnumerable<FileInfo> directoryFiles = directory.GetFiles();

		// Load the BP, so we know what mesh, animations, and textures to load and what shaders to load.
		FileInfo blueprintFile = directoryFiles.FirstOrDefault(x => x.Name.EndsWith("_unit.bp", StringComparison.CurrentCultureIgnoreCase));
		if (blueprintFile == null || !blueprintFile.Exists)
		{
			Debug.LogError(directoryPath + " does not contain a unit blueprint.");
			yield break;
		}
		BPImporter.Load(gameObject, blueprintFile);
		_blueprint = GetComponent<BPComponent>();
		yield return null;

		// Get the LOD0 files.
		LODFiles lod0 = GetLODFiles(0, directoryFiles);
		if (lod0.mesh == null)
		{
			Debug.LogError(directoryPath + " does not contain an LOD0.scm file.");
			yield break;
		}

		// Load the mesh.
		SCMImporter.Load(gameObject, lod0.mesh);

		// Apply the blueprint's scale.
		LuaValue scaleVal = _blueprint.Get("Display.UniformScale");
		float scale = (scaleVal == null || scaleVal.LVT != LuaValueType.LVT_Float) ? 1.0f : scaleVal.FloatValue;
		transform.root.localScale = new Vector3(scale, scale, scale);
		yield return new WaitForSeconds(5f);

		// Iteratively load the DDS files.
		for (DDSImporter.LoadStages i = DDSImporter.LoadStages.Init; i < DDSImporter.LoadStages.Count; ++i)
		{
			bool success = DDSImporter.LoadStage(i, gameObject, lod0);
			yield return new WaitForSeconds(5f);

			if (!success)
			{
				break;
			}
		}

		// Load the SCAs referenced in the blueprint.
		List<FileInfo> scaFiles = GetSCAFiles();
		foreach (FileInfo scaFile in scaFiles)
		{
			SCAImporter.Load(gameObject, scaFile);
			yield return null;
		}
	}

	private LODFiles GetLODFiles(int level, IEnumerable<FileInfo> directoryFiles)
	{
		string blueprintPath = "Display.Mesh.LODs." + level;
		string meshPath = FAGameComponent.Instance.MakeAbsolutePath(_blueprint.GetString(blueprintPath + ".MeshName"));
		string albedoPath = FAGameComponent.Instance.MakeAbsolutePath(_blueprint.GetString(blueprintPath + ".AlbedoName"));
		string normalsTSPath = FAGameComponent.Instance.MakeAbsolutePath(_blueprint.GetString(blueprintPath + ".NormalsName"));
		string specTeamPath = FAGameComponent.Instance.MakeAbsolutePath(_blueprint.GetString(blueprintPath + ".SpecularName"));
		string shaderName = _blueprint.GetString(blueprintPath + ".ShaderName");

		LODFiles lod;
		lod.mesh = (meshPath != null) ? new FileInfo(meshPath) : directoryFiles.FirstOrDefault(
			x =>	x.Name.EndsWith("LOD0.scm", StringComparison.CurrentCultureIgnoreCase));
		lod.albedo = (albedoPath != null) ? new FileInfo(albedoPath) : directoryFiles.FirstOrDefault(
			x =>	x.Name.EndsWith("Albedo.dds", StringComparison.CurrentCultureIgnoreCase) &&
					x.Name.IndexOf("LOD1", StringComparison.CurrentCultureIgnoreCase) == -1);
		lod.normalsTS = (normalsTSPath != null) ? new FileInfo(normalsTSPath) : directoryFiles.FirstOrDefault(
			x =>	x.Name.EndsWith("NormalsTS.dds", StringComparison.CurrentCultureIgnoreCase) &&
					x.Name.IndexOf("LOD1", StringComparison.CurrentCultureIgnoreCase) == -1);
		lod.specTeam = (specTeamPath != null) ? new FileInfo(specTeamPath) : directoryFiles.FirstOrDefault(
			x =>	x.Name.EndsWith("SpecTeam.dds", StringComparison.CurrentCultureIgnoreCase) &&
					x.Name.IndexOf("LOD1", StringComparison.CurrentCultureIgnoreCase) == -1);
		lod.shaderName = shaderName;

		return lod;
	}

	private List<FileInfo> GetSCAFiles()
	{
		List<LuaValue> scaValues = _blueprint.FindAll(x => x.LVT == LuaValueType.LVT_String && x.StringValue.EndsWith(".sca"));
		List<FileInfo> scaFiles = new List<FileInfo>();
		foreach (LuaValue val in scaValues)
		{
			string path = FAGameComponent.Instance.MakeAbsolutePath(val.StringValue);
			if (path != null)
			{
				FileInfo scaFile = new FileInfo(path);
				if (scaFile.Exists)
				{
					scaFiles.Add(scaFile);
				}
			}
		}
		return scaFiles;
	}
}

public struct LODFiles
{
	public FileInfo mesh;
	public FileInfo albedo;
	public FileInfo normalsTS;
	public FileInfo specTeam;
	public string shaderName;
}
