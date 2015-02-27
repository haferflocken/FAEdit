using UnityEngine;
using System.Text;
using System.Collections.Generic;

public class BPComponent : MonoBehaviour
{
	private LuaValue _root = null;

	public bool SetRoot(LuaValue newRoot)
	{
		if (_root == null)
		{
			_root = newRoot;
			Debug.Log(_root);
			return true;
		}
		else
		{
			Debug.LogError("Attempting to set root more than once.");
			return false;
		}
	}

	public LuaValue Get(string key)
	{
		return _root.Get(key);
	}

	public string GetString(string key)
	{
		LuaValue value = Get(key);
		if (value != null)
		{
			return value.StringValue;
		}
		return null;
	}

	public delegate bool FindDelegate(LuaValue val);
	public List<LuaValue> FindAll(FindDelegate condition)
	{
		List<LuaValue> foundValues = new List<LuaValue>();
		Stack<LuaValue> stack = new Stack<LuaValue>();
		stack.Push(_root);
		while (stack.Count > 0)
		{
			LuaValue val = stack.Pop();
			if (condition(val))
			{
				foundValues.Add(val);
			}
			if (val.LVT == LuaValueType.LVT_Table)
			{
				ICollection<LuaValue> pushVals = val.TableValue.Values;
				foreach (LuaValue v in pushVals)
				{
					stack.Push(v);
				}
			}
		}
		return foundValues;
	}
}

public enum LuaValueType
{
	LVT_Nil = 0,
	LVT_String = 1,
	LVT_Float = 2,
	LVT_Bool = 3,
	LVT_Table = 4,
}

public class LuaValue
{
	public LuaValueType LVT { get; private set; }

	public bool IsNil { get; private set; }
	public string StringValue { get; private set; }
	public float FloatValue { get; private set; }
	public bool BoolValue { get; private set; }
	public LuaTable TableValue { get; private set; }

	public LuaValue()
	{
		Init();
	}

	public LuaValue(string str)
	{
		Init();
		IsNil = false;
		StringValue = str;
		LVT = LuaValueType.LVT_String;
	}

	public LuaValue(float flt)
	{
		Init();
		IsNil = false;
		FloatValue = flt;
		LVT = LuaValueType.LVT_Float;
	}

	public LuaValue(bool bl)
	{
		Init();
		IsNil = false;
		BoolValue = bl;
		LVT = LuaValueType.LVT_Bool;
	}

	public LuaValue(LuaTable tbl)
	{
		Init();
		IsNil = false;
		TableValue = tbl;
		LVT = LuaValueType.LVT_Table;
	}

	private void Init()
	{
		IsNil = true;
		StringValue = null;
		FloatValue = 0.0f;
		BoolValue = false;
		TableValue = null;
	}

	public LuaValue Get(string key)
	{
		if (key == "")
		{
			return this;
		}

		switch (LVT)
		{
		case LuaValueType.LVT_Nil:
		case LuaValueType.LVT_String:
		case LuaValueType.LVT_Float:
		case LuaValueType.LVT_Bool:
			return null;

		case LuaValueType.LVT_Table:
		{
			int dotIndex = key.IndexOf('.');
			string currentKey = key;
			if (dotIndex != -1)
			{
				currentKey = key.Substring(0, dotIndex);
			}

			LuaValue val = null;
			if (TableValue.TryGetValue(currentKey, out val))
			{
				if (dotIndex == -1)
				{
					return val;
				}
				else
				{
					string nextKey = key.Substring(dotIndex + 1);
					return val.Get(nextKey);
				}
			}

			return null;
		}
		default:
			Debug.LogError("Invalid LVT.");
			return null;
		}
	}

	public override string ToString()
	{
		return ToString("");
	}

	public string ToString(string indent)
	{
		switch (LVT)
		{
		case LuaValueType.LVT_Nil:
			return "nil";
		case LuaValueType.LVT_String:
			return '\"' + StringValue + '\"';
		case LuaValueType.LVT_Float:
			return FloatValue.ToString();
		case LuaValueType.LVT_Bool:
			return BoolValue.ToString();
		case LuaValueType.LVT_Table:
			return TableValue.ToString(indent);
		default:
			return indent + "INVALID LUAVALUE";
		}
	}
}

public class LuaTable
{
	public string Name { get; private set; }
	private IDictionary<string, LuaValue> _table;

	public LuaTable(string name, IDictionary<string, LuaValue> tbl)
	{
		Name = name;
		_table = tbl;
	}

	public void Add(string key, LuaValue value)
	{
		_table.Add(key, value);
	}

	public bool Remove(string key)
	{
		return _table.Remove(key);
	}

	public bool ContainsKey(string key)
	{
		return _table.ContainsKey(key);
	}

	public bool TryGetValue(string key, out LuaValue value)
	{
		return _table.TryGetValue(key, out value);
	}

	public int Count
	{
		get { return _table.Count; }
	}

	public ICollection<string> Keys
	{
		get { return _table.Keys; }
	}

	public ICollection<LuaValue> Values
	{
		get { return _table.Values; }
	}

	public override string ToString()
	{
		return ToString("");
	}

	public string ToString(string indent)
	{
		StringBuilder buffer = new StringBuilder();
		if (Name != null)
		{
			buffer.Append(Name);
			buffer.Append(' ');
		}
		buffer.Append("{\n");
		foreach (KeyValuePair<string, LuaValue> entry in _table)
		{
			buffer.Append(indent + "    ");
			buffer.Append(entry.Key + " = " + entry.Value.ToString(indent + "    ") + ",\n");
		}
		buffer.Append(indent);
		buffer.Append("}");
		return buffer.ToString();
	}
}