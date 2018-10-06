using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/* ===================================================================
 * Contains the Data used through the game as static fields
 * All of these are unique and ment to be accessed quickly by anything
 * ===================================================================*/

/// <summary>
///		The general data
/// </summary>
public static class Data
{
	public static DataStructure persistend_data;
	public static DataStructure settings;
	public static DataStructure player_data;
	public static DataStructure parts;
	public static DataStructure battle_list;
	public static DataStructure premade_ships;
	public static DataStructure ammunition;

	public static PersistendObject persistend;

	public static Dictionary<string, Ammunition> ammunition_insts = new Dictionary<string, Ammunition>();
	public static List<Character> characters = new List<Character>();

	public static NMS.OS.OperatingSystem current_os;
	public static Chapter loaded_chapter;
}

/// <summary>
///		The data of the currently loaded scene
/// </summary>
public static class SceneData
{
	public static bool in_console;

	public static GUIScript ui_script;
	public static Ship Player;
	public static ConsoleBehaviour console;
	public static GeneralExecution general;
	public static Canvas canvas;
	public static Canvas map_canvas;

	public static Camera ship_camera;
	public static Camera map_camera;
	public static MapDrawer mapdrawer;

	public static HashSet<Ship> ship_list = new HashSet<Ship>();
	public static HashSet<Missile> missile_list = new HashSet<Missile>();
	public static HashSet<Bullet> bullet_list = new HashSet<Bullet>();

	public static HashSet<IPhysicsObject> physics_objects = new HashSet<IPhysicsObject>();

	public static bool PlayerSide {
		get { return Player.side; }
	}
}