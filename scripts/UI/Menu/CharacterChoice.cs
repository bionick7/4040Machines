using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterChoice : MonoBehaviour {

	public List<Button> character_slots;

	private RectTransform rtransf;

	private bool _on;
	public bool On {
		get { return _on; }
		set {
			rtransf.anchoredPosition = value ? new Vector3(300, 0): new Vector3(-300, 0);
			_on = value;
		}
	}

	private void Start () {
		rtransf = GetComponent<RectTransform>();
		List<Character> character_list = Globals.characters;
		for (int i=0; i < 8; i++) {
			if (i < character_list.Count) {
				string ch_name = string.Format("{0} {1}", character_list[i].forename, character_list[i].aftername);
				character_slots [i].GetComponentInChildren<Text>().text = ch_name;
			} else {
				character_slots [i].interactable = false;
			}
		}
	}

	public void Toggle () {
		On = !On;
		Globals.audio.UIPlay(UISound.soft_crackle);
	}

	public void ChooseCharacter(int num) {
		if (num >= Globals.characters.Count) return;
		Globals.persistend.LoadCharacter(Globals.characters [num]);
	}
}
