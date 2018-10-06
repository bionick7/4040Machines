using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


/// <summary> Defines a shape </summary>
public interface IShape
{
	/// <summary> Returns the points in 3D space </summary>
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
		get {
			return CalculatePoints();
		}
	}

	public int PointsCount {
		get {
			return sides + 1;
		}
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
		points [0] = points[sides] = perp;
		Quaternion rot_step = Quaternion.AngleAxis(-360f / sides, normal);
		for (int i=1; i < sides; i++) {
			points [i] = center + rot_step * (points[i - 1] - center);
		}

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
		get {
			return _shown;
		}
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
				obj.transform.SetParent(SceneData.map_canvas.transform);
				img = obj.GetComponent<Image>();
			}
			img.sprite = sprites [i % sprites.Length];

			images.Add(img);
			positions [i] = points [i];
		}
	}

	public void Update () {
		for (int i=0; i < positions.Length; i++) {
			Camera cam = SceneData.map_camera;
			Vector3 position_2d = Vector3.Angle(cam.transform.forward, positions[i] - cam.transform.position) < 90 ? cam.WorldToScreenPoint(positions[i]) : new Vector3(-200, -200);
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

public class MapDrawnObject : IDrawable
{
	/// <summary> The object drawn on the screen </summary>
	public GameObject obj;

	private RectTransform rect_trans;
	private Vector2 original_size;

	private SceneObject _linked_object;
	/// <summary> The object, the drawn object is referring to </summary>
	public SceneObject LinkedObject {
		get {
			if (!HasLinkedObject) throw new System.NullReferenceException("There is no object linked");
			return _linked_object;
		}
		set { _linked_object = value; }
	}

	/// <summary> The Image or button of the drawn object, throws exception, if none is present </summary>
	public Behaviour[] GraphicsComponents {
		get {
			if (!HasLinkedObject) throw new System.NullReferenceException("There is no object linked");
			Behaviour[] imgs = System.Array.ConvertAll<Image, Behaviour>(obj.GetComponentsInChildren<Image>(), x => (Behaviour) x);
			Behaviour[] butts = System.Array.ConvertAll<Button, Behaviour>(obj.GetComponentsInChildren<Button>(), x => (Behaviour) x);
			Behaviour[] res = new Behaviour[imgs.Length + butts.Length];
			System.Array.Copy(imgs, res, imgs.Length);
			System.Array.Copy(butts, res, butts.Length);
			return res;
			throw new System.Exception(string.Format("Object {0} has neither an Image or a Button", obj.name));
		}
	}

	/// <summary> True, LinkedObject exists, else false </summary>
	public bool HasLinkedObject {
		get { return _linked_object != null && _linked_object.Exists; }
	}

	private bool _enabled;
	/// <summary> True if shown, else false </summary>
	public bool Enabled {
		get { return _enabled; }
		set {
			foreach (Behaviour behaviour in GraphicsComponents) {
				behaviour.enabled = value;
			}
			_enabled = value;
		}
	}

	/// <summary> True if the mous hovers over the Image </summary>
	public bool MouseOver {
		get { return Contains(new Vector2Int((int) Input.mousePosition.x, (int) Input.mousePosition.y)); }
	}

	/// <summary> The position of the drawn object </summary>
	public Vector2 Position {
		get { return (Vector2) obj.transform.position; }
		set { obj.transform.position = (Vector3) value; }
	}

	/// <param name="screen_object"> The object drawn on the screen, should have a Image or Button attached to it </param>
	/// <param name="linked"> The object in word space covered by the mapdrawnobject </param>
	public MapDrawnObject(GameObject screen_object, SceneObject linked, float size_multiplyer = 1) {
		obj = screen_object;
		if (obj.GetComponent<Image>() == null && obj.GetComponent<Button>() == null)
			throw new System.ArgumentNullException("screenobj must be Image or Button");
		rect_trans = screen_object.GetComponent<RectTransform>();
		if (rect_trans == null) throw new System.ArgumentException("screen_object must have a RectTransform");
		LinkedObject = linked;
		original_size = rect_trans.sizeDelta * size_multiplyer;
	}

	/// <param name="screen_object"> The object drawn on the screen, should have a Image or Button attached to it </param>
	public MapDrawnObject(GameObject screen_object, float size_multiplyer = 1) {
		obj = screen_object;
		if (obj.GetComponent<Image>() == null && obj.GetComponent<Button>() == null)
			throw new System.ArgumentNullException("screenobj must be Image or Button");
		RectTransform rect_trans = screen_object.GetComponent<RectTransform>();
		if (rect_trans == null) throw new System.ArgumentException("screen_object must have a RectTransform");
		original_size = rect_trans.sizeDelta * size_multiplyer;
	}

	/// <summary> Updates the position of the object </summary>
	/// <param name="camera"> The map camera supposed to render the linked object </param>
	public void Draw (Camera camera) {
		if (!HasLinkedObject) return;
		Transform camera_transform = camera.transform;

		// Position
		if (Vector3.Angle(camera_transform.forward, LinkedObject.Transform.position - camera_transform.position) >= camera.fieldOfView) {
			Enabled = false;
		} else {
			if (!Enabled) Enabled = true;
			Vector3 pos = camera.WorldToScreenPoint(LinkedObject.Transform.position);
			Position = new Vector2Int((int) pos.x, (int) pos.y);
		}

		// Size
		float dist = Vector3.Distance(camera_transform.position, LinkedObject.Transform.position);
		rect_trans.sizeDelta = original_size * (30 / dist + .1f);
	}

	/// <param name="point"></param>
	/// <returns> True, if the  </returns>
	public bool Contains (Vector2Int point) {
		return rect_trans.rect.Contains(point - Position);
	}

	public static explicit operator GameObject(MapDrawnObject map_obj) {
		return map_obj.obj;
	}
}

public struct SimpleDrawnObject : IDrawable
{
	public Image img;

	public Vector2 Position {
		get { return img.rectTransform.position; }
		set { img.rectTransform.position = (Vector3) value; }
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