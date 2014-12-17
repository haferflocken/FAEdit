using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

public class FAUnitComponent : MonoBehaviour
{
	private struct LODFiles
	{
		public FileInfo mesh;
		public FileInfo albedo;
		public FileInfo normalsTS;
		public FileInfo specTeam;
	}

	public string _directoryPath;
	private BPComponent _blueprint;

	void Start()
	{
		// Verify that the directory exists and grab its files.
		DirectoryInfo directory = new DirectoryInfo(_directoryPath);
		if (!directory.Exists)
		{
			Debug.LogError(_directoryPath + " does not exist.");
			return;
		}
		IEnumerable<FileInfo> directoryFiles = directory.GetFiles();

		// Load the BP, so we know what mesh, animations, and textures to load and what shaders to load.
		FileInfo blueprintFile = directoryFiles.FirstOrDefault(x => x.Name.EndsWith("_unit.bp", StringComparison.CurrentCultureIgnoreCase));
		if (blueprintFile == null || !blueprintFile.Exists)
		{
			Debug.LogError(_directoryPath + " does not contain a unit blueprint.");
			return;
		}
		BPImporter.Load(gameObject, blueprintFile);
		_blueprint = GetComponent<BPComponent>();

		// Get the LOD0 files and load them.
		LODFiles lod0 = GetLODFiles(0, directoryFiles);
		if (lod0.mesh == null)
		{
			Debug.LogError(_directoryPath + " does not contain an LOD0.scm file.");
			return;
		}
		SCMImporter.Load(gameObject, lod0.mesh);
		DDSImporter.Load(gameObject, lod0.albedo, lod0.normalsTS, lod0.specTeam);

		// TODO Load the SCAs referenced in the blueprint.
		List<string> scaPaths = GetSCAReferences();

		// Apply the blueprint's scale.
		LuaValue scaleVal = _blueprint.Get("Display.UniformScale");
		float scale = (scaleVal == null || scaleVal.LVT != LuaValueType.LVT_Float) ? 1.0f : scaleVal.FloatValue;
		transform.root.localScale = new Vector3(scale, scale, scale);
	}

	private LODFiles GetLODFiles(int level, IEnumerable<FileInfo> directoryFiles)
	{
		string blueprintPath = "Display.Mesh.LODs." + level;
		string meshPath = _blueprint.GetString(blueprintPath + ".MeshName"); // TODO(jwerner) these paths are relative to game root; needs correction.
		string albedoPath = _blueprint.GetString(blueprintPath + ".AlbedoName");
		string normalsTSPath = _blueprint.GetString(blueprintPath + ".NormalsName");
		string specTeamPath = _blueprint.GetString(blueprintPath + ".SpecularName");

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

		return lod;
	}

	private List<string> GetSCAReferences()
	{
		List<LuaValue> scaValues = _blueprint.FindAll(x => x.LVT == LuaValueType.LVT_String && x.StringValue.EndsWith(".sca"));
		List<string> scaStrings = new List<string>();
		foreach (LuaValue val in scaValues)
		{
			scaStrings.Add(val.StringValue);
		}
		return scaStrings;
	}
}
