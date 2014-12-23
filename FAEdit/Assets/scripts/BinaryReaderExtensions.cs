using UnityEngine;
using System.IO;
using System.Text;

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

	public static void ReadQuaternion(this BinaryReader reader, out Quaternion quaternion)
	{
		quaternion.x = reader.ReadSingle();
		quaternion.y = reader.ReadSingle();
		quaternion.z = reader.ReadSingle();
		quaternion.w = reader.ReadSingle();
	}

	public static string ReadNullTerminatedString(this BinaryReader reader)
	{
		StringBuilder buffer = new StringBuilder();
		while (reader.PeekChar() != '\0')
		{
			buffer.Append(reader.ReadChar());
		}
		reader.ReadChar();
		return buffer.ToString();
	}
}

