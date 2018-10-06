using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class AmmoChooser : MonoBehaviour {

	public Button original;

	private Ship ship;
	private Transform content;

	private List<Ammunition> ammos = new List<Ammunition>();
	private Button[] buttons;
	private byte current_amm = 0x00;
	private bool ammo_present = true;

	private static readonly Color pressed = new Color(0xc9/255f, 0x6a/255f, 0x51/255f);
	private static readonly Color idle = new Color(0xb4/255f, 0xb7/255f, 0x9f/255f);

	private void Start () {
		ship = SceneData.Player;
		content = transform.GetChild(0).GetChild(0);

		SpawnButtons();
		if (ammo_present) {
			AmmoButtonClicked();
		}
	}

	private void SpawnButtons() {
		byte i = 0x00;

		int ammo_num = ship.AmmoAmounts.Count;
		buttons = new Button [ammo_num];

		content.GetComponent<RectTransform>().sizeDelta = new Vector3(200, 100 * ammo_num + 20);

		foreach (KeyValuePair<Ammunition, uint> pair in ship.AmmoAmounts) {
			GameObject button_obj = Instantiate(original.gameObject);
			button_obj.transform.SetParent(content);
			button_obj.transform.position = new Vector3(100, i * -100);
			button_obj.GetComponentsInChildren<Text>() [0].text = pair.Value.ToString();
			button_obj.GetComponentsInChildren<Text>() [1].text = pair.Key.name;
			Button bt = button_obj.GetComponent<Button>();
			ammos.Add(pair.Key);
			buttons[i] = bt;
			i++;
		}
		ammo_present = i != 0;
	}

	public void UpdateButtons () {
		if (!ammo_present) return;
		buttons [current_amm].GetComponentsInChildren<Text>() [0].text = ship.Ammo.ToString();
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
		ship.CurrentAmmo = ammos [num];
	}

	private void Update () {
		UpdateButtons();
	}

	/// <summary>
	///		Retrns the world rectangle
	/// </summary>
	/// <remarks> From the internet, but Hey! </remarks>
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
