using UnityEngine;

/* ===================================================================
 * The behaviour of the arrows, that dragg the movable objects around
 * =================================================================== */

public class Arrows : MonoBehaviour {

	public Vector3 translation_speed = new Vector3(20, 20, 20);

	private bool HasParent {
		get { return parent != null; }
	}
	/// <summary> The thing, that gets dragged around </summary>
	public Movable parent;
	private Camera cam;
	private MeshRenderer[] renderers;
	private EditorCameraMovement camera_movement;
	private Vector3 initial_scale;

	private Collider x_arrow;
	private Collider y_arrow;
	private Collider z_arrow;

	private Transform velocity_indi;
	private MeshRenderer velocity_renderer;
	private Transform angvel_indi;
	private MeshRenderer angvel_renderer;

	public bool clicked = false;

	private bool dragging = false;
	private Direction drag_direction;
	private Vector3 clickpos;

	private bool first_drag_step = true;

	private bool _shown;
	/// <summary> If the arrows are visible </summary>
	private bool Shown {
		get {
			return _shown;
		}
		set {
			foreach (MeshRenderer renderer in renderers) {
				renderer.enabled = value;
			}
			_shown = value;
		}
	}

	private void Start () {
		parent = GetComponentInParent<Movable>();
		cam = EditorGeneral.maincam;
		camera_movement = cam.GetComponent<EditorCameraMovement>();

		x_arrow = transform.GetChild(0).GetComponent<Collider>();
		y_arrow = transform.GetChild(1).GetComponent<Collider>();
		z_arrow = transform.GetChild(2).GetComponent<Collider>();
		velocity_indi = transform.GetChild(3);
		velocity_renderer = velocity_indi.GetComponent<MeshRenderer>();
		angvel_indi = transform.GetChild(4);
		angvel_renderer = angvel_indi.GetComponent<MeshRenderer>();

		initial_scale = transform.localScale;
		clickpos = Vector3.zero;
		renderers = GetComponentsInChildren<MeshRenderer>();
	}

	/// <summary> If a movable object is selected </summary>
	/// <param name="obj"> The movable object in question </param>
	public void Select (Movable obj) {
		transform.SetParent(obj.transform);
		transform.localPosition = Vector3.zero;
		parent = obj;
		EditorGeneral.current_movable = obj;
		EditorGeneral.inspector.UpdatePositionFileds();
		EditorGeneral.inspector.UpdateRotationFields();
		EditorGeneral.inspector.UpdateVelocities();
	}

	/// <summary> Translates the arrow with the object </summary>
	/// <param name="transition"> Translation vector </param>
	private void Translate(Vector3 transition) {
		if (HasParent) {
			parent.Position += transition;
		} else {
			transform.Translate(transition, Space.World);
		}
	}
	
	/// <summary> Update has to be called manually evry frame </summary>
	public void Update_ () {
		// Handles clicking
		if (Input.GetMouseButtonDown(0)) {
			Ray mouseray = cam.ScreenPointToRay(Input.mousePosition);
			RaycastHit[] hits = Physics.RaycastAll(mouseray);
			if (hits.Length > 0) {
				dragging = true;
				if (System.Array.Exists(hits, h => h.collider == x_arrow)) {
					drag_direction = Direction.x;
					clicked = true;
				} else if (System.Array.Exists(hits, h => h.collider == y_arrow)) {
					drag_direction = Direction.y;
					clicked = true;
				} else if (System.Array.Exists(hits, h => h.collider == z_arrow)) {
					drag_direction = Direction.z;
					clicked = true;
				} else {
					dragging = false;
				}
			}
		} else if (Input.GetMouseButtonUp(0)) {
			clicked = false;
			dragging = false;
			first_drag_step = true;
		}

		// Handles dragging
		if (dragging) {
			Vector3 dir;
			switch (drag_direction) {
			default:
			case Direction.x:
				dir = Vector3.left;
				break;
			case Direction.y:
				dir = Vector3.back;
				break;
			case Direction.z:
				dir = Vector3.up;
				break;
			}
			Vector3 new_click_pos = Vector3.Project(cam.ScreenPointToRay(Input.mousePosition).direction, dir);
			Vector3 diff = new_click_pos - clickpos;
			clickpos = new_click_pos;
			if (first_drag_step) first_drag_step = false;
			else Translate(diff * Vector3.Distance(cam.transform.position, transform.position));
			EditorGeneral.inspector.UpdatePositionFileds();
		}

		transform.rotation = Quaternion.identity;
		Vector3 new_scale = Vector3.Distance(transform.position, cam.transform.position) * initial_scale * .05f;

		// Handles parent moving and velocity visibility
		if (HasParent) {
			Shown = true;
			transform.localScale = new Vector3(
				new_scale.x / parent.transform.lossyScale.x,
				new_scale.y / parent.transform.lossyScale.y,
				new_scale.z / parent.transform.lossyScale.z);

			if (parent.Velocity == Vector3.zero) {
				velocity_renderer.enabled = false;
			} else {
				velocity_renderer.enabled = true;
				velocity_indi.forward = parent.Velocity;
			}
			if (parent.AngularVelocity == Vector3.zero) {
				angvel_renderer.enabled = false;
			} else {
				angvel_renderer.enabled = true;
				angvel_indi.forward = parent.AngularVelocity;
			}
		} else {
			Shown = false;
		}
	}

	/// <summary> Transitions from the ingmae coordinate system to the UnityEngine coordinate system </summary>
	public static Vector3 UI2Engine(Vector3 inp) {
		return new Vector3(-inp.x, inp.z, -inp.y);
	}
	
	/// <summary> Transitions from the UnityEngine coordinate system to the ingame coordinate system </summary>
	public static Vector3 Engine2UI(Vector3 inp) {
		return new Vector3(-inp.x, -inp.z, inp.y);
	}

	private enum Direction { x,y,z }
}
