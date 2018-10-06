using System.Collections.Generic;
using UnityEngine;

public class MapDrawer : MonoBehaviour {

	public Dictionary<byte, LineRenderer> renderers = new Dictionary<byte, LineRenderer>();
	public Dictionary<byte, IShape> shapes = new Dictionary<byte, IShape>();
	public Dictionary<byte, SpriteGroup> sprite_groups = new Dictionary<byte, SpriteGroup>();
	public List<IDrawable> drawables = new List<IDrawable>();

	public Material material;
	public Sprite [] sprites;

	private bool _shown;
	public bool Shown {
		get {
			return _shown;
		}
		set {
			foreach (LineRenderer shape in renderers.Values) {
				shape.enabled = value;
			}
			_shown = value;
		}
	}

	private void Start () {
		transform.position = Vector3.zero;
		renderers.Add(0, GetComponent<LineRenderer>());
	}

	private void Update () {
		Shown = SceneData.general.InMap;
		Draw(1);

		if (Shown) {
			foreach (SpriteGroup group in sprite_groups.Values) {
				group.Update();
			}
			foreach (IDrawable drawable in drawables) {
				drawable.Draw(SceneData.map_camera);
			}
		}
	}

	public void Draw (byte id, float size_mult=1) {
		IShape shape = shapes[id];

		LineRenderer renderer = renderers [id];

		renderer.startWidth = renderer.endWidth = size_mult;

		renderer.positionCount = shape.PointsCount;
		renderer.SetPositions(shape.Points);
	}

	public void AddRenderer (byte id, string name="line") {
		GameObject child = new GameObject(name, new System.Type[1] { typeof(LineRenderer) } );
		child.transform.SetParent(transform);
		LineRenderer drawer = child.GetComponent<LineRenderer>();
		drawer.materials = new Material [1] { material };
		drawer.startWidth = drawer.endWidth = .5f;
		renderers.Add(id, drawer);
	}

	public void AddShape(IShape shape, byte id_ = 0) {
		byte id = id_ == 0 || id_ < renderers.Count ? (byte) renderers.Count : id_;
		AddRenderer(id, shape is Polygon ? "polygon" : "line");
		LineRenderer drawer = renderers[id];
		drawer.loop = false;

		shapes.Add(id, shape);

		Draw(id);
	}

	public void AddSpriteGroup(IShape shape, Sprite[] images, Vector2Int size_, byte id_ = 0) {
		byte id = id_ == 0 || id_ < sprite_groups.Count ? (byte) sprite_groups.Count : id_;
		SpriteGroup group = new SpriteGroup(images, shape) { size = size_ };
		sprite_groups.Add(id, group);
		// Debug.Log(id);
	}

	public MapDrawnObject AddSingleSprite(GameObject drawn_object, SceneObject base_object) {
		MapDrawnObject res = new MapDrawnObject(drawn_object, base_object);
		drawables.Add(res);
		return res;
	}

	public static Vector3[] DrawLine(Vector3 start, Vector3 end) {
		return new Vector3 [2] { start, end };
	}
}
