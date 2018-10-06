using UnityEngine.UI;
using UnityEngine;

public class SettingsBehaviour : MonoBehaviour {

	public Slider total_vol;
	public Slider music_vol;
	public Slider UI_vol;
	public Slider spacecraft_vol;
	public Switch fullscreen;
	public Switch[] quality;

	private RectTransform rect;
	private DataStructure settings;

	private ushort curr_quality;
	private bool _shown;
	public bool Shown {
		get {
			return _shown;
		}
		set {
			if (value) {
				Init();
			} else {
				Exit();
			}
			_shown = value;
		}
	}

	private void Start () {
		rect = GetComponent<RectTransform>();
		settings = Data.settings;
	}

	private void UpdateSettings () {
		DataStructure volumes = settings.GetChild("sound volume");
		total_vol.value = volumes.Get<float>("total");
		music_vol.value = volumes.Get<float>("music");
		UI_vol.value = volumes.Get<float>("UIsound");
		spacecraft_vol.value = volumes.Get<float>("spacecraft");

		DataStructure graphics = settings.GetChild("graphics");
		fullscreen.On = graphics.Get<bool>("fullscreen");
		curr_quality = graphics.Get<ushort>("graphics");
		for (int i=0; i < quality.Length; i++) {
			quality [i].TriggerQuiet(i == curr_quality);
		}
	}

	private void UpdateFile () {
		DataStructure volumes = settings.GetChild("sound volume");
		volumes.Set("total", total_vol.value);
		volumes.Set("music", music_vol.value);
		volumes.Set("UIsound", UI_vol.value);
		volumes.Set("spacecraft", spacecraft_vol.value);

		DataStructure graphics = settings.GetChild("graphics");
		graphics.Set("fullscreen", fullscreen.On);
		graphics.Set("graphics", curr_quality);

		settings.Save("saved/settings.txt");
	}

	private void Update () {
		bool changed = false;
		ushort new_q = 0;
		for (int i=0; i < quality.Length; i++) {
			if (quality[i].On && i != curr_quality) {
				changed = true;
				new_q = (ushort) i;
			}
		}
		curr_quality = new_q;
		if (!changed) { return; }
		for (int i=0; i < quality.Length; i++) {
			if (quality[i].On && i != curr_quality) {
				quality [i].TriggerQuiet(false);
			}
		}
	}

	private void Init () {
		rect.position = new Vector3(300, Screen.height / 2 - 25);
		UpdateSettings();
	}

	private void Exit () {
		rect.position = new Vector3(-500, 0);
		UpdateFile();
	}

}
