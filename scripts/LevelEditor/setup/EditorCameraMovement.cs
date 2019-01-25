using UnityEngine;

/* ======================================================
 * Manages the movement of the camera in the leveleditor.
 * ======================================================*/

public class EditorCameraMovement : MonoBehaviour {

	/// <summary> The speed of the camera's rotation in both axis </summary>
	public Vector2 rot_speed;
	/// <summary> The speed of the camera's transition </summary>
	public Vector3 transspeed = new Vector3(10, 10, 10);

	private Vector3 init_mouse_pos;
	public float scroll_pos;
	private bool rotation_free;
	private Vector3 pivot_point;
	private bool dragging = false;

	private KeyBindingCollection keys;
	private Vector3 bef_mousepos;

	private void Start () {
		init_mouse_pos = Vector3.zero;
		scroll_pos = 0;
		rotation_free = false;
		pivot_point = Vector3.zero;
		keys = Globals.bindings;
	}

	/// <summary> Translates the camers </summary>
	/// <param name="transition"> Translation vector</param>
	private void Translate(Vector3 transition) {
		pivot_point += transition;
		transform.position += transition;
	}

	private void Update () {
		if (Input.GetMouseButtonDown(1)){
			rotation_free = true;
			init_mouse_pos = Input.mousePosition;
		}
		if (!Input.GetMouseButton(1)){
			rotation_free = false;
		}

		// Transition from keyboard
		Vector3 transition = Vector3.zero;
		float y_rot = transform.rotation.eulerAngles.y * Mathf.Deg2Rad;
		if (keys.map_move_fore.IsPressed()) {
			transition.x += transspeed.z * Time.deltaTime * Mathf.Sin(y_rot);
			transition.z += transspeed.z * Time.deltaTime * Mathf.Cos(y_rot);
		} else if (keys.map_move_back.IsPressed()) {
			transition.x -= transspeed.z * Time.deltaTime * Mathf.Sin(y_rot);
			transition.z -= transspeed.z * Time.deltaTime * Mathf.Cos(y_rot);
		}
		if (keys.map_move_right.IsPressed()) {
			transition.x += transspeed.x * Time.deltaTime * Mathf.Cos(y_rot);
			transition.z -= transspeed.x * Time.deltaTime * Mathf.Sin(y_rot);
		} else if (keys.map_move_left.IsPressed()) {
			transition.x -= transspeed.x * Time.deltaTime * Mathf.Cos(y_rot);
			transition.z += transspeed.x * Time.deltaTime * Mathf.Sin(y_rot);
		}
		if (keys.map_move_up.IsPressed()) {
			transition.y = transspeed.y * Time.deltaTime;
		} else if (keys.map_move_down.IsPressed()) {
			transition.y = -transspeed.y * Time.deltaTime;
		}
		Translate(transition * transform.position.magnitude * .1f);

		// Mouse dragging handling
		if (Input.GetMouseButtonDown(2)) {
			dragging = true;
			bef_mousepos = Input.mousePosition;
		} else if (Input.GetMouseButtonUp(2)) dragging = false;

		if (dragging) {
			Vector3 curr_mousepos = Input.mousePosition;
			Vector3 d_pos = bef_mousepos - curr_mousepos;
			Translate(transform.rotation * d_pos * Time.deltaTime * transform.position.magnitude * .1f);
			bef_mousepos = curr_mousepos;
		}
	}

	private void LateUpdate () {
		// Rotation
		if (rotation_free){
			float dx = ((init_mouse_pos.x - Input.mousePosition.x) / Screen.width) * rot_speed.x * Time.deltaTime * 60;
			float dy = ((init_mouse_pos.y - Input.mousePosition.y) / Screen.height) * rot_speed.y * Time.deltaTime * 60;
			init_mouse_pos = Input.mousePosition;

			transform.RotateAround(pivot_point, transform.right, dy);
			transform.RotateAround(pivot_point + Vector3.Project(transform.position - pivot_point, Vector3.up), Vector3.up, -dx);
		}

		// Scrolling
		float scrolldelta = Input.mouseScrollDelta.y;
		if (scroll_pos * scrolldelta > 100f) return;
		transform.position += (pivot_point - transform.position) * scrolldelta * .1f;
		scroll_pos += scrolldelta;
	}
}
