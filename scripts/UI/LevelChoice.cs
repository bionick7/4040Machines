using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class LevelChoice : MonoBehaviour {

	public Button level_template_button;

	private Transform content_transform;

	private Dictionary<Button, string> level_buttons = new Dictionary<Button, string>();

	private bool _shown;
	public bool Shown {
		get {
			return _shown;
		}
		set {
			transform.position = value ? new Vector3(300, Screen.height / 2 - 25) : new Vector3(-1000, 1000);
			_shown = value;
		}
	}


	private void Start () {
		content_transform = transform.GetChild(0).GetChild(0);
		DisplayButtons();
	}

	private void DisplayButtons () {
		List<DataStructure> battles = new List<DataStructure>(Data.battle_list.AllChildren);
		content_transform.GetComponent<RectTransform>().sizeDelta = new Vector3(500, 300 * battles.Count + 20);
		for (ushort i=0; i < battles.Count; i++) {
			GameObject button_obj = Instantiate(level_template_button.gameObject);
			DataStructure battle_data = battles[i];
			button_obj.GetComponentInChildren<Text>().text = battle_data.Name;
			button_obj.transform.SetParent(content_transform);
			button_obj.transform.localPosition = new Vector3(150, -150 -300 * i);
			level_buttons.Add(button_obj.GetComponent<Button>(), battle_data.Name);
		}
	}
	
	public void OnClick () {
		foreach (KeyValuePair<Button, string> pair in level_buttons) {
			Rect global_rect = AmmoChooser.GetWorldRect(pair.Key.GetComponent<RectTransform>());
			if (global_rect.Contains(Input.mousePosition)) {
				Data.persistend.LoadBattle(pair.Value);
			}
		}
	}
}
