using UnityEngine;

public class Selector : SelectorLike
{
	private const ushort thickness = 2;

	public Material line_material;

	public MapTgtMarker marker = null;

	private uint lineindex;

	public Color line_color = Color.white;

	private bool is_from_selection;

	public ushort[] sub_options = new ushort[4];

	public SelectorParent Parent {
		get {
			if (targets != null) return SelectorParent.target;
			return SelectorParent.selector;
		}
	}

	public Vector2 AnchorPoint { get; set; }

	private Line2D ConnectionLine {
		get{ return new Line2D(AnchorPoint, UpperLeft + new Vector2(5, -5), thickness, line_color); }
	}

	public void Init () {
		if (targets == null) targets = new IMarkerParentObject[0];

		// Figures out, which things get shown
		int strng_num = 0;
		for (int i = 0; i < 4; i++) {
			if (sub_options [i] != 0)
				strng_num++;
		}

		options_str = new string [strng_num];
		options_icons = new Sprite [strng_num];
		options_flags = new int [strng_num];
		option_functions = new int [strng_num];
		int indx2 = 0;
		for (int i=0; i < 4; i++) {
			if (sub_options [i] != 0) {
				options_str [indx2] = Globals.selector_data.main_options [i];
				options_icons [indx2] = Globals.selector_data.main_icon[i];
				options_flags [indx2] = Globals.selector_data.main_flags [i];
				indx2++;
			}
		}

		base.Init();

		lineindex = CameraDrawing.AddLine(ConnectionLine);
	}

	private new void Update () {
		base.Update();

		if (dragging) {
			CameraDrawing.UpdateLine(lineindex, ConnectionLine);
		}

		// Searches the nearest corner
		if (marker != null) {
			Vector2 act_anchorpos = marker.Corners [0];
			float act_distance_sqr = (marker.Corners [0] - Position).sqrMagnitude;
			for (int i=1; i < 4; i++) {
				float new_distance_sqr = (marker.Corners [i] - Position).sqrMagnitude;
				if (new_distance_sqr < act_distance_sqr) {
					act_anchorpos = marker.Corners [i];
					act_distance_sqr = new_distance_sqr;
				}
			}
			if (AnchorPoint != act_anchorpos) {
				AnchorPoint = act_anchorpos;
				CameraDrawing.UpdateLine(lineindex, ConnectionLine);
			}
		}
		if (currentstate == SelectorState.opening | currentstate == SelectorState.closing) {
			CameraDrawing.UpdateLine(lineindex, ConnectionLine);
		}
	}

	public void SpawnSubSelector (SelectionClass pclass) {
		SelectorOptions options = new SelectorOptions(sub_options [(int) pclass], pclass);
		if (child != null) {
			try {
				Destroy(child.gameObject);
			} catch { }
		}
		sbyte pos = (sbyte) options.selection_class;
		GameObject selectorobj = Instantiate(GameObject.Find("map_subselector"));
		selectorobj.transform.SetParent(SceneGlobals.map_canvas.transform);

		SubSelector new_child = Loader.EnsureComponent<SubSelector>(selectorobj);
		new_child.targets = targets;
		new_child.options = options;
		new_child.InitSub();
		new_child.targets = targets;
		
		new_child.Position = UpperLeft + new Vector2(width + new_child.width / 2, - new_child.head_height);
		new_child.draggable = false;
		new_child.transform.SetParent(transform, true);
		child = new_child;
	}

	/// <summary> Should trigger, when the exit button is pressed </summary>
	public new void ExitButtonPressed () {
		if (is_from_selection) {
			MapCore.Active.selection_viewer.Remove(base.targets [0]);
		} else {
			MapCore.Active.SelectorRemoved(this);
		}
		CameraDrawing.DeleteLine(lineindex);
		base.ExitButtonPressed();
	}
}