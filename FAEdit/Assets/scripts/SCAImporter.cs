using UnityEngine;
using System.IO;
using System.Text;

public class SCAImporter
{
	public static void Load(GameObject gameObject, FileInfo scaFile)
	{
		using (BinaryReader reader = new BinaryReader(scaFile.OpenRead(), Encoding.UTF8))
		{
			SCAHeader header = new SCAHeader();
			header.Load(reader);

			// Read in the bones.
			reader.BaseStream.Seek(header.BoneNamesOffset, SeekOrigin.Begin);

			SCABone[] bones = new SCABone[header.NumBones];
			for (uint i = 0; i < header.NumBones; ++i)
			{
				bones[i] = new SCABone();
				bones[i].LoadName(reader);
			}

			reader.BaseStream.Seek(header.BoneLinksOffset, SeekOrigin.Begin);

			for (uint i = 0; i < header.NumBones; ++i)
			{
				bones[i].LoadParentIndex(reader);
			}

			// Read in the actual animation data.
			reader.BaseStream.Seek(header.AnimDataOffset, SeekOrigin.Begin);

			Vector3 totalPositionDelta;
			Quaternion totalOrientationDelta;

			reader.ReadVector(out totalPositionDelta);
			reader.ReadQuaternion(out totalOrientationDelta);

			SCAKeyFrame[] keyFrames = new SCAKeyFrame[header.NumFrames];
			for (uint i = 0; i < header.NumFrames; ++i)
			{
				keyFrames[i] = new SCAKeyFrame();
				keyFrames[i].Load(reader, header);
			}

			// TODO(jwerner) Apply this to the Unity animation.
		}
	}
}

public class SCAHeader
{
	public string Marker { get; private set; }
	public uint Version { get; private set; }
	public uint NumFrames { get; private set; }
	public float Duration { get; private set; } // The animation plays at (NumFrames - 1) / Duration FPS.
	public uint NumBones { get; private set; }
	public uint BoneNamesOffset { get; private set; }
	public uint BoneLinksOffset { get; private set; }
	public uint AnimDataOffset { get; private set; }
	public uint FrameSize { get; private set; } // The number of bytes in one animation frame.

	public SCAHeader()
	{
	}

	public void Load(BinaryReader reader)
	{
		Marker = new string(reader.ReadChars(4));
		Version = reader.ReadUInt32();
		NumFrames = reader.ReadUInt32();
		Duration = reader.ReadSingle();
		NumBones = reader.ReadUInt32();
		BoneNamesOffset = reader.ReadUInt32();
		BoneLinksOffset = reader.ReadUInt32();
		AnimDataOffset = reader.ReadUInt32();
		FrameSize = reader.ReadUInt32();

		if (!IsValid())
		{
			Debug.LogWarning("SCA header failed validation.");
		}
	}

	public bool IsValid()
	{
		return Marker == "ANIM";
	}
}

public class SCABone
{
	public string Name { get; private set; }
	public uint ParentIndex { get; private set; }

	public SCABone()
	{
	}

	public void LoadName(BinaryReader reader)
	{
		Name = reader.ReadNullTerminatedString();
	}

	public void LoadParentIndex(BinaryReader reader)
	{
		ParentIndex = reader.ReadUInt32();
	}
}

public class SCAKeyFrame
{
	public float Time { get; private set; } // TODO(jwerner) is this a duration or a time?
	public uint Flags { get; private set; } // Unused by the SCA format.

	public Vector3[] BonePositions { get; private set; }
	public Quaternion[] BoneOrientations { get; private set; }

	public SCAKeyFrame()
	{
	}

	public void Load(BinaryReader reader, SCAHeader header)
	{
		Time = reader.ReadSingle();
		Flags = reader.ReadUInt32();

		BonePositions = new Vector3[header.NumBones];
		BoneOrientations = new Quaternion[header.NumBones];
		for (uint i = 0; i < header.NumBones; ++i)
		{
			reader.ReadVector(out BonePositions[i]);
			reader.ReadQuaternion(out BoneOrientations[i]);
		}
	}
}

