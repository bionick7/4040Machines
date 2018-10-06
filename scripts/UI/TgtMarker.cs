using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TgtMarker : MonoBehaviour, IPointerClickHandler
{

	public Ship parent;
	
	public bool is_original = false;

	public bool Activated {
		get {
			return player.Target == parent.associated_target;
		}
		private set {
			if (value) {
				player.Target = parent.associated_target;
			} else {
				player.Target = Target.None;
			}
			img.color = value ? new Color(1,1,0) : new Color(1,0,0);
		}
	}

	private Camera _camera;
	private Button button;
	private Image img;
	private Ship player;

	private void Start () {
		_camera = GameObject.Find("ShipCamera").GetComponent<Camera>();
		button = GetComponent<Button>();
		img = GetComponent<Image>();
		player = SceneObject.PlayerObj().GetComponent<ShipControl>().myship;
	}
	

	private void Update () {
		if (!is_original) {
			if (parent.Exists && _camera != null) {
				if (Vector3.Angle(_camera.transform.forward, parent.Position - _camera.transform.position) < 90) {
					transform.position = _camera.WorldToScreenPoint(parent.Position);
					img.color = Activated ? Color.yellow : Color.red;
				}
				else {
					transform.position = new Vector3(-200, -200);
				}
			} else {
				Destroy(gameObject);
			}
		}
	}

	public void OnPointerClick(PointerEventData data) {
		Activated = !Activated;
	}
}
