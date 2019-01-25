using System.Collections.Generic;
using FileManagement;
using UnityEngine;

/* ===================================================================
 * Contains the Data used through the game as static fields
 * All of these are unique and ment to be accessed quickly by anything
 * ===================================================================*/

/// <summary>
///		The general data
/// </summary>
public static class Globals
{
	public static string config_path;
	public static string plugin_path;

	public static SoundData loaded_data;
	public static PluginHandling plugins;

	public static DataStructure persistend_data;
	public static DataStructure settings;
	public static DataStructure player_data;
	public static DataStructure parts;
	public static DataStructure battle_list;
	public static DataStructure premade_ships;
	public static DataStructure ammunition;
	public static DataStructure planet_information;
	public static DataStructure nation_information;
	public static DataStructure defaults;

	public static ImpactTextures impact_textures;
	public static SelectorData selector_data;

	public static AudioManager audio;
	public static MusicPlayer music_player = null;

	public static PersistendObject persistend;
	public static SoundCollection soundcollection;
	public static KeyBindingCollection bindings;

	public static Dictionary<string, Ammunition> ammunition_insts = new Dictionary<string, Ammunition>();
	public static List<Character> characters = new List<Character>();
	public static Character current_character;

	public static NMS.OS.OperatingSystem current_os;
	public static Chapter loaded_chapter;

	public static ushort progress_if_won = 0;
}


/// <summary>
///		The global variables of the currently loaded scene
/// </summary>
public static class SceneGlobals
{
	public static bool in_console;
	public static bool is_save;

	public static GUIScript ui_script;
	public static ConsoleBehaviour console;
	public static GeneralExecution general;
	public static Canvas permanent_canvas;
	public static Canvas main_canvas;
	public static Canvas map_canvas;

	public static Camera ship_camera;
	public static Camera map_camera;
	public static MapDrawer map_drawer;
	public static MapCore map_core;

	public static HashSet<Ship> ship_collection = new HashSet<Ship>();
	public static HashSet<Missile> missile_collection = new HashSet<Missile>();
	public static HashSet<Bullet> bullet_collection = new HashSet<Bullet>();
	public static HashSet<Explosion> explosion_collection = new HashSet<Explosion>();

	public static HashSet<IPhysicsObject> physics_objects = new HashSet<IPhysicsObject>();
	public static HashSet<DestroyableTarget> destroyables = new HashSet<DestroyableTarget>();

	public static float velocity_multiplyer = .5f;
	public static float acceleration_multiplyer = 0.5f;

	public static int battlefiled_size;

	public static bool Paused {
		get { return ui_script.Paused; }
		set { ui_script.Paused = value; }
	}

	public static ReferenceSystem ReferenceSystem {
		get { return map_core.CurrentSystem; }
		set { map_core.CurrentSystem = value; }
	}

	private static Ship _player = null;
	public static Ship Player {
		get { return _player; }
		set {
			value.control_script.SetAsPlayer();
			Loader.EnsureComponent<CameraMovement>(ship_camera.gameObject).ChangeControl(value);
			ui_script.ResetPlayer(value);
			_player = value;
		}
	}

	public static void Refresh() {
		ship_collection.Clear();
		missile_collection.Clear();
		bullet_collection.Clear();
		explosion_collection.Clear();
		physics_objects.Clear();
		destroyables.Clear();
		SceneObject.TotObjectList.Clear();

		_player = null;
	}
}

