using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TgtMarker : MonoBehaviour, IPointerClickHandler
{
	public ITargetable parent;
	private Target parent_tgt;
	private bool started = false;
	
	/// <summary> If the ship is the player </summary>
	public bool is_original = false;
	private bool is_deactivated;

	private string RelativeVelocity {
		get { return pred_point_velocity.text; }
		set { pred_point_velocity.text = value; }
	}

	private string Distance {
		get { return pred_point_distance.text; }
		set { pred_point_distance.text = value; }
	}

	public bool Activated {
		get { return player.Target == parent_tgt.Associated; }
		private set {
			if (value) {
				player.Target = parent_tgt.Associated;
				Activate();
			} else {
				player.Target = Target.None;
				Deactivate();
			}
			img.color = value ? new Color(1,1,0) : new Color(1,0,0);
		}
	}

	private Camera _camera;
	private Button button;
	private Image img;
	private Ship player;
	private ShipControl player_control;

	private Image predicted_point;
	private Text pred_point_velocity;
	private Text pred_point_distance;

	public void Start () {
		// For the template object
		if (parent == null) return;

		_camera = SceneGlobals.ship_camera;
		button = GetComponent<Button>();
		img = GetComponent<Image>();
		player = SceneGlobals.Player;
		player_control = player.Object.GetComponent<ShipControl>();
		parent_tgt = parent.Associated;
		predicted_point = Instantiate(GameObject.Find("pred_point")).GetComponent<Image>();
		predicted_point.transform.SetParent(transform);
		
		pred_point_velocity = predicted_point.GetComponentsInChildren<Text>() [0];
		pred_point_distance = predicted_point.GetComponentsInChildren<Text>() [1];

		var clicked_color = parent_tgt.Friendly ? new Color(.25f, .25f, .92f) : new Color(.92f, .92f, .25f);
		predicted_point.color = pred_point_velocity.color = pred_point_distance.color = clicked_color;

		Deactivate();
		started = true;
	}
	

	private void Update () {
		if (!started || is_original) return;
		if (parent_tgt.Exists && _camera != null) {
			if (Vector3.Angle(_camera.transform.forward, parent_tgt.Position - _camera.transform.position) < 90) {
				// Executed if seen

				transform.position = _camera.WorldToScreenPoint(parent_tgt.Position);

				// Calculates predicted point
				Vector3 pred_pos = Vector3.zero;
				if (player.Parts.GetAll<Weapon>().Length > 0)
					pred_pos = _camera.WorldToScreenPoint(player_control.Predicted(player.Parts.GetAll<Weapon>() [0], parent_tgt.Position, parent_tgt.Velocity));
				else if (player.Parts.GetAll<Turret>().Length > 0)
					pred_pos = _camera.WorldToScreenPoint(player_control.Predicted(player.Parts.GetAll<Turret>() [0], parent_tgt.Position, parent_tgt.Velocity));
				if (Activated) {
					pred_pos.z = 0;
					predicted_point.transform.position = pred_pos;
					RelativeVelocity = Vector3.Distance(parent_tgt.Velocity, player.Velocity).ToString();
					Distance = Vector3.Distance(parent_tgt.Position, player.Position).ToString();
				} else {
					if (!is_deactivated) {
						Deactivate();
					}
				}
				predicted_point.color = img.color = parent_tgt.Friendly ? Color.blue :  Activated ? Color.yellow : Color.red;
				predicted_point.enabled = Activated;
			}
			else {
				transform.position = new Vector3(-200, -200);
			}
		} else {
			Destroy(gameObject);
		}
	}

	public void Activate () {
		img.color = parent_tgt.Friendly ? Color.blue : Color.yellow;
		predicted_point.enabled = true;
		pred_point_distance.enabled = true;
		pred_point_velocity.enabled = true;

		is_deactivated = false;
	}

	public void Deactivate () {
		img.color = parent_tgt.Friendly ? Color.blue : Color.red;
		pred_point_distance.enabled = false;
		pred_point_velocity.enabled = false;
		predicted_point.enabled = false;

		is_deactivated = true;
	}

	public void OnPointerClick(PointerEventData data) {
		Activated = !Activated;
	}

	public static TgtMarker Instantiate(ITargetable p_parent, byte parent_type) {
		// FileReader.FileLog("Marker Instantiated", LogType.loader);
		GameObject marker_obj;
		// Instantiates normal UI marker
		switch (parent_type) {
		case 1:
			// Ship
			marker_obj = Instantiate(GameObject.Find("tgt_pos_marker")) as GameObject;
			break;
		case 2:
			// Missile
			marker_obj = Instantiate(GameObject.Find("tgt_pos_marker2")) as GameObject;
			break;
		default:
			goto case 1;
		}
		marker_obj.transform.SetParent(GameObject.Find("MainCanvas").transform);
		marker_obj.transform.SetSiblingIndex(1);
		var marker = Loader.EnsureComponent<TgtMarker>(marker_obj);
		marker.parent = p_parent;
		marker.is_original = false;
		return marker;
	}
}
