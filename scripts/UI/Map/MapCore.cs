using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapCore : MonoBehaviour
{
	public List<Selector> selector_list = new List<Selector>();

	public ReferenceSystem CurrentSystem { get; set; }

	private MapDrawer map_drawer;
	public SelectionViewer selection_viewer;

	public static Color friendly_color = new Color(.15f, .1f, .62f);
	public static Color ennemy_color = new Color(.65f, .1f, .12f);
	public static Color friendly_color_selected = new Color(.45f, .4f, .92f);
	public static Color ennemy_color_selected = new Color(.95f, .4f, .42f);

	public SelectorEventSystem selector_event_system = new SelectorEventSystem();

	public List<IMarkerParentObject> selection {
		get { return selection_viewer.selected; }
		set {
			List<IMarkerParentObject> rem_list = selection_viewer.selected.FindAll(x => !value.Contains(x));
			foreach (IMarkerParentObject item in rem_list) {
				selection_viewer.Remove(item);
			}
			foreach (IMarkerParentObject obj in value) {
				if (!selection_viewer.selected.Contains(obj)) {
					selection_viewer.Add(obj);
				}
			}
		}
	}

	private Selector group_selector = null;

	public static MapCore Active {
		get { return SceneGlobals.map_core; }
		set { SceneGlobals.map_core = value; }
	}

	public void Start_() {
		map_drawer = SceneGlobals.map_drawer;
		SceneGlobals.map_core = this;
		CurrentSystem = ReferenceSystem.default_system;
		
		Vector3 pivot_point = Vector3.zero;

		map_drawer.AddSpriteGroup(new Polygon(4, Vector3.zero, Vector3.up, Vector3.right * 130), map_drawer.sprites, new Vector2Int(10, 10), 1);
	}

	private void Update () {
		if (!SceneGlobals.general.InMap || SceneGlobals.in_console) return;
		CurrentSystem.Update();

		// Rotates ship at clicked point
		if (Input.GetMouseButtonDown(0) && Input.GetKey(KeyCode.LeftShift)) {
			Ray ray = SceneGlobals.map_camera.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;
			Vector3 dir = ray.direction;
			if (Physics.Raycast(ray, out hit)) {
				dir = (hit.point - transform.position).normalized;
			} else {
				if (Vector3.Dot(ray.direction, SceneGlobals.map_camera.transform.forward) < 0) {
					dir *= -1;
				}
			}
			SceneGlobals.Player.low_ai.Point2(dir);
		}
	}

	/// <summary> Should be called every frame </summary>
	/// <param name="pivot_point"> The pivot point, around which the camera turns </param>
	/// <param name="camera_transform"> The transform of the camera </param>
	public void UpdateDraw (Vector3 pivot_point, Transform camera_transform) {
		float mult = Mathf.Min(Mathf.Max(40f, (pivot_point - camera_transform.position).magnitude) / 200f, 40f);
		float turr_mult = mult * .5f;
		map_drawer.Draw(new Polygon(100 * mult, 70, pivot_point, Vector3.up), mult);
		map_drawer.Draw(new Line(pivot_point - 120 * mult * Vector3.back, pivot_point + 120 * mult * Vector3.back), mult);
		map_drawer.Draw(new Line(pivot_point - 120 * mult * Vector3.left, pivot_point + 120 * mult * Vector3.left), mult);
		map_drawer.Draw(new Polygon(80 * mult, 50, pivot_point, Vector3.up), mult);
		map_drawer.Draw(new Line(pivot_point + 50 * mult * Vector3.up, pivot_point - 50 * mult * Vector3.up), mult);
		map_drawer.sprite_groups [1].ChangeShape(new Polygon(4, pivot_point, Vector3.up, pivot_point + Vector3.right * 130 * mult));

		var player = SceneGlobals.Player;		
		foreach (TurretGroup tg in player.TurretGroups) {
			if (tg.Count > 0) {
				Vector3 tgtpos =  tg.GetTgtDir(tg[0]);
				if (tgtpos.sqrMagnitude > 1e6f) {
					tgtpos = tgtpos.normalized * 1000;
				}

				// Turret aiming lines
				foreach (Turret turr in tg.TurretArray) {
					Line connection = new Line(turr.MidPos, turr.MidPos + tg.GetTgtDir(turr));
					map_drawer.Draw(connection, new Color(.85f, .8f, .25f), turr_mult);
				}
			}
		}
	}

	/// <summary> Adds a new selector (like a constructor) </summary>
	/// <param name="position"> The position of the selector </param>
	/// <param name="anchorposition"> The position, where the selector is anchored </param>
	/// <param name="line_color"> The color of the line conn </param>
	/// <param name="sub_selections"> The selection possibilities of the categories </param>
	/// <returns> The selector </returns>
	private Selector AddNewSelector (Vector2 position, Vector2 anchorposition, Color line_color, ushort[] sub_selections) {
		GameObject selectorobj = Instantiate(GameObject.Find("map_selector"));
		selectorobj.transform.SetParent(SceneGlobals.map_canvas.transform);
		selectorobj.transform.position = position;

		Selector selector = Loader.EnsureComponent<Selector>(selectorobj);
		selector.line_color = line_color;
		selector.AnchorPoint = anchorposition;

		selector.sub_options = sub_selections;
		selector.Init();
		return selector;
	}

	/// <summary> Adds a new selector-like (like a constructor) </summary>
	/// <param name="options"> The options in chronological order, as text </param>
	/// <param name="position"> The position of the selector </param>
	/// <param name="anchorposition"> The position, where the selector is anchored </param>
	/// <param name="line_color"> The color of the line conn </param>
	/// <returns> The selector </returns>
	private SelectorLike AddNewSelectorLike (string[] options, Vector2 position) {
		GameObject selectorobj = Instantiate(GameObject.Find("map_selector_simple"));
		selectorobj.transform.SetParent(SceneGlobals.map_canvas.transform);
		selectorobj.transform.position = position;

		SelectorLike selector = Loader.EnsureComponent<SelectorLike>(selectorobj);
		selector.options_str = options;

		selector.Init();
		return selector;
	}

	/// <summary>
	///		Adds a selector for a sceneobject, if none is present
	/// </summary>
	/// <param name="obj"> The concerned sceneobject </param>
	/// <param name="position"> The position of the selector in 2D-space on teh screen </param>
	/// <param name="line_color"> The color of the line conn </param>
	/// <returns> The selector behaviour </returns>
	public Selector AddSelectorForSceneObject (IMarkerParentObject obj, Vector2 position, Vector2 anchorposition, Color line_color) {
		IMarkerParentObject[] obj_arr = new IMarkerParentObject [] { obj };

		foreach (Selector sel in selector_list) {
			if (sel.targets == obj_arr) return sel;
		}

		ushort [] sub_options = new ushort[] { 0xf, 0, 0, 0 };

		if (obj is ITargetable) {
			sub_options[1] = 0x7d;
		}
		if (obj is Ship | (obj is Target && !(obj as Target).virt_ship)) {
			sub_options [2] = 0x3;
			if (obj.Friendly) sub_options [3] += 0x7f;
			else sub_options [1] += 0x2;
		}

		Selector selector = AddNewSelector(position, anchorposition, line_color, sub_options);
		selector.targets = obj_arr;

		selector_list.Add(selector);
		return selector;
	}

	/// <summary>
	///		Adds a selector for a sceneobject, if none is present
	/// </summary>
	/// <param name="obj"> The concerned sceneobject </param>
	/// <param name="position"> The position of the selector in 2D-space on teh screen </param>
	/// <param name="line_color"> The color of the line conn </param>
	/// <returns> The selector behaviour </returns>
	private Selector AddSelectorForSceneObjects (IMarkerParentObject [] objs, Vector2 position, Vector2 anchorposition, Color line_color) {
		ushort[] sub_options = new ushort[] { 0, 0, 0, 0x7f };

		foreach (IMarkerParentObject obj in objs) {
			Target tgt = obj as Target;
			if ((!(obj is Ship) && tgt != null && !(tgt.virt_ship)) | !obj.Friendly) {
				sub_options [3] = 0;
			}
		}

		Selector selector = AddNewSelector(position, anchorposition, line_color, sub_options);
		selector.targets = objs;

		selector_list.Add(selector);
		return selector;
	}

	private void AddSelectorFromMarker (MapTgtMarker marker) {
		if (!selector_list.TrueForAll(x => x.marker != marker)) return;
		IMarkerParentObject sc_obj = marker.LinkedObject;
		Vector2 position = (Vector2) marker.RectTransform.position + new Vector2(100, 100);
		Vector2 anchorposition = marker.RectTransform.position + new Vector3(marker.RectTransform.sizeDelta.x / 2f, 0);
		Selector sel = AddSelectorForSceneObject(sc_obj, position, anchorposition, marker.GetComponent<Image>().color);
		sel.marker = marker;
	}

	#region public functions
	// +-------------------------+
	// |						 |
	// |	PUBLIC FUNCTIONS	 |
	// |						 |
	// +-------------------------+


	/// <summary> Should be called, if a SceneObject gets initiated </summary>
	/// <param name="obj"> The SceneObject, that gets initiated </param>
	public void ObjectSpawned (IMarkerParentObject obj) {
		GameObject map_pointer = Instantiate(GameObject.Find("map_pointer_template"));
		map_pointer.name = string.Format("Indicator ({0})", obj.Name);
		var img = map_pointer.GetComponent<Image>();
		map_pointer.transform.SetParent(SceneGlobals.map_canvas.transform);

		MapTgtMarker map_image = map_pointer.GetComponent<MapTgtMarker>();
		map_image.img = img;
		map_image.LinkedObject = obj;
		obj.Marker = map_image;
	}

	public void SelectorRemoved (Selector concerned) {
		selector_list.Remove(concerned);
	}

	/// <summary> Should be called, if an indicator is clicked </summary>
	/// <param name="own"> The indicator </param>
	public void TgtMarkerClickedR (MapTgtMarker own) {
		AddSelectorFromMarker(own);
	}

	public void TgtMarkerClickedL (MapTgtMarker own) {
		IMarkerParentObject sc_obj = own.LinkedObject;
		if (selection.Contains(sc_obj))
			selection_viewer.Remove(sc_obj);
		else
			selection_viewer.Add(sc_obj);
	}

	public void CommandToSelected () {
		if (group_selector != null)
			SelectorRemoved(group_selector);

		IMarkerParentObject[] sel = System.Array.FindAll(selection.ToArray(), x => x.Friendly);
		if (sel.Length > 0) {
			group_selector = AddSelectorForSceneObjects(sel, new Vector2(200, 300), new Vector2(160, 185), friendly_color_selected);
			group_selector.SpawnSubSelector(SelectionClass.Command);
		}
	}

	#endregion
}

/// <summary> Everything on the UI is mesured comared to this </summary>
public class ReferenceSystem
{
	private Vector3 _position;
	public Vector3 Position {
		get { return _position + Offset; }
		set {
			Offset = Vector3.zero;
			_position = value;
		}
	}

	private Vector3 _offset = Vector3.zero;
	public Vector3 Offset {
		get { return _offset; }
		set { _offset = value; }
	}

	public SceneObject ref_obj = null;

	public bool HasParent {
		get { return ref_obj != null; }
	}

	public Vector3 Velocity {
		get {
			if (ref_obj == null) return Vector3.zero;
			return ref_obj.Velocity;
		}
	}

	public static readonly ReferenceSystem default_system = new ReferenceSystem(Vector3.zero);

	/// <param name="parent_obj"> The Object, the system is attached to </param>
	public ReferenceSystem (SceneObject parent_obj) {
		ref_obj = parent_obj;
	}

	/// <param name="p_position"> The initial position of the system </param>
	public ReferenceSystem (Vector3 p_position) {
		Position = p_position;
	}

	public void Update () {
		if (ref_obj != null) {
			if (ref_obj.Exists) {
				_position = ref_obj.Position;
			} else {
				ref_obj = Target.None;
				_position = ref_obj.Position;
			}
		}
	}

	/// <summary> Returns the relative velocity </summary>
	/// <param name="vel"> Velocity in world space </param>
	public Vector3 RelativeVelocity (Vector3 vel) {
		return vel - Velocity;
	}

	/// <summary> Returns the relative position </summary>
	/// <param name="vel"> Position in world space </param>
	public Vector3 RelativePosition (Vector3 pos) {
		return pos - Position;
	}
}