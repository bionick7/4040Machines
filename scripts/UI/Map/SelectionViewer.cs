using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(ScrollRect))]
public class SelectionViewer : MonoBehaviour
{
	public RectTransform content;
	public RectTransform button_template;

	public List<IMarkerParentObject> selected = new List<IMarkerParentObject>();
	public List<SelectonViewButton> buttons = new List<SelectonViewButton>();
	private Dictionary<IMarkerParentObject, SelectonViewButton> button_dict = new Dictionary<IMarkerParentObject, SelectonViewButton>();

	public Vector2 button_dimensions;

	private void Start () {
		MapCore.Active.selection_viewer = this;
		button_dimensions = button_template.sizeDelta;
	}

	public void Add (IMarkerParentObject p_obj) {
		if (selected.Contains(p_obj) || button_dict.ContainsKey(p_obj)) return;
		SelectonViewButton button = SelectonViewButton.Get(p_obj, this, button_template.gameObject);
		button.parent = this;

		content.sizeDelta = new Vector2(content.sizeDelta.x, selected.Count * button_dimensions.y);

		RectTransform button_rect = button.GetComponent<RectTransform>();

		button_rect.SetParent(content);
		button_rect.anchoredPosition = new Vector3(button_dimensions.x * .5f, -(selected.Count + .5f) * button_dimensions.y);

		selected.Add(p_obj);
		buttons.Add(button);
		button_dict.Add(p_obj, button);
	}

	public void Remove (IMarkerParentObject p_obj) {
		selected.Remove(p_obj);
		SelectonViewButton button = button_dict [p_obj];
		int indx = buttons.IndexOf(button);
		button_dict.Remove(p_obj);
		buttons.Remove(button);
		button.DeSelect();
		Destroy(button.gameObject);

		for (int i=indx; i < buttons.Count; i++) {
			buttons [i].transform.position += new Vector3(0, button_dimensions.y);
		}

		content.sizeDelta = new Vector2(content.sizeDelta.x, selected.Count * button_dimensions.y);
	}

	public void SelectAll () {
		foreach (Ship ship in SceneGlobals.ship_collection) {
			Add(ship);
		}
		foreach (DestroyableTarget tgt in SceneGlobals.destroyables) {
			Add(tgt.Associated);
		}
	}

	public void Selectenemies () {
		foreach (Ship ship in SceneGlobals.ship_collection) {
			if (!ship.Friendly) Add(ship);
		}
		foreach (DestroyableTarget tgt in SceneGlobals.destroyables) {
			if (!tgt.Friendly) Add(tgt.Associated);
		}
	}

	public void SelectFriends () {
		foreach (Ship ship in SceneGlobals.ship_collection) {
			if (ship.Friendly) Add(ship);
		}
		foreach (DestroyableTarget tgt in SceneGlobals.destroyables) {
			if (tgt.Friendly) Add(tgt.Associated);
		}
	}
}
