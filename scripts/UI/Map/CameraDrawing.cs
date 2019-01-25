using System.Collections.Generic;
using UnityEngine;

public class CameraDrawing : MonoBehaviour {

	private static Dictionary<uint, Line2D> lines = new Dictionary<uint, Line2D>();

	private Material mat;

	private void Start () {
		Shader shader = Shader.Find("Hidden/Internal-Colored");
		mat = new Material(shader);
	}

	private void OnPostRender () {
		// Initializing
		GL.PushMatrix();
		GL.LoadOrtho();
		mat.SetPass(0);

		// Draw lines
		GL.Begin(GL.LINES);
		foreach (Line2D line in lines.Values) {
			GL.Color(line.color);
			float angle = Mathf.Atan(line.Steapness);
			for (ushort i = 0; i < line.thickness; i++) {
				Vector2 p1 = new Vector2((line.start.x - i * Mathf.Sin(angle)) / Screen.width, (line.start.y + i * Mathf.Cos(angle)) / Screen.height);
				Vector2 p2 = new Vector2((line.end.x - i * Mathf.Sin(angle)) / Screen.width, (line.end.y + i * Mathf.Cos(angle)) / Screen.height);
				GL.Vertex(p1);
				GL.Vertex(p2);
			}
		}
		GL.End();

		//Finishing
		GL.PopMatrix();
	}

	public static uint AddLine (Line2D line) {
		uint index = 0u;
		while (lines.ContainsKey(index)) index++;
		lines.Add(index, line);
		return index;
	}

	public static void UpdateLine (uint index, Line2D line) {
		lines [index] = line;
	}

	public static void DeleteLine (uint index) {
		lines.Remove(index);
	}
}

public struct Line2D
{
	public Vector2 start;
	public Vector2 end;
	public ushort thickness;
	public Color color;

	public float Steapness {
		get {
			if (start.x == end.x) return float.PositiveInfinity;
			return (end.y - start.y) / (end.x - start.x);
		}
	}

	public Line2D (Vector2 pstart, Vector2 pend) {
		start = pstart;
		end = pend;
		thickness = 5;
		color = Color.white;
	}

	public Line2D (Vector2 pstart, Vector2 pend, ushort pthickness, Color pcolor) {
		start = pstart;
		end = pend;
		thickness = pthickness;
		color = pcolor;
	}
}
