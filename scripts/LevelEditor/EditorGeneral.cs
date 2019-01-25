using System.Collections.Generic;
using UnityEngine;

/* =======================================================
 * Manages the whole LevelEditor and stores key variables.
 * Heartpiece of the level editor
 * ======================================================= */

public class EditorGeneral : MonoBehaviour
{
	/// <summary> All the movables </summary>
	private HashSet<Movable> movables = new HashSet<Movable>();

	public static Arrows arrows;
	public static Camera maincam;
	public static EditorGeneral active;
	public static Inspector inspector;

	public static EDShip Player;
	public static Canvas mainV;
	private static Canvas storyV;

	private static InspectorType ins_type;
	/// <summary>
	///		Which kind of specific inspector (bottom-right) is used?
	/// </summary>
	public static InspectorType InspectorType {
		get { return ins_type; }
		set {
			switch (value) {
			default:
			case InspectorType.ship:
				ShipInspector.active.Shown = true;
				TargetInspector.active.Shown = false;
				SquadInspector.active.Shown = false;
				break;
			case InspectorType.target:
				ShipInspector.active.Shown = false;
				TargetInspector.active.Shown = true;
				SquadInspector.active.Shown = false;
				break;
			case InspectorType.squad:
				ShipInspector.active.Shown = false;
				TargetInspector.active.Shown = false;
				SquadInspector.active.Shown = true;
				break;
			}
			ins_type = value;
		}
	}

	private static bool _storyview;
	/// <summary>
	///		True, if the storymode is in view;
	///		False, if not
	/// </summary>
	public static bool StoryView {
		get { return _storyview; }
		set {
			storyV.enabled = value;
			_storyview = value;
		}
	}

	public static List<Squadron> squadron_list = new List<Squadron>() { Squadron.default_squadron };
	public static List<EDTarget> target_list = new List<EDTarget>();

	public static Movable current_movable;

	private void Awake () {
		arrows = GameObject.FindGameObjectWithTag("Arrows").GetComponent<Arrows>();
		maincam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
		inspector = GameObject.Find("inspector").GetComponent<Inspector>();
		active = this;
		current_movable = null;
		mainV = GameObject.Find("Canvas").GetComponent<Canvas>();
		storyV = GameObject.Find("StoryCanvas").GetComponent<Canvas>();
		StoryView = false;
	}

	/// <summary> Adds a "movable" object </summary>
	/// <param name="item"> The object in question </param>
	public void AddMovable (Movable item) {
		movables.Add(item);
	}

	/// <summary> Removes a "movable" object </summary>
	/// <param name="item"> The object in question </param>
	public void RemoveMovables (Movable item) {
		movables.Remove(item);
	}

	public void Reload () {
		ShipInspector.active.ReloadSquads();
	}

	/// <summary> Clears a scene, deleting everything in it </summary>
	public void Clear () {
		arrows.parent = null;
		foreach (Movable movable in movables) {
			if (movable.correspondence is EDShip || movable.correspondence is EDTarget) {
				Destroy(movable.gameObject, Time.deltaTime);
				Destroy(movable.circ.gameObject);
			}
		}
		movables.RemoveWhere(x => x.correspondence is EDShip || x.correspondence is EDTarget);
		squadron_list.Clear();
		target_list.Clear();
		current_movable = null;
	}

	/// <summary> Throws an error </summary>
	/// <param name="message"> The error in question </param>
	public static void Throw (string message) {
		DeveloppmentTools.Log(message);
	}

	private void Update () {
		arrows.Update_();
		if (Input.GetMouseButtonDown(0) & !arrows.clicked) {
			RaycastHit hit = new RaycastHit();
			if (Physics.Raycast(maincam.ScreenPointToRay(Input.mousePosition), out hit)) {
				foreach (Movable item in movables) {
					item.CheckClicked(hit);
				}
			}
		}
	}
}

/// <summary>
///		Represent the 3 possibilities for the inspector window
/// </summary>
public enum InspectorType
{
	ship,
	target,
	squad
}
