using UnityEngine;
using System.Text;
using System.IO;
using System.Collections.Generic;

public static class BPImporter
{
	private struct TableBuilder
	{
		public LuaTable table;
		public Stack<string> identifierStack;

		public void PushID(string id)
		{
			identifierStack.Push(id);
		}

		public string PopID()
		{
			if (identifierStack.Count > 0)
			{
				return identifierStack.Pop();
			}
			return null;
		}

		public string PeekID()
		{
			return identifierStack.Peek();
		}
	}

	public static void Load(GameObject gameObject, FileInfo bpFile)
	{
		BPComponent bpComponent = gameObject.GetComponent<BPComponent>();

		using (StreamReader reader = new StreamReader(bpFile.OpenRead(), Encoding.UTF8))
		{
			Stack<TableBuilder> tableStack = new Stack<TableBuilder>();

			StringBuilder input = new StringBuilder();
			while (reader.Peek() >= 0)
			{
				char c = (char)reader.Read();
				input.Append(c);

				// Skip over whitespace.
				if (char.IsWhiteSpace(c))
				{
					while (reader.Peek() >= 0)
					{
						char next = (char)reader.Peek();
						if (char.IsWhiteSpace(next))
						{
							reader.Read();
						}
						else
						{
							break;
						}
					}
				}

				c = input[input.Length - 1];
				// Equals denotes the end of an identifier.
				if (c == '=')
				{
					input.Remove(input.Length - 1, 1);
					string id = input.ToString();
					id = id.Trim();
					input.Remove(0, input.Length);
					if (id.Length > 0)
					{
						tableStack.Peek().PushID(id);
					}
				}
				// Number signs and -- indicate a comment.
				else if (c == '#')
				{
					input.Remove(input.Length - 1, 1);
					reader.ReadLine();
				}
				else if (c == '-')
				{
					if (reader.Peek() == '-')
					{
						input.Remove(input.Length - 1, 1);
						reader.ReadLine();
					}
				}
				// Quotes indicate the beginning of a string.
				else if (c == '\"')
				{
					input.Remove(input.Length - 1, 1);
					string val = ReadString(reader, '\"');
					if (val == null)
					{
						Debug.LogError("String parse error.");
						return;
					}
					TableBuilder builder = tableStack.Peek();
					EncounteredString(builder.table, builder.PopID(), val);
				}
				else if (c == '\'')
				{
					input.Remove(input.Length - 1, 1);
					string val = ReadString(reader, '\'');
					if (val == null)
					{
						Debug.LogError("String parse error.");
						return;
					}
					TableBuilder builder = tableStack.Peek();
					EncounteredString(builder.table, builder.PopID(), val);
				}
				// Curly brace indicates a table.
				else if (c == '{')
				{
					input.Remove(input.Length - 1, 1);
					string tableName = input.ToString();
					tableName = tableName.Trim();
					input.Remove(0, input.Length);
					if (tableName.Length == 0)
					{
						tableName = null;
					}

					StartTable(tableStack, tableName);
				}
				else if (c == '}')
				{
					input.Remove(input.Length - 1, 1);
					LuaValue root = EndTable(tableStack);
					if (root != null)
					{
						bpComponent.SetRoot(root);
						break;
					}
				}
				// Whitespace or comma denotes the end of a token.
				else if (char.IsWhiteSpace(c) || c == ',')
				{
					input.Remove(input.Length - 1, 1);
					string val = input.ToString();
					val = val.Trim();
					input.Remove(0, input.Length);
					
					if (val.Length > 0)
					{
						if (val == "true")
						{
							TableBuilder builder = tableStack.Peek();
							EncounteredBool(builder.table, builder.PopID(), true);
						}
						else if (val == "false")
						{
							TableBuilder builder = tableStack.Peek();
							EncounteredBool(builder.table, builder.PopID(), false);
						}
						else if (val == "nil")
						{
							TableBuilder builder = tableStack.Peek();
							EncounteredNil(builder.table, builder.PopID());
						}
						else
						{
							float f = 0.0f;
							if (float.TryParse(val, out f))
							{
								TableBuilder builder = tableStack.Peek();
								EncounteredFloat(builder.table, builder.PopID(), f);
							}
							else if (c != ',')
							{
								input.Append(val);
							}
							else
							{
								Debug.LogError("Token parse error: " + val + " " + c);
								//input.Append(val);
							}
						}
					}
				}
			}
		}
	}

	private static string ReadString(StreamReader reader, char delimiter)
	{
		StringBuilder buffer = new StringBuilder();
		int c = reader.Read();
		while (c >= 0)
		{
			if (c == delimiter)
			{
				return buffer.ToString();
			}
			else if (c == '\\')
			{
				int next = reader.Read();
				if (next == -1)
				{
					return null;
				}
				else if (next == 'n')
				{
					buffer.Append('\n');
				}
				else if (next == '\\')
				{
					buffer.Append('\\');
				}
				else if (next == '\'')
				{
					buffer.Append('\'');
				}
				else if (next == '\"')
				{
					buffer.Append('\"');
				}
				// TODO(jwerner) More escape characters.
			}
			else
			{
				buffer.Append((char)c);
			}
			c = reader.Read();
		}
		return null;
	}

	private static void StartTable(Stack<TableBuilder> tableStack, string tableName)
	{
		LuaTable table = new LuaTable(tableName, new Dictionary<string, LuaValue>());
		TableBuilder builder;
		builder.table = table;
		builder.identifierStack = new Stack<string>();
		tableStack.Push(builder);
	}

	private static LuaValue EndTable(Stack<TableBuilder> tableStack)
	{
		TableBuilder builder = tableStack.Pop();
		LuaValue value = new LuaValue(builder.table);
		if (tableStack.Count == 0)
		{
			return value;
		}

		TableBuilder parentBuilder = tableStack.Peek();
		string identifier = parentBuilder.PopID();
		if (identifier == null)
		{
			identifier = parentBuilder.table.Count.ToString();
		}

		if (parentBuilder.table.ContainsKey(identifier))
		{
			Debug.LogError(identifier);
		}
		parentBuilder.table.Add(identifier, value);
		return null;
	}

	private static void EncounteredBool(LuaTable currentTable, string currentIdentifier, bool val)
	{
		if (currentIdentifier == null)
		{
			currentIdentifier = currentTable.Count.ToString();
		}
		currentTable.Add(currentIdentifier, new LuaValue(val));
	}

	private static void EncounteredNil(LuaTable currentTable, string currentIdentifier)
	{
		if (currentIdentifier == null)
		{
			currentIdentifier = currentTable.Count.ToString();
		}
		currentTable.Add(currentIdentifier, new LuaValue());
	}

	private static void EncounteredFloat(LuaTable currentTable, string currentIdentifier, float val)
	{
		if (currentIdentifier == null)
		{
			currentIdentifier = currentTable.Count.ToString();
		}
		currentTable.Add(currentIdentifier, new LuaValue(val));
	}

	private static void EncounteredString(LuaTable currentTable, string currentIdentifier, string val)
	{
		if (currentIdentifier == null)
		{
			currentIdentifier = currentTable.Count.ToString();
		}
		currentTable.Add(currentIdentifier, new LuaValue(val));
	}

}
