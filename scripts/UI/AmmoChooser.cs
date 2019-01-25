using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class AmmoChooser : MonoBehaviour {

	public Button original;

	private Ship Ship {
		get { return SceneGlobals.Player; }
	}
	public Transform content;

	private List<Ammunition> ammos = new List<Ammunition>();
	private Button[] buttons;
	private byte current_amm = 0x00;
	private bool ammo_present = true;

	private bool hidden = false;

	private static readonly Color pressed = new Color(0xc9/255f, 0x6a/255f, 0x51/255f);		// #c96a51
	private static readonly Color idle = new Color(0xb4/255f, 0xb7/255f, 0x9f/255f);		// #b4b79f

	private void Start () {
		SpawnButtons();
		if (ammo_present) {
			AmmoButtonClicked();
		}
	}

	private void SpawnButtons() {
		byte i = 0x00;

		int ammo_num = Ship.AmmoAmounts.Count;
		buttons = new Button [ammo_num];

		content.GetComponent<RectTransform>().sizeDelta = new Vector3(200, 100 * ammo_num + 20);

		foreach (KeyValuePair<Ammunition, uint> pair in Ship.AmmoAmounts) {
			GameObject button_obj = Instantiate(original.gameObject);
			button_obj.transform.SetParent(content);
			button_obj.GetComponent<RectTransform>().localPosition = new Vector3(95, i * -100 -75);
			button_obj.GetComponentsInChildren<Text>() [0].text = pair.Value.ToString();
			button_obj.GetComponentsInChildren<Text>() [1].text = pair.Key.name;

			Button bt = button_obj.GetComponent<Button>();
			bt.onClick.AddListener(AmmoButtonClicked);

			ammos.Add(pair.Key);
			buttons[i] = bt;
			i++;
		}
		ammo_present = i != 0;
	}

	public void UpdateButtons () {
		if (!ammo_present) return;
		buttons [current_amm].GetComponentsInChildren<Text>() [0].text = Ship.Ammo.ToString();
	}

	public void AmmoButtonClicked() {
		byte num = 0x00;
		for (byte i=0; i < buttons.Length; i++) {
			Rect world_rect = GetWorldRect(buttons[i].GetComponent<RectTransform>());
			if (world_rect.Contains(Input.mousePosition)) {
				num = i;
			}
		}

		for (byte i=0; i < buttons.Length; i++) {
			buttons [i].GetComponent<Image>().color = i == num ? pressed : idle;
		}

		current_amm = num;
		Ship.CurrentAmmo = ammos [num];
	}

	public void ShowHide () {
		if (hidden) {
			transform.position = new Vector3(100, 313);
		} else {
			transform.position = new Vector3(-75, 313);
		}
		hidden = !hidden;
	}

	private void Update () {
		UpdateButtons();
	}

	/// <summary> Retrns the world rectangle </summary>
	/// <remarks> From the internet, but Hey: it works! </remarks>
     static public Rect GetWorldRect (RectTransform rt) {
         // Convert the rectangle to world corners and grab the top left
         Vector3[] corners = new Vector3[4];
         rt.GetWorldCorners(corners);
         Vector3 topLeft = corners[0];
 
         // Rescale the size appropriately based on the current Canvas scale
         Vector2 scaledSize = new Vector2(rt.rect.size.x, rt.rect.size.y);
 
         return new Rect(topLeft, scaledSize);
     }
}
