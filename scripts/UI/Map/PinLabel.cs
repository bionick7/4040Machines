using UnityEngine;
using UnityEngine.UI;

public class PinLabel : MonoBehaviour {

	private Image img;
	private Text text;

	private static readonly Vector3 offset = new Vector3(120, -20);

	public IAimable Object = null;
	public static PinLabel Active { get; set; }

	public PinContext Context { get; set; }
	
	private static bool dragging_ = false;
	public static bool Dragging {
		get {
			return dragging_;
		}
		set {
			Cursor.SetCursor(Resources.Load(value ? "Pin" : "Pointer") as Texture2D, value ? new Vector2(0, 50) : Vector2.zero, CursorMode.Auto);
			Active.Shown = value;
			dragging_ = value;
		}
	}

	private bool _shown;
	public bool Shown {
		get { return _shown; }
		set {
			img.enabled = value;
			text.enabled = value;
			_shown = value;
		}
	}

	private void Start () {
		img = GetComponent<Image>();
		text = GetComponentInChildren<Text>();
		Active = this;
		Shown = false;
	}

	private void Update () {
		transform.position = Input.mousePosition + offset;
		if (Shown) {
			Ray screenray = SceneGlobals.map_camera.ScreenPointToRay(Input.mousePosition);
			RaycastHit currenthit;
			if (Physics.Raycast(screenray, out currenthit)) {
				IAimable aimable = GetAimable(currenthit.transform);
				ShipPart part = (ShipPart) aimable;
				if (part != null) {
					// If target is part
					Object = new PartOffsetAim(currenthit.point, part);
					text.text = part.Name();
				} else {
					DestroyableTarget dest_tgt = (DestroyableTarget) aimable;
					if (dest_tgt != null) {
						// If target is destroyable target
						Object = new PhysicsOffsetAim(currenthit.point, dest_tgt);
						text.text = part.Name();
					} else {
						// void
						text.text = currenthit.transform.name;
					}
				}
			} else {
				Object = null;
				foreach (MapTgtMarker marker in MapTgtMarker.marker_list) {
					if (marker.RectTransform.rect.Contains((Vector2) Input.mousePosition - marker.Position)) {
						// If target is ship
						Object = marker.LinkedObject;
						text.text = marker.LinkedObject.Name;
						//Debug.DrawLine(Vector3.zero, Object.Position, Color.red, 100);
					}
				}
				if (Object == null) {
					// If no object selected
					text.text = "";
				}
			}

			// Mousebutton unleashed
			if (!Input.GetMouseButton(0) && Dragging) {
				Dragging = false;
				switch (Context) {
				default:
				case PinContext.turret:
					break;
				case PinContext.selector:
					MapCore.Active.selector_event_system.DraggedTo(Object);
					break;
				}
				Context = PinContext.none;
			}
		}
	}

	public static IAimable GetAimable (Transform trns) {
		GameObject g_object = trns.gameObject;
		var part = g_object.GetComponent<BulletCollisionDetection>();
		return part == null ? null : part.Part;
	}

	public enum PinContext
	{
		none = 0,
		turret = 1,
		selector = 2,
	}
}
