using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using FileManagement;

public class LoadingObject : MonoBehaviour {
	
	private Slider loading_bar;

	private const string music_path = "GameData/Audio/music";
	private const string sound_path = "GameData/Audio/sfx";

	private bool music_loaded;
	private bool sound_loaded;

	private DataStructure persistend_data;

	private int loading_stuff;
	private int LoadingStuff {
		get { return loading_stuff; }
		set {
			//print(value > loading_stuff ? "inc" : "dec");
			loading_stuff = value;
		}
	}

	private void Start () {
		loading_bar = FindObjectOfType<Slider>();
		Globals.loaded_data = new SoundData(true);
		
		FilePreparartion();
		LoadSounds();
		LoadMusic();
		LoadPlugins();
		_Update();
		LoadAdditionalStuff();
	}

	private void Update () {
		if (loading_stuff == 0)
			EndLoading();
	}

	private void FilePreparartion () {
		// Debug Logging
		string[] dir_split = Application.dataPath.Split('/');
		string last_path = dir_split[dir_split.Length - 1];
		FileReader.logfile = Application.dataPath.Substring(0, Application.dataPath.Length - last_path.Length) + "logfile.txt";
		System.IO.File.WriteAllText(FileReader.logfile, "New Game");

		// Specify path
		Globals.config_path = Application.dataPath.Substring(0, Application.dataPath.Length - last_path.Length) + "configs/";
		Globals.plugin_path = Application.dataPath.Substring(0, Application.dataPath.Length - last_path.Length) + "plugins/";

		Globals.defaults = DataStructure.Load("DefaultValues", "Defaults");
	}

	/// <summary> Called to update data </summary>
	private void _Update () {
		Globals.persistend_data = persistend_data = new DataStructure("persistend_data");

		Globals.settings = DataStructure.Load("saved/settings", "settings", persistend_data);
		Globals.bindings = new KeyBindingCollection("saved/keybindings");
		Globals.parts = DataStructure.LoadFromDir("parts", "parts", persistend_data);
		Globals.premade_ships = DataStructure.Load("ships/premade_ships", "premade", persistend_data);
		Globals.planet_information = DataStructure.Load("campagne/nations/celestials", "Planets", persistend_data);
		Globals.nation_information = DataStructure.Load("campagne/nations/nations", "Nations", persistend_data);

		Globals.impact_textures = new ImpactTextures("GameData/Textures/impact_textures");
		Globals.selector_data = new SelectorData("GameData/Textures/selector_sprite_data");

		var battle_list = new DataStructure("battles", persistend_data);
		foreach (string battlepath in FileReader.AllFileNamesInDir(DataStructure.GeneralPath + "battles")) {
			string[] bpsplit = battlepath.Split('/');
			string battlename = bpsplit[bpsplit.Length - 1];
			battlename = battlename.Substring(0, battlename.Length - 5);
			var ds = new DataStructure(battlename, battle_list);
			ds.Set("path", battlepath);
		}
		Globals.battle_list = battle_list;
		Globals.ammunition = DataStructure.LoadFromDir("ammunition", "ammunition", persistend_data);
	}

	private void LoadAdditionalStuff () {
		// Load ammo
		foreach (Ammunition ammo in Loader.LoadAmmo(Globals.ammunition)) {
			if (!ammo.IsNone) {
				Globals.ammunition_insts.Add(ammo.name, ammo);
			}
		}

		// Load character and chapter
		Globals.characters = Loader.Load_Characters(DataStructure.GeneralPath + "saved/characters");
		Globals.current_character = Globals.characters[0];
		Globals.loaded_chapter = Globals.current_character.LoadedChapter;
	}

	private void LoadSounds () {
		DataStructure sfx_ds = DataStructure.Load(sound_path);

		DataStructure ui_ds = sfx_ds.GetChild("UI");
		DataStructure weapons_ds = sfx_ds.GetChild("Weapons");
		DataStructure computer_ds = sfx_ds.GetChild("Computer");

		string ui_path = ui_ds.Get<string>("dir");
		foreach (KeyValuePair<string, string> pair in ui_ds.strings) {
			if (pair.Key != "dir") {
				LoadingStuff++;
				StartCoroutine(LoadSoundCoroutine(string.Format("GameData/Audio/{0}/{1}", ui_path, pair.Value), pair.Key, SFXType.ui));
			}
		}

		string weapon_path = weapons_ds.Get<string>("dir");
		foreach (KeyValuePair<string, string> pair in weapons_ds.strings) {
			if (pair.Key != "dir") {
				LoadingStuff++;
				StartCoroutine(LoadSoundCoroutine(string.Format("GameData/Audio/{0}/{1}", weapon_path, pair.Value), pair.Key, SFXType.weapon));
			}
		}

		string computer_path = computer_ds.Get<string>("dir");
		foreach (KeyValuePair<string, string> pair in computer_ds.strings) {
			if (pair.Key != "dir") {
				LoadingStuff++;
				StartCoroutine(LoadSoundCoroutine(string.Format("GameData/Audio/{0}/{1}", computer_path, pair.Value), pair.Key, SFXType.computer));
			}
		}

		StartCoroutine(LoadSoundCoroutine("default_sound.wav", "default", SFXType.default_));
		LoadingStuff++;
	}

	private void LoadPlugins () {
		Globals.plugins = new PluginHandling();
		Globals.plugins.Load();
		Globals.plugins.LoadShipParts();
	}

	private IEnumerator LoadSoundCoroutine (string path, string name, SFXType type) {
		string url = string.Format("file://{0}{1}", DataStructure.GeneralPath, path);
		if (System.IO.File.Exists(DataStructure.GeneralPath + path)) {
			WWW www = new WWW(url);
			yield return www;

			AudioClip clip = www.GetAudioClip(false, false);
			clip.name = name;

			switch (type) {
			case SFXType.ui:
				Globals.loaded_data.ui_sounds.Add(name, clip);
				break;
			case SFXType.weapon:
				Globals.loaded_data.weapon_sounds.Add(name, clip);
				break;
			case SFXType.computer:
				Globals.loaded_data.computer_sounds.Add(name, clip);
				break;
			case SFXType.rcs:
				Globals.loaded_data.rcs_sound = clip;
				break;
			case SFXType.exoplosion:
				Globals.loaded_data.explosion_sound = clip;
				break;
			default:
			case SFXType.default_:
				Globals.loaded_data.placeholder_sound = clip;
				break;
			}
			LoadingStuff--;
		} else {
			Debug.LogErrorFormat("File not found: {0}", DataStructure.GeneralPath + path);
		}
	}

	private void LoadMusic () {
		DataStructure data = DataStructure.Load(music_path);
		foreach (DataStructure child in data.AllChildren) {
			StartCoroutine(LoadMusicCoroutine(child.Get("music", new string [] { "default_sound.waw" }), child.Name));
			LoadingStuff++;
		}
	}

	private IEnumerator LoadMusicCoroutine (string[] paths, string name) {
		List<AudioClip> clips = new List<AudioClip>();
		foreach (string path in paths) {
			string url = string.Format("file://{0}{1}", DataStructure.GeneralPath, path);
			if (System.IO.File.Exists(DataStructure.GeneralPath + path)) {
				WWW www = new WWW(url);
				yield return www;

				AudioClip clip = www.GetAudioClip(false, false);
				clip.name = name;
				clips.Add(clip);
				music_loaded = true;
			} else {
				Debug.LogErrorFormat("File not found: {0}", DataStructure.GeneralPath + path);
			}
		}
		Globals.loaded_data.music_dict.Add(name, clips.ToArray());
		LoadingStuff--;
	}

	private void EndLoading () {
		SceneManager.LoadScene(sceneName: "menu");
	}

	enum SFXType
	{
		ui,
		weapon,
		computer,
		rcs,
		exoplosion,
		default_,
	}
}

public struct SoundData
{
	public Dictionary<string, AudioClip[]> music_dict;
	public Dictionary<string, AudioClip> ui_sounds;
	public Dictionary<string, AudioClip> weapon_sounds;
	public Dictionary<string, AudioClip> computer_sounds;
	public AudioClip rcs_sound;
	public AudioClip explosion_sound;
	public AudioClip placeholder_sound;

	public SoundData(bool confirm) {
		music_dict = new Dictionary<string, AudioClip[]>();
		ui_sounds = new Dictionary<string, AudioClip>();
		weapon_sounds = new Dictionary<string, AudioClip>();
		computer_sounds = new Dictionary<string, AudioClip>();
		rcs_sound = null;
		explosion_sound = null;
		placeholder_sound = null;
	}
}