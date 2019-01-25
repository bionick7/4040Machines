using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KeyBindingOptions : MonoBehaviour
{
	public KeyBindingInput template;
	public Text title;

	private KeyBindingCollection bindings;
	private RectTransform rect;
	public Transform content;

	private bool _shown;
	public bool Shown {
		get {
			return _shown;
		}
		set {
			if (value) Init();
			else Exit(false);
			_shown = value;
		}
	}

	private void SpawnButtons () {
		ushort i = 1;
		foreach(KeyValuePair<string, List<KeyBinding>> pair in bindings.binding_dict) {
			GameObject title_obj = Instantiate(title.gameObject);
			title_obj.transform.SetParent(content);
			title_obj.GetComponent<RectTransform>().anchoredPosition = new Vector3(210, -30 * i);
			title_obj.GetComponent<Text>().text = pair.Key;
			i++;
			foreach (KeyBinding binding in pair.Value) {
				GameObject binding_image = Instantiate(template.gameObject);
				binding_image.GetComponent<KeyBindingInput>().Initialize(binding, this, new Vector3(210, -30 * i));
				i++;
			}
		}
		content.GetComponent<RectTransform>().sizeDelta = new Vector3(500, i * 30);
	}

	public void Changed (KeyBinding new_binding, string function) {
		bindings.Change(function, new_binding);
	}

	private void Start () {
		bindings = Globals.bindings;
		rect = GetComponent<RectTransform>();
		content = transform.GetChild(0).GetChild(0);
		SpawnButtons();
	}

	public void Init () {
		rect.position = new Vector3(300, Screen.height / 2 - 25);
	}

	public void Exit (bool from_button) {
		rect.position = new Vector3(-500, 0);
		if (from_button) Globals.audio.UIPlay(UISound.soft_crackle);
		bindings.Save();
	}
}