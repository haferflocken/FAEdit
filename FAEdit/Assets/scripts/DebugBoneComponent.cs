using UnityEngine;
using System.Collections.Generic;

// This class draws all the skeletons in the scene.
public class DebugBoneComponent : MonoBehaviour
{
	private Material _lineMaterial = null;
	private Material LineMaterial
	{
		get
		{
			if (_lineMaterial == null)
			{
				_lineMaterial = new Material("Shader \"Lines/Colored Blended\" {" +
					"SubShader { Pass { " +
					"    Blend SrcAlpha OneMinusSrcAlpha " +
					"    ZWrite Off Cull Off Fog { Mode Off } " +
					"    BindChannels {" +
					"      Bind \"vertex\", vertex Bind \"color\", color }" +
					"} } }");
				_lineMaterial.hideFlags = HideFlags.HideAndDontSave;
				_lineMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
			}
			return _lineMaterial;
		}
	}

	public void OnPostRender()
	{
		GameObject[] allBones = GameObject.FindGameObjectsWithTag("debug_bone");
		int maxDepth = 1;
		foreach (GameObject bone in allBones)
		{
			int depth = CalcDepth(bone.transform);
			if (depth > maxDepth)
			{
				maxDepth = depth;
			}
		}

		LineMaterial.SetPass(0);
		GL.Begin(GL.LINES);

		foreach (GameObject bone in allBones)
		{
			Transform t = bone.transform;
			int depth = CalcDepth(t);

			if (depth > 0)
			{
				float lerpAmount = ((float)depth)/((float)maxDepth);
				GL.Color(Color.Lerp(Color.red, Color.blue, lerpAmount));
				GL.Vertex(t.parent.position);
				GL.Vertex(t.position);
			}
		}

		GL.End();
	}

	private int CalcDepth(Transform t)
	{
		int depth = 0;
		while (t.parent != null)
		{
			t = t.parent;
			++depth;
		}
		return depth;
	}
}
