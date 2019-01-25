using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class KeyBindingInput : MonoBehaviour,
IPointerEnterHandler, IPointerExitHandler
{
	private RectTransform rect_trans;
	private Text function_text;
	private Text key_text;
	private Image img;

	public KeyBinding associated;
	public KeyBindingOptions parent;

	private static Color idle_color = Color.white;
	private static Color hover_color = new Color(.9f, .9f, .9f);

	private bool _hover;
	public bool Hover {
		get { return _hover; }
		set {
			img.color = value ? hover_color : idle_color;
			key_text.text = value ? "---" : KeyText;
			_hover = value;
		}
	}

	private string KeyText {
		get { return string.Join(" + ", System.Array.ConvertAll(associated.act_code, c => c.ToString())); }
	}

	public void Initialize (KeyBinding p_associated, KeyBindingOptions p_parent, Vector3 position) {
		associated = p_associated;
		parent = p_parent;
		rect_trans = GetComponent<RectTransform>();
		function_text = transform.GetChild(0).GetComponent<Text>();
		key_text = transform.GetChild(1).GetComponent<Text>();
		img = GetComponent<Image>();
		
		transform.SetParent(parent.content);
		rect_trans.anchoredPosition = position;

		function_text.text = associated.function;
		key_text.text = KeyText;

		Hover = false;
	}

	private KeyCode[] GetPressedKeys () {
		List<KeyCode> res = new List<KeyCode>();
		for (ushort i=0; i < 509; i++) {
			if (Input.GetKey((KeyCode) i) && i != 323) res.Add((KeyCode) i);
		}
		return res.ToArray();
	}

	private void Update () {
		if (Hover) {
			if (Input.anyKey) {
				KeyCode[] new_keys = GetPressedKeys();
				key_text.text = string.Join(" + ", System.Array.ConvertAll(new_keys, c => c.ToString()));
			} else {
				key_text.text = "---";
			}
		}
	}

	public void Confirm () {
		KeyCode[] new_keys = GetPressedKeys();
		associated.act_code = new_keys;
		associated.keyids = System.Array.ConvertAll(new_keys, c => (ushort) c);
		parent.Changed(associated, associated.function);
		Globals.audio.UIPlay(UISound.soft_crackle);
	}

	public void OnPointerEnter (PointerEventData pdata) {
		Hover = true;
		Globals.audio.UIPlay(UISound.soft_click, .2f);
	}

	public void OnPointerExit (PointerEventData pdata) {
		Hover = false;
	}
}
