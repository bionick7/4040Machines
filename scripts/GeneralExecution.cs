using FileManagement;
using UnityEngine;
using NMS.OS;

/* ==========================================================
 * The general execution of a level, surveilling the
 * objectives and managing everything else, that is not bound
 * to a specific Object
 * ========================================================== */

public class GeneralExecution : MonoBehaviour {

	public bool ignore_player = true;
	
	public DataStructure eternal_data;
	public ConsoleBehaviour console;
	public OperatingSystem os;
	public Loader loader;

	public string battle_path;

	private GUIScript uiscript;
	private MapCore map_core;

	public MissionCore mission_core;

	private Camera ship_camera;
	private Camera map_camera;

	private bool in_map;
	public bool InMap {
		get { return in_map; }
		set {
			if (value) MapView();
			else NormalView();
			in_map = value;
		}
	}

	public static double TotalTime { get; private set; }

	/// <summary> Gets called from the Loader, before he is loading </summary>
	public void PreLoading() {
		loader = Globals.persistend.loader;
		os = new OperatingSystem(null, SceneGlobals.Player);
		mission_core = new MissionCore(console, loader);
	}

	private void NotLoadingRelated () {
		// What we can do right away
		SceneGlobals.Refresh();
		SceneGlobals.main_canvas = GameObject.Find("MainCanvas").GetComponent<Canvas>();
		SceneGlobals.map_canvas = GameObject.Find("MapCanvas").GetComponent<Canvas>();
		SceneGlobals.permanent_canvas = GameObject.Find("PermanentCanvas").GetComponent<Canvas>();
		SceneGlobals.map_drawer = Loader.EnsureComponent<MapDrawer>(gameObject);
		SceneGlobals.general = this;

		uiscript = Loader.EnsureComponent<GUIScript>(gameObject);
		map_core = Loader.EnsureComponent<MapCore>(gameObject);
		SceneGlobals.console = console = SceneGlobals.permanent_canvas.GetComponentInChildren<ConsoleBehaviour>();
		console.Start_();
		console.ConsolePos = ConsolePosition.lower;

		// Configure the cameras
		ship_camera = GameObject.Find("ShipCamera").GetComponent<Camera>();
		if (ship_camera == null) Debug.LogError("NO ShipCamera");
		ship_camera.farClipPlane = 1e9f;
		Loader.EnsureComponent<CameraMovement>(ship_camera.gameObject);
		map_camera = GameObject.Find("MapCamera").GetComponent<Camera>();
		SceneGlobals.ship_camera = ship_camera;
		SceneGlobals.map_camera = map_camera;

		// Initialize the audio source
		GameObject audiosource_object = new GameObject("Audio Source");
		audiosource_object.transform.SetParent(ship_camera.transform);
		Globals.audio = Loader.EnsureComponent<AudioManager>(audiosource_object);

		// Get the map view manager
		map_core.Start_();
		SceneGlobals.map_core = map_core;

		SceneGlobals.ui_script = uiscript;
	}

	/// <summary> Gets called on the beginning of the first frame after loading </summary>
	public void PostLoading() {
		InMap = true;
		SceneGlobals.Paused = true;
	}

	private void Awake () {
		NotLoadingRelated();
		if (!SceneGlobals.is_save) {
			PreLoading();
			loader.attack_data = new AttackData(Vector3.forward, new Vector3(0, 100, -200));
			loader.LoadEssentials();
			loader.LoadObjects();
			NextCommand();
		}
		PostLoading();

		Debug.Log(DeveloppmentTools.LogIterable(SceneObject.TotObjectList));
	}

	/// <summary> Switches to Map View </summary>
	private void MapView () {
		map_camera.enabled = true;
		map_camera.GetComponent<AudioListener>().enabled = true;
		ship_camera.enabled = false;
		ship_camera.GetComponent<AudioListener>().enabled = false;

		SceneGlobals.main_canvas.enabled = false;
		SceneGlobals.map_canvas.enabled = true;
	}

	/// <summary> Switches to Ship View </summary>
	private void NormalView () {
		ship_camera.enabled = true;
		ship_camera.GetComponent<AudioListener>().enabled = true;

		map_camera.enabled = false;
		map_camera.GetComponent<AudioListener>().enabled = false;
		
		SceneGlobals.main_canvas.enabled = true;
		SceneGlobals.map_canvas.enabled = false;
	}

	/// <summary> Requests the next command to be executed </summary>
	public void NextCommand () {
		mission_core.NextCommand();
	}

	/// <summary> Called if the game (un)pauses </summary>
	/// <param name="pause"> If the game pauses or unpauses </param>
	public void OnPause (bool pause) {
		foreach (Ship ship in SceneGlobals.ship_collection) {
			ship.OnPause(pause);
		}
		foreach (Missile missile in SceneGlobals.missile_collection) {
			missile.OnPause(pause);
		}
		foreach (Explosion explosion in SceneGlobals.explosion_collection) {
			explosion.OnPause(pause);
		}
	}

	private void Update () {
		os.Update();
		mission_core.Update();
		
		if (!uiscript.Paused) {
			SceneGlobals.physics_objects.RemoveWhere(x => !x.Exists);

			foreach (IPhysicsObject obj in SceneGlobals.physics_objects) {
				try { obj.PhysicsUpdate(Time.deltaTime); } catch (MissingReferenceException) {; }
			}
			
			SceneGlobals.explosion_collection.RemoveWhere(exp => !exp.exists);

			foreach (Explosion explosion in SceneGlobals.explosion_collection) {
				explosion.Update(Time.deltaTime);
			}

			TotalTime += Time.deltaTime;			
		}

		if (SceneGlobals.Player != null && !SceneGlobals.Player.Exists) {
			EndBattle(false);
		}
	}

	public void Save () {
		LoadSaving.Save();
	}

	/// <summary> If one Battle (Scene) has ended </summary>
	public void EndBattle (bool won) {
		if (won) {
			Globals.current_character.story_stage += Globals.progress_if_won;
			Globals.current_character.Save();
		} else {
			FileReader.FileLog("GameOver", FileLogType.story);
		}
		Globals.progress_if_won = 0;
		Globals.persistend.Back2Menu();
	}

	/// <summary> Can label things on GUI </summary>
	private void OnGUI () {
		// frames per second (FPS)
		GUI.Label(new Rect(0,   0, 100, 100), (1f / Time.deltaTime).ToString());
		GUI.Label(new Rect(0, 100, 100, 100), (Armor.thickness).ToString());
	}
}


