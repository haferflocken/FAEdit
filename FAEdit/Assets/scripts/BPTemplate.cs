using UnityEngine;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public enum TemplateValueType
{
	TVT_Invalid,
	TVT_String,
	TVT_Float,
	TVT_Int,
	TVT_Bool,
	TVT_Reference,
	TVT_List,
	TVT_Map,
}

public class TemplateValue
{
	private static Regex ReferenceRegex = new Regex("\\w+");
	private static Regex ListRegex = new Regex("list\\s*(\\s*\\w+\\s*)");
	private static Regex MapRegex = new Regex("map\\s*(\\s*\\w+\\s*->\\s*\\w+\\s*)");

	public static TemplateValue MakeFromString(string input)
	{
		// string
		// float
		// int
		// bool
		// reference: basically just any plain old word
		// list(TemplateValueType)
		// map(TemplateValueType -> TemplateValueType)

		TemplateValueType type = TemplateValueType.TVT_Invalid;
		if (input == "string")
		{
			type = TemplateValueType.TVT_String;
		}
		else if (input == "float")
		{
			type = TemplateValueType.TVT_Float;
		}
		else if (input == "int")
		{
			type = TemplateValueType.TVT_Int;
		}
		else if (input == "bool")
		{
			type = TemplateValueType.TVT_Bool;
		}
		else if (ReferenceRegex.IsMatch(input))
		{
			type = TemplateValueType.TVT_Reference;
		}
		else if (ListRegex.IsMatch(input))
		{
			type = TemplateValueType.TVT_List;
		}
		else if (MapRegex.IsMatch(input))
		{
			type = TemplateValueType.TVT_Map;
		}
		else
		{
			return null;
		}

		string referencedType = null;
		TemplateValue keyType = null;
		TemplateValue valueType = null;
		switch (type)
		{
		case TemplateValueType.TVT_String:
		case TemplateValueType.TVT_Float:
		case TemplateValueType.TVT_Int:
		case TemplateValueType.TVT_Bool:
			break;

		case TemplateValueType.TVT_Reference:
		{
			referencedType = input;
			break;
		}
		case TemplateValueType.TVT_List:
		{
			int leftParenIndex = input.IndexOf('(');
			int rightParenIndex = input.LastIndexOf(')');
			string rawListType = input.Substring(leftParenIndex, rightParenIndex - leftParenIndex);
			valueType = MakeFromString(rawListType);
			break;
		}
		case TemplateValueType.TVT_Map:
		{
			int leftParenIndex = input.IndexOf('(');
			int arrowIndex = input.IndexOf("->");
			int rightParenIndex = input.LastIndexOf(')');

			string rawMapKeyType = input.Substring(leftParenIndex, arrowIndex - leftParenIndex).Trim();
			keyType = MakeFromString(rawMapKeyType);

			string rawMapValueType = input.Substring(arrowIndex + 2, rightParenIndex - (arrowIndex + 2)).Trim();
			valueType = MakeFromString(rawMapValueType);
			break;
		}
		default:
			Debug.LogError("Invalid template value type.");
			return null;
		}

		TemplateValue value = new TemplateValue();
		value.Type = type;
		value.ReferencedType = referencedType;
		value.KeyType = keyType;
		value.ValueType = valueType;

		return value;
	}

	public TemplateValueType Type { get; private set; }
	public string ReferencedType { get; private set; }
	public TemplateValue KeyType { get; private set; }
	public TemplateValue ValueType { get; private set; }

	private TemplateValue()
	{
		Type = TemplateValueType.TVT_Invalid;
		ReferencedType = null;
		KeyType = null;
		ValueType = null;
	}
}

public class BPTemplate
{
	
}
