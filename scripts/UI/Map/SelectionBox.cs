using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

[RequireComponent(typeof(RectTransform))]
public class SelectionBox : MonoBehaviour
{
	private RectTransform rect_trans;

	private bool mb_down;
	private bool dragging;
	private Vector3 start_point;

	private void Start () {
		rect_trans = GetComponent<RectTransform>();
	}
	
	private void Update () {
		if (Input.GetMouseButtonUp(0)) {
			mb_down = false;
			dragging = false;
		}
		if (!EventSystem.current.IsPointerOverGameObject()) {
			if (Input.GetMouseButtonDown(0)) {
				mb_down = true;
				start_point = Input.mousePosition;
			}
			if (dragging) {
				MarkRectangle(start_point, Input.mousePosition);
			} else {
				rect_trans.sizeDelta = Vector3.one;
				rect_trans.position = new Vector3(-2, -2);
			}
			if (mb_down && Vector3.Distance(Input.mousePosition, start_point) > 5) {
				dragging = true;
			}
		}


	}

	private void MarkRectangle(Vector3 start_pos, Vector3 end_pos) {
		Vector3 size = (end_pos - start_pos);
		size.y *= -1;
		rect_trans.sizeDelta = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y));
		rect_trans.position = start_pos + (end_pos - start_pos) / 2;

		List<IMarkerParentObject> selected = new List<IMarkerParentObject>();
		Vector3 rect_vec = end_pos - start_pos;
		Vector3 mrk_vec;
		foreach (MapTgtMarker marker in MapTgtMarker.marker_list) {
			if (marker.gameObject) {
				mrk_vec = (Vector3) marker.Position - start_pos;
				if (Vector3.Angle(rect_vec, mrk_vec) < 45 && rect_vec.sqrMagnitude > mrk_vec.sqrMagnitude)
					selected.Add(marker.LinkedObject);
			}
		}
		MapCore.Active.selection = selected;
	}
}
