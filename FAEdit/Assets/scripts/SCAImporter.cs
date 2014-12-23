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

			// Create an animation curve for the position and rotation of each bone (7 properties).
			AnimationClip clip = new AnimationClip();

			for (int i = 0; i < bones.Length; ++i)
			{
				AnimationCurve posXCurve = new AnimationCurve();
				AnimationCurve posYCurve = new AnimationCurve();
				AnimationCurve posZCurve = new AnimationCurve();
				AnimationCurve rotXCurve = new AnimationCurve();
				AnimationCurve rotYCurve = new AnimationCurve();
				AnimationCurve rotZCurve = new AnimationCurve();
				AnimationCurve rotWCurve = new AnimationCurve();

				for (int j = 0; j < keyFrames.Length; ++j)
				{
					float time = keyFrames[j].Time;

					Vector3 pos = keyFrames[j].BonePositions[i];
					posXCurve.AddKey(time, pos.x);
					posYCurve.AddKey(time, pos.y);
					posZCurve.AddKey(time, pos.z);

					Quaternion rot = keyFrames[j].BoneOrientations[i];
					rotXCurve.AddKey(time, rot.x);
					rotYCurve.AddKey(time, rot.y);
					rotZCurve.AddKey(time, rot.z);
					rotWCurve.AddKey(time, rot.w);
				}

				StringBuilder bonePathBuffer = new StringBuilder();
				SCABone curBone = bones[i];
				while (curBone != null)
				{
					bonePathBuffer.Insert(0, curBone.Name + "/");

					if (curBone.ParentIndex != -1)
					{
						curBone = bones[curBone.ParentIndex];
					}
					else
					{
						curBone = null;
					}
				}
				if (bonePathBuffer.Length > 0)
				{
					bonePathBuffer.Remove(bonePathBuffer.Length - 1, 1);
				}
				string bonePath = bonePathBuffer.ToString();
				Debug.Log(bonePath);

				clip.SetCurve(bonePath, typeof(Transform), "localPosition.x", posXCurve);
				clip.SetCurve(bonePath, typeof(Transform), "localPosition.y", posYCurve);
				clip.SetCurve(bonePath, typeof(Transform), "localPosition.z", posZCurve);
				clip.SetCurve(bonePath, typeof(Transform), "localRotation.x", rotXCurve);
				clip.SetCurve(bonePath, typeof(Transform), "localRotation.y", rotYCurve);
				clip.SetCurve(bonePath, typeof(Transform), "localRotation.z", rotZCurve);
				clip.SetCurve(bonePath, typeof(Transform), "localRotation.w", rotWCurve);
			}

			clip.EnsureQuaternionContinuity();
			clip.wrapMode = WrapMode.Loop;

			// Apply the clip to the animation component.
			Animation animComponent = gameObject.GetComponentInParent<Animation>();
			animComponent.AddClip(clip, scaFile.Name);
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
	public int ParentIndex { get; private set; }

	public SCABone()
	{
	}

	public void LoadName(BinaryReader reader)
	{
		Name = reader.ReadNullTerminatedString();
	}

	public void LoadParentIndex(BinaryReader reader)
	{
		ParentIndex = reader.ReadInt32();
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

