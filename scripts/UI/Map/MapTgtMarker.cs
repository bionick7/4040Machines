using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

[RequireComponent(typeof(RectTransform))]
public class MapTgtMarker : MonoBehaviour, IPointerClickHandler
{
	public RectTransform RectTransform { get; private set; }
	private Vector2 original_size;
	/// <summary> The object, the drawn object is referring to </summary>
	public IMarkerParentObject LinkedObject { get; set; }

	public static List<MapTgtMarker> marker_list = new List<MapTgtMarker>();

	private bool Selected {
		get { return MapCore.Active.selection.Contains(LinkedObject); }
	}

	public Image img;

	/// <summary> The Image or button of the drawn object, throws exception, if none is present </summary>
	public Behaviour[] GraphicsComponents {
		get {
			Behaviour[] imgs = System.Array.ConvertAll<Image, Behaviour>(GetComponentsInChildren<Image>(), x => (Behaviour) x);
			Behaviour[] butts = System.Array.ConvertAll<Button, Behaviour>(GetComponentsInChildren<Button>(), x => (Behaviour) x);
			Behaviour[] res = new Behaviour[imgs.Length + butts.Length];
			System.Array.Copy(imgs, res, imgs.Length);
			System.Array.Copy(butts, res, butts.Length);
			if (res.Length > 0) return res;
			throw new System.Exception(string.Format("Object {0} has neither an Image or a Button", name));
		}
	}

	private bool _enabled;
	/// <summary> True if shown, else false </summary>
	public bool Enabled {
		get { return _enabled; }
		set {
			if (!value) Position = new Vector2(-500, -500);
			_enabled = value;
		}
	}

	/// <summary> The position of the drawn object </summary>
	public Vector2 Position {
		get { return (Vector2) transform.position; }
		set { transform.position = (Vector3) value; }
	}

	public Vector2[] Corners {
		get {
			Vector2 size = RectTransform.sizeDelta;
			Vector2[] res = new Vector2[4];
			res [0] = new Vector2( 0, size.y / 2) + Position;
			res [1] = new Vector2( 0,-size.y / 2) + Position;
			res [2] = new Vector2( size.x / 2, 0) + Position;
			res [3] = new Vector2(-size.y / 2, 0) + Position;
			return res;
		}
	}

	public void Start () {
		RectTransform = GetComponent<RectTransform>();
		original_size = RectTransform.sizeDelta;
		marker_list.Add(this);
	}

	/// <summary> Updates the position of the object </summary>
	/// <param name="camera"> The map camera supposed to render the linked object </param>
	private void Update () {
		if (LinkedObject == null) return;
		if (LinkedObject.Friendly) {
			if (Selected) img.color = MapCore.friendly_color_selected;
			else img.color = MapCore.friendly_color;
		} else {
			if (Selected) img.color = MapCore.ennemy_color_selected;
			else img.color = MapCore.ennemy_color;
		}

		var camera = SceneGlobals.map_camera;
		marker_list.RemoveAll(x => x == null);
		if (!LinkedObject.Exists) {
			marker_list.Remove(this);
			Destroy(gameObject);
			return;
		}
		Transform camera_transform = camera.transform;

		// Position
		if (Vector3.Angle(camera_transform.forward, LinkedObject.Position - camera_transform.position) >= camera.fieldOfView) {
			Enabled = false;
		} else {
			if (!Enabled) Enabled = true;
			Vector3 pos = camera.WorldToScreenPoint(LinkedObject.Position);
			Position = new Vector2Int((int) pos.x, (int) pos.y);
		}

		// Size
		float dist = Vector3.Distance(camera_transform.position, LinkedObject.Position);
		RectTransform.sizeDelta = original_size * (30 / dist + .3f);
	}

	public void OnPointerClick(PointerEventData pdata) {
		if (pdata.pointerPress == gameObject) {
			if (pdata.button == PointerEventData.InputButton.Right)
				MapCore.Active.TgtMarkerClickedR(this);
			else if (pdata.button == PointerEventData.InputButton.Left)
				MapCore.Active.TgtMarkerClickedL(this);
		}
	}
}