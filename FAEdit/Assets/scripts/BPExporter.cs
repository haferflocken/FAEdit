using UnityEngine;
using System.Text;
using System.IO;
using System.Collections.Generic;

public static class BPExporter
{
	public static void Save(BPComponent blueprint, FileInfo blueprintFile)
	{
		using (StreamWriter writer = new StreamWriter(blueprintFile.OpenWrite(), Encoding.UTF8))
		{
			SaveLuaValue(blueprint.Get(""), writer, "	");
		}
	}

	private static void SaveLuaValue(LuaValue luaValue, StreamWriter writer, string tableIndent)
	{
		switch (luaValue.LVT)
		{
		case LuaValueType.LVT_Nil:
		{
			writer.Write("Nil");
			break;
		}
		case LuaValueType.LVT_String:
		{
			writer.Write('\'');

			string sanitizedString = luaValue.StringValue;
			sanitizedString.Replace("'", "\\'");
			sanitizedString.Replace("\"", "\\\"");

			writer.Write(sanitizedString);
			writer.Write('\'');
			break;
		}
		case LuaValueType.LVT_Float:
		{
			writer.Write(luaValue.FloatValue);
			break;
		}
		case LuaValueType.LVT_Bool:
		{
			writer.Write(luaValue.BoolValue ? "true" : "false");
			break;
		}
		case LuaValueType.LVT_Table:
		{
			LuaTable table = luaValue.TableValue;

			if (table.Name != null)
			{
				writer.Write(table.Name);
				writer.Write(' ');
			}
			writer.Write("{\n");

			ICollection<string> tableKeys = table.Keys;
			foreach (string tableKey in tableKeys)
			{
				writer.Write(tableIndent);

				writer.Write(tableKey);
				writer.Write(" = ");

				LuaValue tableValue = null;
				if (table.TryGetValue(tableKey, out tableValue))
				{
					SaveLuaValue(tableValue, writer, tableIndent + "	");
				}
				else
				{
					writer.Write("TABLE SAVE FAILURE");
				}

				writer.Write(",\n");
			}

			writer.Write(tableIndent.Substring(1));
			writer.Write("}");

			break;
		}
		default:
			Debug.LogError("Invalid LVT.");
			break;
		}
	}
}
