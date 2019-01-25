using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using FileManagement;

[RequireComponent(typeof(MapDrawer))]
public class CampagneManager : MonoBehaviour
{
	public static MapDrawer drawer;
	public static CampagneManager active;
	public static CameraBehaviour cam;
	public static CelestialData planet_view;
	public static CelestialData planet_hover;
	public static Dictionary<string, string> occupation_data = new Dictionary<string, string>();
	public static Dictionary<string, List<ChapterBattle>> battle_data = new Dictionary<string, List<ChapterBattle>>();
	public static List<IChapterEvent> constant_events = new List<IChapterEvent>();
	public static HashSet<Celestial> celestials = new HashSet<Celestial>();

	public Button back_button;

	private GraphicRaycaster raycaster;

	private ConsoleBehaviour console;

	private void Awake () {
		active = this;
		drawer = GetComponent<MapDrawer>();
		cam = FindObjectOfType<CameraBehaviour>();
		raycaster = FindObjectOfType<GraphicRaycaster>();
		console = FindObjectOfType<ConsoleBehaviour>();

		planet_view = CelestialData.None;
		planet_hover = CelestialData.None;
		celestials.Clear();

		UpdateChapter();
	}
	
	public void UpdateChapter () {
		occupation_data.Clear();
		battle_data.Clear();
		constant_events.Clear();
		foreach (DataStructure child in Globals.nation_information.AllChildren) {
			if (child.Contains<string []>("start celestials")) {
				foreach (string celestial in child.Get<string []>("start celestials")) {
					occupation_data.Add(celestial, child.Name);
				}
			}
		}
		back_button = GameObject.Find("Back").GetComponent<Button>();

		Chapter current_chapter = Globals.loaded_chapter;
		foreach (IChapterEvent event_ in current_chapter.all_events) {
			if (System.Array.Exists(event_.AviableOn, x => x == Globals.current_character.story_stage)) {
				if (event_ is ChapterBattle) {
					var battle = (ChapterBattle) event_;
					if (battle_data.ContainsKey(battle.planet_name)) {
						battle_data [battle.planet_name].Add(battle);
					} else {
						battle_data.Add(battle.planet_name, new List<ChapterBattle>() { battle });
					}
				} else {
					constant_events.Add(event_);
				}
			}
		}
		
		foreach (Celestial cel in celestials) {
			cel.ChapterUpdate();
		}
	}

	private void Update () {
		Celestial selected = null;
		foreach (Celestial cel in celestials) {
			if (cel.mouse_hover) {
				if (selected == null || cel.click_distance < selected.click_distance) {
					selected = cel;
				}
			}
		}
		if (selected == null) planet_hover = CelestialData.None;
		else {
			planet_hover = selected.data;
			if (Input.GetMouseButtonDown(0) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) {
				selected.in_focus = true;
				cam.Stick(selected);
				PlanetInformation.Active.UpdateLabels(selected.data);
				NatonInformation.Active.UpdateLabels();
			}
		}
		back_button.enabled = !planet_view.none;
	}

	public void Back2Menu () {
		Globals.persistend.Back2Menu();
	}

	public void Engage (IChapterEvent @event) {
		if (@event is ChapterBattle) {
			var b = (ChapterBattle) @event;
			Globals.progress_if_won = b.progress;
			Globals.persistend.LoadBattle(b.own_data, DataStructure.GeneralPath + b.path + ".cfgt");
		} else if (@event is ChapterConversation) {
			var c = (ChapterConversation) @event;
			console.ConsolePos = ConsolePosition.shown;
			Globals.progress_if_won = c.progress;
			new Conversation(c.own_data, console) { Running = true };
		} else if (@event is ChapterJump) {
			var j = (ChapterJump) @event;
			string bef_chapter_name = Globals.current_character.chapter;
			Globals.current_character.chapter = j.new_chapter;
			Globals.loaded_chapter = Globals.current_character.LoadedChapter;
			if (Globals.loaded_chapter == Chapter.Empty) {
				Globals.current_character.chapter = bef_chapter_name;
				Globals.loaded_chapter = Globals.current_character.LoadedChapter;
			} else {
				Globals.current_character.story_stage = 0;
				Globals.current_character.Save();
			}
			UpdateChapter();
		}
	}

	public void ExitConversation () {
		Globals.current_character.story_stage += Globals.progress_if_won;
		Globals.progress_if_won = 0;
		Globals.current_character.Save();
		UpdateChapter();
	}
}
