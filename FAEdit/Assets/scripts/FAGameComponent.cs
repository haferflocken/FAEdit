using UnityEngine;

public class FAGameComponent : MonoBehaviour
{
	private static FAGameComponent _instance;
	public static FAGameComponent Instance
	{
		get
		{
			if (_instance == null)
			{
				GameObject gameObject = GameObject.Find("FAGame");
				if (gameObject == null)
				{
					Debug.LogError("Failed to find required GameObject: FAGame.");
					return null;
				}
				_instance = gameObject.GetComponent<FAGameComponent>();
				if (_instance == null)
				{
					Debug.LogError("Required GameObject FAGame requires a FAGameComponent.");
					return null;
				}
			}
			return _instance;
		}
	}

	public string _gameRootPath;

	public string MakeAbsolutePath(string relativePath)
	{
		if (relativePath == null || relativePath.Length == 0)
		{
			return null;
		}

		if (relativePath[0] == '/')
		{
			return _gameRootPath + relativePath;
		}
		return _gameRootPath + '/' + relativePath;
	}
}
