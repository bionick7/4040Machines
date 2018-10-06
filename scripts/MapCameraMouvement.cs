using UnityEngine;

public class MapCameraMouvement : MonoBehaviour {
	
	public Vector2 rot_speed = new Vector2(90, 90);
	public Vector3 pivot_point;

	
	private bool rotation_free;
	private float scroll_pos;
	private Vector3 init_mouse_pos;
	private MapDrawer map_drawer;

	private float Zoom {
		get { return (scroll_pos + 100) / 200; }
	}

	private void Start () {
		pivot_point = Vector3.zero;
		transform.position = new Vector3(0, 0, -20);
		scroll_pos = 1;

		transform.forward = pivot_point - transform.position;

		map_drawer = SceneData.mapdrawer;

		map_drawer.AddShape(new Polygon(100, 32, pivot_point, Vector3.up), 1);
		map_drawer.AddShape(new Line(pivot_point - 100 * Vector3.back, pivot_point + 100 * Vector3.back), 2);
		map_drawer.AddShape(new Line(pivot_point - 100 * Vector3.left, pivot_point + 100 * Vector3.left), 3);
		map_drawer.AddSpriteGroup(new Polygon(4, Vector3.zero, Vector3.up, Vector3.right * 110), map_drawer.sprites, new Vector2Int(10, 10), 1);
	}

	public void TunePivotPoint(Vector3 adjustment) {
		Vector3 act_adjustment = adjustment / (Zoom * 10);
		Vector3 plane_adjustment = new Vector3(act_adjustment.x, 0, act_adjustment.z);
		Quaternion plane_rotation = Quaternion.AngleAxis(SceneData.map_camera.transform.rotation.eulerAngles.y, Vector3.up);
		Vector3 final_vec = plane_rotation * plane_adjustment + Vector3.up * act_adjustment.y;
		pivot_point += final_vec;
		transform.position += final_vec;

		UpdateDraw();
	}

	private void UpdateDraw() {
		map_drawer.shapes [1] = new Polygon(100, 32, pivot_point, Vector3.up);
		map_drawer.shapes [2] = new Line(pivot_point - 100 * Vector3.back, pivot_point + 100 * Vector3.back);
		map_drawer.shapes [3] = new Line(pivot_point - 100 * Vector3.left, pivot_point + 100 * Vector3.left);
		//byte[] numeration = new byte[0]; map_drawer.sprite_groups.Keys.CopyTo(numeration, 0);
		//Debug.Log(string.Join(";", System.Array.ConvertAll(numeration, x => x.ToString())));
		map_drawer.sprite_groups [1].ChangeShape(new Polygon(4, pivot_point, Vector3.up, pivot_point + Vector3.right * 110));

		float mult = (pivot_point - transform.position).magnitude / 100f;

		map_drawer.Draw(1, mult);
		map_drawer.Draw(2, mult);
		map_drawer.Draw(3, mult);
	}
	
	private void Update() {
		if (!SceneData.general.InMap || SceneData.in_console) return;
		if (Input.GetMouseButtonDown(1)){
			rotation_free = true;
			init_mouse_pos = Input.mousePosition;
		}
		if (!Input.GetMouseButton(1)){
			rotation_free = false;
		}
	}

	private void LateUpdate () {
		if (!SceneData.general.InMap || SceneData.in_console) return;
		if (rotation_free){
			float dx = ((init_mouse_pos.x - Input.mousePosition.x) / Screen.width) * rot_speed.x * Time.deltaTime * 60;
			float dy = ((init_mouse_pos.y - Input.mousePosition.y) / Screen.height) * rot_speed.y * Time.deltaTime * 60;
			init_mouse_pos = Input.mousePosition;
			
			transform.RotateAround(pivot_point, transform.right, dy);
			transform.RotateAround(pivot_point + Vector3.Project(transform.position - pivot_point, Vector3.up), Vector3.up, -dx);
		}
		// Scrolling
		float scrolldelta = Input.mouseScrollDelta.y;
		if (scroll_pos * scrolldelta > 60f) return; 
		transform.position += (pivot_point - transform.position) * scrolldelta * .1f;
		scroll_pos += scrolldelta;
	}
}
