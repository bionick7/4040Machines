using UnityEngine;
using UnityEngine.EventSystems;

public class MapCameraMouvement : MonoBehaviour {
	
	public Vector2 rot_speed = new Vector2(90, 90);

	private bool dragging;
	private Vector3 bef_mousepos;

	/// <summary> Global position of the point around which the camera rotates </summary>
	public Vector3 PivotPoint {
		get { return SceneGlobals.ReferenceSystem.Position; }
		set { SceneGlobals.ReferenceSystem.Position = value; }
	}

	private Vector3 bef_pivot_point;

	public static MapCameraMouvement active;

	private bool rotation_free;
	private float scroll_pos;
	private Vector3 init_mouse_pos;
	private Vector3 offset = Vector3.zero;

	private float Zoom {
		get { return (scroll_pos + 100) / 200; }
	}

	private void Start () {
		Camera camera_component = GetComponent<Camera>();
		camera_component.farClipPlane = 1e9f;

		transform.position = new Vector3(0, 0, -20);
		scroll_pos = 1;

		transform.forward = PivotPoint - transform.position;
		Debug.DrawRay(transform.position, PivotPoint - transform.position, Color.white, 100);

		bef_pivot_point = PivotPoint;

		active = this;
	}

	/// <summary> Moves the pivot point around </summary>
	/// <param name="adjustment"> The adjustments made to the pivot point from the perspective of the camera </param>
	public void TunePivotPoint (Vector3 adjustment) {
		Vector3 act_adjustment = adjustment / (Zoom * 10);
		Vector3 plane_adjustment = new Vector3(act_adjustment.x, 0, act_adjustment.z);
		Quaternion plane_rotation = Quaternion.AngleAxis(SceneGlobals.map_camera.transform.rotation.eulerAngles.y, Vector3.up);
		Vector3 final_vec = plane_rotation * plane_adjustment + Vector3.up * act_adjustment.y;

		SceneGlobals.ReferenceSystem.Offset += final_vec;
	}
	
	private void Update() {
		if (!SceneGlobals.general.InMap) return;
		SceneGlobals.map_core.UpdateDraw(PivotPoint, transform);

		if (SceneGlobals.in_console) return;
		if (Input.GetMouseButtonDown(1)){
			rotation_free = true;
			init_mouse_pos = Input.mousePosition;
		}
		if (!Input.GetMouseButton(1)){
			rotation_free = false;
		}
		if (bef_pivot_point != PivotPoint) {
			transform.position += PivotPoint - bef_pivot_point;
		}
		bef_pivot_point = PivotPoint;

		if (!EventSystem.current.IsPointerOverGameObject()) {
			// Scrolling
			float scrolldelta = Input.mouseScrollDelta.y;
			if (scroll_pos * scrolldelta > 80f) return;
			transform.position += (PivotPoint - transform.position) * scrolldelta * .1f;
			scroll_pos += scrolldelta;

			// Mouse dragging handling
			if (Input.GetMouseButtonDown(2)) {
				dragging = true;
				bef_mousepos = Input.mousePosition;
			} else if (Input.GetMouseButtonUp(2)) dragging = false;

			if (dragging) {
				Vector3 curr_mousepos = Input.mousePosition;
				Vector3 d_pos = bef_mousepos - curr_mousepos;
				TunePivotPoint(d_pos * Time.deltaTime * Vector3.Distance(transform.position, PivotPoint) * .3f);
				bef_mousepos = curr_mousepos;
			}
		}
	}

	private void LateUpdate () {
		if (!SceneGlobals.general.InMap || SceneGlobals.in_console) return;
		if (rotation_free){
			float dx = ((init_mouse_pos.x - Input.mousePosition.x) / Screen.width) * rot_speed.x * Time.deltaTime * 60;
			float dy = ((init_mouse_pos.y - Input.mousePosition.y) / Screen.height) * rot_speed.y * Time.deltaTime * 60;
			init_mouse_pos = Input.mousePosition;
			
			transform.RotateAround(PivotPoint, transform.right, dy);
			transform.RotateAround(PivotPoint + Vector3.Project(transform.position - PivotPoint, Vector3.up), Vector3.up, -dx);
		}
	}
}
