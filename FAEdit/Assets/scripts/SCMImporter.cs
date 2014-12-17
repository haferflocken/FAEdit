﻿using UnityEngine;
using System.IO;
using System.Text;

// This file is based on the import script for SCMs and SCAs for Blender from
// https://github.com/Oygron/SupCom_Import_Export_Blender
// which is licensed under the CC BY license.

public class SCMImporter
{
	/*
	 * SCM Format:
	 * 120 byte header
	 * 8 bytes of ???
	 * Bone names until the bone spatial data
	 */
	public static void Load(GameObject gameObject, FileInfo scmFile)
	{
		SkinnedMeshRenderer meshRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();

		using(BinaryReader reader = new BinaryReader(scmFile.OpenRead(), Encoding.UTF8))
		{
			SCMHeader header = new SCMHeader();
			header.Load(reader);

			// What the fuck is this? I do not understand the corresponding Python.
			int headerSize = 120;
			//reader.ReadBytes(Pad(headerSize));
			//int numBoneNamesBytes = (header.BoneOffset - 4) - headerSize + Pad(headerSize);

			reader.BaseStream.Seek(Pad(headerSize), SeekOrigin.Current); // This goes forward by 8 bytes. What are those 8 bytes?
			int numBoneNamesBytes = (int)(header.BoneOffset - 4) - (int)(reader.BaseStream.Position);

			// Read in the bone names.
			string rawBoneNames = new string(reader.ReadChars(numBoneNamesBytes));
			string[] boneNames = rawBoneNames.Split("\0".ToCharArray());

			// Read in the bones.
			reader.BaseStream.Seek(header.BoneOffset, SeekOrigin.Begin);
			SCMBone[] bones = new SCMBone[header.TotalBoneCount];
			for (int i = 0; i < bones.Length; ++i)
			{
				bones[i] = new SCMBone(boneNames[i]);
				bones[i].Load(reader);
			}

			// Set parent bones.
			// TODO this potentially does not traverse the bones in the correct order (it's a tree, after all). That should be figured out.
			foreach (SCMBone bone in bones)
			{
				if (bone.ParentIndex != -1)
				{
					SCMBone parent = bones[bone.ParentIndex];
					bone.TransformMatrix = bone.TransformMatrix * parent.TransformMatrix.inverse;
				}
			}

			// Read vertex data.
			reader.BaseStream.Seek(header.VertexOffset, SeekOrigin.Begin);
			Vector3[] vertexes = new Vector3[header.VertexCount];
			Vector4[] tangents = new Vector4[header.VertexCount];
			Vector3[] normals = new Vector3[header.VertexCount];
			Vector3[] binormals = new Vector3[header.VertexCount];
			Vector2[] uv1 = new Vector2[header.VertexCount];
			Vector2[] uv2 = new Vector2[header.VertexCount];
			BoneWeight[] weights = new BoneWeight[header.VertexCount];
			for (int i = 0; i < header.VertexCount; ++i)
			{
				reader.ReadVector(out vertexes[i]);
				tangents[i].x = reader.ReadSingle();
				tangents[i].y = reader.ReadSingle();
				tangents[i].z = reader.ReadSingle();
				tangents[i].w = 1f;
				reader.ReadVector(out normals[i]);
				reader.ReadVector(out binormals[i]); // TODO Unity calculates its own binormals. Don't bother reading them.
				reader.ReadVector(out uv1[i]);
				reader.ReadVector(out uv2[i]);

				// Correct flipped y coordinates in the UVs.
				//uv1[i].y = 1.0f - uv1[i].y;
				//uv2[i].y = 1.0f - uv2[i].y;

				int boneIndex = (int)reader.ReadUInt32();
				weights[i].boneIndex0 = boneIndex;
				weights[i].weight0 = 1f;
			}

			// Read triangles.
			reader.BaseStream.Seek(header.IndexOffset, SeekOrigin.Begin);
			int[] triangles = new int[header.IndexCount];
			for (int i = 0; i < triangles.Length; ++i)
			{
				triangles[i] = reader.ReadInt16();
			}

			// TODO(maybe) Read info.

			// Assign all this data to the mesh.
			Mesh mesh = new Mesh();
			mesh.name = scmFile.Name;
			mesh.vertices = vertexes;
			mesh.uv = uv1;
			mesh.uv2 = uv2;
			mesh.triangles = triangles;
			//mesh.normals = normals;  // TODO Something is wrong with the normals that are read in.
			mesh.RecalculateNormals(); // The calculated normals are really good. Once a real normal map is applied I'll know if they're perfect.
			mesh.tangents = tangents;
			mesh.boneWeights = weights;

			// Create a skeleton from the bone data.
			Transform[] boneTransforms = new Transform[bones.Length];
			Transform rootBone = null;
			Matrix4x4[] bindPoses = new Matrix4x4[bones.Length];
			for (int i = 0; i < bones.Length; ++i)
			{
				SCMBone bone = bones[i];

				boneTransforms[i] = new GameObject(bone.Name).transform;
				if (bone.ParentIndex != -1)
				{
					boneTransforms[i].parent = boneTransforms[bone.ParentIndex];
				}
				else if (rootBone == null)
				{
					GameObject topLevel = new GameObject(scmFile.Directory.Name);
					topLevel.AddComponent<Animation>();

					topLevel.transform.position = gameObject.transform.position;
					topLevel.transform.rotation = gameObject.transform.rotation;
					gameObject.transform.parent = topLevel.transform;
					gameObject.transform.localPosition = Vector3.zero;
					gameObject.transform.localRotation = Quaternion.identity;

					rootBone = boneTransforms[i];
					boneTransforms[i].parent = topLevel.transform;
				}
				else
				{
					Debug.LogError("Multiple root bones in armature.");
				}
				boneTransforms[i].localPosition = bone.Position;
				boneTransforms[i].localRotation = bone.Rotation;

				bindPoses[i] = boneTransforms[i].worldToLocalMatrix * gameObject.transform.localToWorldMatrix;
			}
			mesh.bindposes = bindPoses;
			meshRenderer.bones = boneTransforms;
			meshRenderer.rootBone = rootBone;
			meshRenderer.sharedMesh = mesh;

		}
	}

	private static int Pad(int size)
	{
		int val = 32 - (size % 32);
		val = val > 31 ? 0 : val;
		return val;
	}


}

public class SCMHeader
{
	public string Marker { get; private set; }
	public uint Version { get; private set; }
	public uint BoneOffset { get; private set; }
	public uint BoneCount { get; private set; }
	public uint VertexOffset { get; private set; }
	public uint ExtraVertexOffset { get; private set; }
	public uint VertexCount { get; private set; }
	public uint IndexOffset { get; private set; }
	public uint IndexCount { get; private set; }
	public uint TriangleCount { get; private set; }
	public uint InfoOffset { get; private set; }
	public uint InfoCount { get; private set; }
	public uint TotalBoneCount { get; private set; }

	public SCMHeader()
	{
	}

	public void Load(BinaryReader reader)
	{
		Marker = new string(reader.ReadChars(4));
		Version = reader.ReadUInt32();
		BoneOffset = reader.ReadUInt32();
		BoneCount = reader.ReadUInt32();
		VertexOffset = reader.ReadUInt32();
		ExtraVertexOffset = reader.ReadUInt32();
		VertexCount = reader.ReadUInt32();
		IndexOffset = reader.ReadUInt32();
		IndexCount = reader.ReadUInt32();
		TriangleCount = IndexCount / 3;
		InfoOffset = reader.ReadUInt32();
		InfoCount = reader.ReadUInt32();
		TotalBoneCount = reader.ReadUInt32();
	}
}

public class SCMBone
{
	public string Name { get; private set; }
	public Matrix4x4 TransformMatrix { get; set; } // Transform of the bone relative to the local origin of the mesh.
	public Vector3 Position { get; private set; } // Position relative to the parent bone.
	public Quaternion Rotation { get; private set; } // Rotation relative to the parent bone.
	public int ParentIndex { get; private set; }

	public SCMBone(string name)
	{
		Name = name;
		TransformMatrix = Matrix4x4.zero;
		Position = Vector3.zero;
		Rotation = Quaternion.identity;
		ParentIndex = -1;
	}

	public void Load(BinaryReader reader)
	{
		// Read the transform.
		// TODO This transform never ends up getting used, so I'm not sure how correct it is. A use should be found for it.
		for (int row = 0; row < 4; ++row)
		{
			for (int col = 0; col < 4; ++col)
			{
				Matrix4x4 matrix = TransformMatrix;
				matrix[row, col] = reader.ReadSingle();
			}
		}
		TransformMatrix = TransformMatrix.inverse;

		// Read the position.
		Vector3 newPos;
		reader.ReadVector(out newPos);
		Position = newPos;

		// Read the rotation.
		Rotation.Set(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

		// Read the parent index.
		reader.ReadInt32();
		ParentIndex = reader.ReadInt32();
		reader.ReadInt32();
		reader.ReadInt32();
	}
}

public static class BinaryReaderExtensions
{
	public static void ReadVector(this BinaryReader reader, out Vector2 vector)
	{
		vector.x = reader.ReadSingle();
		vector.y = reader.ReadSingle();
	}

	public static void ReadVector(this BinaryReader reader, out Vector3 vector)
	{
		vector.x = reader.ReadSingle();
		vector.y = reader.ReadSingle();
		vector.z = reader.ReadSingle();
	}
	
}
