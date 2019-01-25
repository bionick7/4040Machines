using UnityEngine;

public class CameraBehaviour : MonoBehaviour
{
	public Camera camera_inst;

	private Vector3 startpos;
	private Quaternion startrot;

	private Vector3 velocity;
	private Vector3 angular_velocity;
	private Transform planed_parent;
	private bool sticking = false;
	private Vector3 sticking_offset;
	private bool mouse_moving = false;

	private float moving_time = -1;
	float bef_mouse_pos_x = 0;
	
	private const float zoom_time = 1;

	private void Start () {
		camera_inst = GetComponent<Camera>();

		startpos = transform.position;
		startrot = transform.rotation;
	}
	
	private void Update () {
		if (moving_time - Time.time > 0) {
			// Moving to target
			transform.position += velocity * Time.deltaTime;
		} else {
			if (!sticking && planed_parent != null) {
				sticking_offset = Quaternion.Inverse(transform.rotation) * (transform.position - planed_parent.position);
				sticking = true;
			}
		}
		if (sticking) {
			transform.position = planed_parent.transform.position + transform.rotation * sticking_offset;
			if (Input.GetMouseButton(1)) {
				if (mouse_moving) {
					float mouse_delta = Input.mousePosition.x - bef_mouse_pos_x;
					transform.RotateAround(planed_parent.position, Vector3.up, mouse_delta);
				}
				bef_mouse_pos_x = Input.mousePosition.x;
				mouse_moving = true;
			} else {
				mouse_moving = false;
			}
		}
	}

	public void Stick (Celestial parent) {
		UnStick();
		Vector3 parent_pos = parent.parent_celestial.transform.position;
		Vector3 pred_point;
		if (parent.parent_celestial.is_static) {
			pred_point = Quaternion.AngleAxis(parent.angular_velocity * zoom_time, parent.orbit_plane) * parent.transform.position;
		} else {
			pred_point = Quaternion.AngleAxis(parent.parent_celestial.angular_velocity * zoom_time, parent.parent_celestial.orbit_plane) * parent_pos +
				Quaternion.AngleAxis(parent.angular_velocity * zoom_time, parent.orbit_plane) * (parent.transform.position - parent_pos);
		}
		float max_sattelite_radius = parent.Radius;
		foreach (Celestial satelite in parent.satellites) {
			if (satelite.OrbitalRadius > max_sattelite_radius)
				max_sattelite_radius = satelite.OrbitalRadius;
		}
		MoveTo(pred_point - transform.rotation * new Vector3(0.2f, 0, 1) * (1 + max_sattelite_radius), zoom_time);
		planed_parent = parent.transform;
		CampagneManager.planet_view = parent.data;
	}

	public void UnStick () {
		sticking = false;
		CampagneManager.planet_view = CelestialData.None;
		transform.rotation = startrot;
		planed_parent = null;
		MoveTo(startpos, 1);
	}

	public void MoveTo (Vector3 p_position, float time) {
		moving_time = Time.time + time;
		velocity = (p_position - transform.position) / time;
	}
}