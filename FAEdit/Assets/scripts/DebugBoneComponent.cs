using UnityEngine;
using System.Collections.Generic;

// This class draws a given skeleton in the scene.
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

	void OnPostRender()
	{
		LineMaterial.SetPass(0);
		GL.Begin(GL.LINES);
		GL.Color(Color.red);

		GameObject[] allBones = GameObject.FindGameObjectsWithTag("debug_bone");
		foreach (GameObject bone in allBones)
		{
			Transform t = bone.transform;
			if (t.parent != null)
			{
				GL.Vertex(t.parent.position);
				GL.Vertex(t.position);
			}
		}

		GL.End();
	}
}
