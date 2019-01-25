using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MissionShower : Retractable
{
	public RectTransform content;
	public Button button;

	public IChapterEvent[] events = new IChapterEvent[0];
	public static IChapterEvent current = ChapterBattle.None;

	public List<Button> button_list = new List<Button>();

	private void Start () {
		SetVariables(.5f, transform.position, transform.position - new Vector3(90, 0));
	}

	private new void Update () {
		IChapterEvent[] new_events = new IChapterEvent[CampagneManager.planet_view.battles.Length + CampagneManager.constant_events.Count];
		CampagneManager.planet_view.battles.CopyTo(new_events, 0);
		CampagneManager.constant_events.ToArray().CopyTo(new_events, CampagneManager.planet_view.battles.Length);

		bool equal = true;
		if (new_events.Length != events.Length) equal = false;
		else {
			for (int i = 0; i < events.Length; i++) {
				if (new_events [i].Name != events [i].Name) equal = false;
			}
		}

		if (!equal) {
			UpdateLabels(new_events);
		}
		events = new_events;
		Shown = events.Length > 0;
		base.Update();
	}

	private void UpdateLabels (IChapterEvent[] events) {
		foreach (Button bt in button_list) {
			Destroy(bt.gameObject);
		}
		button_list.Clear();

		for (int i=0; i < events.Length; i++) {
			IChapterEvent @event = events [i];
			GameObject button_obj = Instantiate(button.gameObject);
			button_obj.transform.SetParent(content);
			button_obj.transform.position = content.position + new Vector3(-100, content.rect.height - 50 * i);

			Button button_inst = Loader.EnsureComponent<Button>(button_obj);
			button_obj.GetComponentInChildren<Text>().text = @event.Name;
			button_inst.onClick.AddListener(Clicked);

			Image button_img = button_inst.image;
			if (@event is ChapterBattle) {
				button_img.color = Color.red;
			}
			else if (@event is ChapterBattle) {
				button_img.color = Color.green;
			}

			button_list.Add(button_inst);
		}
	}

	public void Clicked () {
		ushort event_num = (ushort) Mathf.Floor((Input.mousePosition.y - content.position.y - content.rect.height) / -50);
		current = events [event_num];
		CampagneManager.active.Engage(current);
	}
}
