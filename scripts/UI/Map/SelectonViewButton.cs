using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Image))]
public class SelectonViewButton : MonoBehaviour,
IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
	public Text label;
	public SelectionViewer parent;

	public IMarkerParentObject referred;
	private bool hover = false;
	private bool remove = false;

	private Selector selector = null;
	private Image img;

	private static readonly Vector2 offset = new Vector2(200, 0);

	private void Start () {
		img = GetComponent<Image>();
		img.color = referred.Friendly ? MapCore.friendly_color_selected : MapCore.ennemy_color_selected;

		label = GetComponentInChildren<Text>();
		label.text = referred.Name;
	}

	private void Update () {
		if (hover) {
			// If the mouse hovers over the Button
			SceneGlobals.ui_script.HighlightingPosition = referred.Marker.Position;
			remove = true;
		} else {
			if (remove) {
				SceneGlobals.ui_script.HighlightingPosition = new Vector3(-200, -200);
				remove = false;
			}
		}
	}

	public static SelectonViewButton Get (IMarkerParentObject p_obj, SelectionViewer p_parent, GameObject template) {
		GameObject game_object = Instantiate(template);
		var res = Loader.EnsureComponent<SelectonViewButton>(game_object);
		res.referred = p_obj;
		return res;
	}

	public void DeSelect () {
		hover = false;
		remove = true;
		selector = null;
	}

	public void OnPointerClick (PointerEventData data) {
		if (data.button == PointerEventData.InputButton.Left) {
			parent.Remove(referred);
			SceneGlobals.ui_script.HighlightingPosition = new Vector3(-200, -200);
		} else if (data.button == PointerEventData.InputButton.Right) {
			if (selector == null) {
				selector = MapCore.Active.AddSelectorForSceneObject(referred, (Vector2) transform.position + offset, (Vector2) transform.position + new Vector2(parent.button_dimensions.x / 2, 0),
																	 referred.Friendly ? MapCore.friendly_color_selected : MapCore.ennemy_color_selected);
			}
		}
	}

	public void OnPointerEnter (PointerEventData data) {
		hover = true;
	}

	public void OnPointerExit  (PointerEventData data) {
		hover = false;
	}
}
