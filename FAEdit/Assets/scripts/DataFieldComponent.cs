using UnityEngine;
using UnityEngine.UI;

public class DataFieldComponent : MonoBehaviour
{
	public string FieldLabelString
	{
		get
		{
			return _fieldLabel.text;
		}
		set
		{
			_fieldLabel.text = value;
		}
	}

	public string FieldInputString
	{
		get
		{
			return _fieldInput.text;
		}
		set
		{
			_fieldInput.text = value;
		}
	}

	public float FieldWidth
	{
		get
		{
			return GetComponent<RectTransform>().sizeDelta.x;
		}
		set
		{
			RectTransform rectTransform = GetComponent<RectTransform>();
			Vector2 dimensions = rectTransform.sizeDelta;
			dimensions.x = value;
			rectTransform.sizeDelta = dimensions;
		}
	}

	private Text _fieldLabel;
	private InputField _fieldInput;

	public void Start()
	{
		_fieldLabel = transform.FindChild("FieldLabel").GetComponent<Text>();
		_fieldInput = transform.FindChild("FieldInput").GetComponent<InputField>();
	}
}
