using UnityEngine;
using UnityEngine.UI;

public class ArrowIndicator : MonoBehaviour {

	private RectTransform top;
	private RectTransform rect_trans;
	private Camera cam;
	private Text description;
	private bool initialized = false;

	public string notice = "";

	private Image[] images;

	public SceneObject parent_object;
	public static ArrowUsage arrow_usage = ArrowUsage.velocity;

	private float radius;
	private float length;

	private const float arrow_thickness = .5f;

	private bool shown_;
	private bool Shown {
		get { return shown_; }
		set {
			if (shown_ != value) {
				foreach (Image img in images) {
					img.enabled = value;
				}
				description.enabled = value;
			}
			shown_ = value;
		}
	}

	private void Start () {
		if (!initialized)
			Start_();
		cam = SceneGlobals.map_camera;
	}

	public void Start_() {
		images = GetComponentsInChildren<Image>();
		radius = 1;
		length = 2;
		top = transform.GetChild(0).GetComponent<RectTransform>();
		description = GetComponentInChildren<Text>();
		rect_trans = GetComponent<RectTransform>();
		initialized = true;
		Shown = true;
	}

	private void Reshape (float pradius, float plength) {
		float act_radius = pradius * 15;
		top.sizeDelta = new Vector3(1, 10 / act_radius);
		rect_trans.sizeDelta = new Vector2(act_radius, plength) * 100;
	}

	public void VectorShape (Vector3 start, Vector3 direction, float thickness) {
		Vector3 projected = Vector3.ProjectOnPlane(direction, SceneGlobals.map_camera.transform.forward);
		Reshape(thickness, projected.magnitude);
		Vector3 pos = SceneGlobals.map_camera.WorldToScreenPoint(start);
		pos.z = 0;
		transform.position = pos;
		if (direction != Vector3.zero) {
			transform.up = Quaternion.Inverse(SceneGlobals.map_camera.transform.rotation) * projected;
		}
	}

	private void Update () {
		if (!SceneGlobals.general.InMap) {
			Shown = false;
			return;
		}

		switch (arrow_usage) {
		case ArrowUsage.velocity:
			Vector3 relative_velocity = SceneGlobals.ReferenceSystem.RelativeVelocity(parent_object.Velocity);
			description.text = relative_velocity.magnitude.ToString("0.00");
			if (Vector3.ProjectOnPlane(relative_velocity, SceneGlobals.map_camera.transform.forward).magnitude * SceneGlobals.velocity_multiplyer > 1
				&& SceneGlobals.velocity_multiplyer != 0
				&& relative_velocity != Vector3.zero) {
				Shown = parent_object.Exists && Vector3.Dot(cam.transform.forward, parent_object.Position - cam.transform.position) > 0;
				VectorShape(parent_object.Position,
							relative_velocity * SceneGlobals.velocity_multiplyer * .05f,
							arrow_thickness * .05f);
			} else {
				notice = "hide";
				Shown = false;
			}
			break;
		case ArrowUsage.acceleration:
			description.text = parent_object.Acceleration.magnitude.ToString("0.00");
			if (Vector3.ProjectOnPlane(parent_object.Acceleration, SceneGlobals.map_camera.transform.forward).magnitude * SceneGlobals.acceleration_multiplyer > .3f
				&& SceneGlobals.acceleration_multiplyer != 0
				&& parent_object.Acceleration != Vector3.zero) {
				Shown = Vector3.Dot(cam.transform.forward, parent_object.Position - cam.transform.position) > 0;
				VectorShape(parent_object.Position,
							parent_object.Acceleration * SceneGlobals.acceleration_multiplyer * .1f,
							arrow_thickness * .1f);
			} else {
				Shown = false;
			}
			break;
		default:
		case ArrowUsage.none:
			Shown = false;
			break;
		}
	}

	public static ArrowIndicator GetArrowIndicator(Vector3 start, Vector3 vector, float thickness) {
		GameObject arrow_obj = Instantiate(Resources.Load("prefs/Arrow") as GameObject);
		ArrowIndicator indicator_instance = arrow_obj.GetComponent<ArrowIndicator>();
		indicator_instance.Start_();
		indicator_instance.VectorShape(start, vector, thickness);
		return indicator_instance;
	}
	
	public enum ArrowUsage
	{
		velocity,
		acceleration,
		none
	}
}
