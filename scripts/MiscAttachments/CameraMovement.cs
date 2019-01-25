using UnityEngine;
using UnityEngine.EventSystems;
using FileManagement;

public class CameraMovement : MonoBehaviour {
	
	public Vector2 rot_speed = new Vector2(90, 90);

	public Vector3 init_dpos;
	public Quaternion init_relrot;

	public bool FreeRotation { get; set; }

	private Ship player;

	private Quaternion player_init_rotation;
	
	private bool rotation_free;
	private float scroll_pos;
	private Vector3 init_mouse_pos;
	

	private void Start () {		
		player = transform.parent.GetComponent<ShipControl>().myship ;
		player_init_rotation = Quaternion.Inverse(player.Transform.rotation);
	}
	
	private void Update() {
		if (SceneGlobals.general.InMap || SceneGlobals.in_console) return;
		if (Input.GetMouseButtonDown(1)){
			rotation_free = true;
			init_mouse_pos = Input.mousePosition;
		}
		if (!Input.GetMouseButton(1)){
			rotation_free = false;
		}
	}

	public void ChangeControl (Ship new_control) {
		player = new_control;
		DataStructure player_data = DataStructure.Load(new_control.config_path, "data", null).GetChild("player");
		try {
			init_dpos = player_data.Get<Vector3 []>("cam pos") [0];
			init_relrot = player_data.Get<Quaternion []>("cam rot") [0];
		} catch { }
		transform.SetParent(new_control.Transform);
	}

	private void LateUpdate () {
		if (SceneGlobals.general.InMap || SceneGlobals.in_console) return;
		if (rotation_free){
			float dx = ((init_mouse_pos.x - Input.mousePosition.x) / Screen.width) * rot_speed.x * Time.deltaTime * 60;
			float dy = ((init_mouse_pos.y - Input.mousePosition.y) / Screen.height) * rot_speed.y * Time.deltaTime * 60;
			init_mouse_pos = Input.mousePosition;

			Vector3 pivot_point = FreeRotation ? player.Position : transform.position;
			transform.RotateAround(pivot_point, transform.right, dy);
			transform.RotateAround(pivot_point + Vector3.Project(transform.position - pivot_point, player.Transform.up), player.Transform.up, -dx);
		}
		// Scrolling
		if (!EventSystem.current.IsPointerOverGameObject()) {
			float scrolldelta = Input.mouseScrollDelta.y;
			if (scroll_pos * scrolldelta > 10f) return;
			transform.position += (player.Position - transform.position) * scrolldelta * .1f;
			scroll_pos += scrolldelta;
		}
	}
}
