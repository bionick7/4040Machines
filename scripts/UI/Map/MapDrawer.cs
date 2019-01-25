using System.Collections.Generic;
using UnityEngine;

public class MapDrawer : MonoBehaviour {

	public List<DrawAssignemt> assignements = new List<DrawAssignemt>();
	public List<LineRenderer> renderers = new List<LineRenderer>();
	public Dictionary<byte, SpriteGroup> sprite_groups = new Dictionary<byte, SpriteGroup>();
	public List<IDrawable> drawables = new List<IDrawable>();

	private List<IDrawable> remove_graphics = new List<IDrawable>();

	public Camera camera;

	public Material material;
	public Sprite [] sprites;

	public static Color std_color = new Color(.84f, .866f, .73f, .76f);

	private bool _shown;
	public bool Shown {
		get { return _shown; }
		set {
			foreach (LineRenderer shape in renderers) {
				shape.enabled = value;
			}
			_shown = value;
		}
	}

	private void Start () {
		transform.position = Vector3.zero;
	}

	private void LateUpdate () {
		//Shown = SceneGlobals.general.InMap;

		if (Shown) {
			foreach (SpriteGroup group in sprite_groups.Values) {
				group.Update();
			}
			foreach (IDrawable drawable in drawables) {
				drawable.Draw(camera);
			}
		}

		for (int i=0; i < assignements.Count || i < renderers.Count; i++) {
			// Draws everything
			if (i >= assignements.Count) {
				Destroy(renderers [i].gameObject);
				renderers.Remove(renderers [i]);
			} else {
				if (i >= renderers.Count) {
					AddRenderer();
				}
				RendererDraw(renderers [i], assignements [i]);
			}
		}
		assignements.Clear();

		// Delete the things to delete
		for (int i=0; i < remove_graphics.Count; i++) {
			IDrawable concerned_graphic = remove_graphics[i];
			drawables.Remove(concerned_graphic);
			for (int j = 0; j < concerned_graphic.GraphicsComponents.Length; j++) {
				Destroy(concerned_graphic.GraphicsComponents [0]);
			}
		}
		remove_graphics.Clear();
	}

	public void Draw (IShape shape, float size=1) {
		Draw(shape, std_color, size);
	}

	public void Draw (IShape shape, Color color, float size=1) {
		assignements.Add(new DrawAssignemt(shape, size, color));
	}

	public LineRenderer AddRenderer (string name="line") {
		GameObject child = new GameObject(name, new System.Type[1] { typeof(LineRenderer) } );
		child.transform.SetParent(transform);
		LineRenderer renderer = child.GetComponent<LineRenderer>();
		renderer.materials = new Material [1] { material };
		renderer.startWidth = renderer.endWidth = .5f;
		renderers.Add(renderer);
		return renderer;
	}

	private void RendererDraw (LineRenderer renderer, DrawAssignemt assignement) {
		renderer.positionCount = assignement.shape.PointsCount;
		renderer.SetPositions(assignement.shape.Points);
		renderer.startWidth = renderer.endWidth = assignement.thickness;
		renderer.startColor = renderer.endColor = assignement.color;
	}

	public void AddSpriteGroup (IShape shape, Sprite[] images, Vector2Int size_, byte id_ = 0) {
		byte id = id_ == 0 || id_ < sprite_groups.Count ? (byte) sprite_groups.Count : id_;
		SpriteGroup group = new SpriteGroup(images, shape) { size = size_ };
		sprite_groups.Add(id, group);
	}

	public void RemoveSingleSprite (IDrawable base_object) {
		remove_graphics.Add(base_object);
	}
}

public struct DrawAssignemt
{
	public IShape shape;
	public float thickness;
	public Color color;

	public DrawAssignemt(IShape p_shape, float p_thickness, Color p_color) {
		shape = p_shape;
		thickness = p_thickness;
		color = p_color;
	}

	public static bool operator == (DrawAssignemt lhs, DrawAssignemt rhs) {
		return lhs.shape == rhs.shape && lhs.thickness == rhs.thickness && lhs.color == rhs.color;
	}

	public static bool operator != (DrawAssignemt lhs, DrawAssignemt rhs) {
		return lhs.shape != rhs.shape || lhs.thickness != rhs.thickness || lhs.color != rhs.color;
	}
}