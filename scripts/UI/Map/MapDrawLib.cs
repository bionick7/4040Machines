using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


/// <summary> Defines a shape </summary>
public interface IShape
{
	/// <summary> Returns the points in 3D-Space </summary>
	Vector3[] Points { get; }
	int PointsCount { get; }
}

/// <summary> Draws a regular polygon </summary>
public struct Polygon : IShape
{
	public float radius;
	public ushort sides;
	public Vector3 center;
	public Vector3 normal;
	public Vector3 firstpoint;
	private bool has_firstpoint;

	public Vector3[] Points {
		get { return CalculatePoints(); }
	}

	public int PointsCount {
		get { return sides + 1; }
	}
	
	/// <param name="sides"> The number of sides (at least 3) </param>
	/// <param name="center"> The center of the polygon </param>
	/// <param name="normal"> The normal vector to the plane the polygon lies on </param>
	/// <param name="first_point"> The first point of the polygon </param>
	public Polygon(int sides_, Vector3 center_, Vector3 normal_, Vector3 first_point) {
		radius = (first_point - center_).magnitude;
		sides = (ushort) sides_;
		center = center_;
		normal = normal_;
		firstpoint = first_point;
		has_firstpoint = true;
	}

	/// <param name="radius"> The radius from the center </param>
	/// <param name="sides"> The number of sides (at least 3) </param>
	/// <param name="center"> The center of the polygon </param>
	/// <param name="normal"> The normal vector to the plane the polygon lies on </param>
	public Polygon(float radius_, int sides_, Vector3 center_, Vector3 normal_) {
		radius = radius_;
		sides = (ushort) sides_;
		center = center_;
		normal = normal_;
		firstpoint = Vector3.zero;
		has_firstpoint = false;
	}

	/// <returns> The points in 3D Space </returns>
	public Vector3[] CalculatePoints() {
		if (sides < 3) {
			throw new System.ArgumentException("'sides' argument must be higher or equal than 3");
		}

		Vector3 perp;

		if (has_firstpoint) {
			perp = firstpoint;
		} else {
			if (normal == Vector3.zero) throw new System.ArgumentException("(0, 0, 0) not permitted as an axis");
			else if (normal.x != 0) perp = center + new Vector3(-(normal.y + normal.z) / normal.x, 1, 1).normalized * radius;
			else if (normal.y != 0) perp = center + new Vector3(1, -(normal.x + normal.z) / normal.y, 1).normalized * radius;
			else perp = center + new Vector3(1, 1, -(normal.x + normal.y) / normal.z).normalized * radius;
		}
		Vector3[] points = new Vector3[sides + 1];
		//points [0] = points[sides] = perp;
		//Quaternion rot_step = Quaternion.AngleAxis(360 / sides, normal);
		Quaternion tilt = Quaternion.FromToRotation(Vector3.up, normal);

		for (int i=0; i <= sides; i++) {
			points [i] = tilt * (radius * new Vector3(Mathf.Sin(2 * Mathf.PI * i / sides), 0, Mathf.Cos(2 * Mathf.PI * i / sides))) + center;
		}
		points [sides] = points [0];

		return points;
	}

	public override string ToString () {
		return string.Format("<regular {0}-sided Polygon with radius {1}>", PointsCount, radius);
	}
}

/// <summary> A simple line from 1 point to another </summary>
public struct Line : IShape
{
	public Vector3 start;
	public Vector3 end;

	public Vector3[] Points {
		get {
			return new Vector3 [2] { start, end };
		}
	}

	public int PointsCount {
		get {
			return 2;
		}
	}

	public Line (Vector3 start_, Vector3 end_) {
		start = start_;
		end = end_;
	}

	public override string ToString () {
		return string.Format("<Line {0} - {1}>", start, end);
	}
}

public class SpriteGroup
{
	public List<Image> images = new List<Image>();
	public Vector3[] positions;

	public Vector2Int size = new Vector2Int(10, 10);
	public Quaternion orientation;

	public Sprite[] sprites;

	private bool _shown;
	public bool Shown {
		get { return _shown; }
		set {
			foreach (Image img in images) {
				img.enabled = value;
			}
			_shown = value;
		}
	}

	public SpriteGroup (Sprite[] images_, IShape shape) {
		sprites = images_;
		ChangeShape(shape);
	}

	public void ChangeShape(IShape shape) {
		Vector3[] points = shape.Points;
		positions = new Vector3 [shape.PointsCount];
		for (int i=0; i < shape.PointsCount; i++) {
			Image img;
			if (i < images.Count) {
				img = images [i];
			} else {
				GameObject obj = new GameObject("symbol_renderer", new System.Type[1] { typeof(Image) });
				obj.GetComponent<RectTransform>().sizeDelta = new Vector3(size.x, size.y);
				obj.transform.position = points [i];
				obj.transform.SetAsFirstSibling();
				obj.transform.SetParent(SceneGlobals.map_canvas.transform);
				img = obj.GetComponent<Image>();
			}
			img.sprite = sprites [i % sprites.Length];
			img.color = MapDrawer.std_color;

			images.Add(img);
			positions [i] = points [i];
		}
	}

	public void Update () {
		for (int i=0; i < positions.Length; i++) {
			Camera cam = SceneGlobals.map_camera;
			Vector3 position_2d = Vector3.Dot(cam.transform.forward, positions[i] - cam.transform.position) > 1 ? cam.WorldToScreenPoint(positions[i]) : new Vector3(-200, -200);
			position_2d.z = 0;
			images[i].transform.position = position_2d;
		}
	}
}

public interface IDrawable
{
	Vector2 Position { get; set; }
	Behaviour[] GraphicsComponents { get; }

	void Draw (Camera camera);
}

public struct SimpleDrawnObject : IDrawable
{
	public Image img;

	public Vector2 Position {
		get { return img.rectTransform.position; }
		set { img.rectTransform.position = (Vector3) value; }
	}

	public byte Clicked {
		// Proivisory
		get { return 0; }
	}

	public Behaviour[] GraphicsComponents {
		get { return new Behaviour [1] { (Behaviour) img }; }
	}

	public SimpleDrawnObject(Image p_img) {
		img = p_img;
	}

	public void Draw (Camera camera) {

	}
}