using UnityEngine;
using System.Collections.Generic;
using FileManagement;

/*
 * This is a library containing all the non-behavior classes, structs and interfaces
 * List of fuctiuons:
 *		-IInteractible:	Base interface for movable things
 *		-Squadron:		Representation of squadrons in the level editor
 *		-EDShip:		Representation of single ships in the level editor
 *		-EDTarget:		Representation of single targets in teh level editor
 * */

/// <summary>
///		Base interface for movable things
/// </summary>
public interface IInteractable
{
	Vector3 Rotation { get; set; }
	Vector3 Position { get; set; }
	Vector3 Velocity { get; set; }
	Vector3 AngularVelocity { get; set; }
}

/// <summary>
///		Representation of squadrons in the level editor
/// </summary>
public struct Squadron
{
	public EDShip leader;
	public List<EDShip> ships;
	public string name;
	public bool friendly;
	public Color color;

	public static readonly Squadron default_squadron = new Squadron("default", true);

	/// <param name="pname"> The squadron's name </param>
	/// <param name="pfriendly"> True, if the squadron is friendly to the player squadron </param>
	public Squadron (string pname, bool pfriendly) {
		ships = new List<EDShip>();
		leader = null;
		name = pname;
		friendly = pfriendly;
		if (pname == "default") color = Color.white;
		else color = Random.ColorHSV(0, 1, .5f, 1, .0f, 1, 1, 1);
	}

	/// <summary> Changes the leader ship </summary>
	/// <param name="ship"> The new leader </param>
	public void ChangeLeader (EDShip ship) {
		if (ship == leader) return;
		if (ships.Contains(ship)) {
			ships.Remove(ship);
			if (leader != null) 	ships.Add(leader);
			leader = ship;
		}
	}

	/// <summary> Adds a ship to the squadron </summary>
	/// <param name="ship"> The ship in question </param>
	public void Add(EDShip ship) {
		ships.Add(ship);
	}

	/// <summary> Removes a ship from the squadron </summary>
	/// <param name="ship"> The ship in question </param>
	public void Remove(EDShip ship) {
		ships.Remove(ship);
	}

	/// <summary> Returns the datastructure of the squadron </summary>
	public DataStructure GetDS () {
		DataStructure ds = new DataStructure();
		ds.Set("name", name);
		ds.Set("friendly", friendly);

		if (leader == null) {
			ds.Set("leader", "NULL");
		} else {
			ds.Set("leader", leader.name);
			ds.Set("leader rot", leader.Rotation);
			ds.Set("leader pos", leader.Position);
		}

		EDShip[] ships_arr = ships.ToArray();
		ds.Set("ships", System.Array.ConvertAll(ships_arr, s => s.name));
		ds.Set("orientations", System.Array.ConvertAll(ships_arr, s => s.Rotation));
		ds.Set("positions", System.Array.ConvertAll(ships_arr, s => s.Position));

		return ds;
	}

	public override string ToString () {
		return string.Format("<Squad {0}>", name);
	}

	public static bool operator==(Squadron lhs, Squadron rhs) {
		return lhs.name == rhs.name;
	}

	public static bool operator!=(Squadron lhs, Squadron rhs) {
		return !(lhs == rhs);
	}
}

/// <summary>
///		Representation of single ships in the level editor
/// </summary>
public class EDShip : IInteractable
{
	public Vector3 Rotation { get; set; }
	public Vector3 Position { get; set; }
	public Vector3 Velocity { get; set; }
	public Vector3 AngularVelocity { get; set; }
	public string name;

	/// <summary> True, if the ship is the leader of it's squadron </summary>
	public bool IsLeader {
		get { return _squad.leader != null && _squad.leader == this; }
	}

	/// <summary> True, if the ship is the player </summary>
	public bool IsPlayer {
		get { return EditorGeneral.Player == this; }
		set { if (value) EditorGeneral.Player = this; }
	}

	private Squadron _squad;
	/// <summary> The squadron, the ship belongs to </summary>
	public Squadron Squad {
		get { return _squad; }
		set {
			if (_squad == value) return;
			if (IsLeader) {
				_squad.leader = null;
			} else _squad.Remove(this);
			value.Add(this);
			_squad = value;
		}
	}

	/// <param name="pname"> The ships name </param>
	/// <param name="pposition"> The initial position of the ship </param>
	/// <param name="protation"> The initial rotation of the ship</param>
	public EDShip(string pname, Vector3 pposition, Vector3 protation) {
		name = pname;
		Position = pposition;
		Rotation = protation;
		_squad = Squadron.default_squadron;
	}

	/// <summary> Assign a squad to the ship, without going throug the other associated mechanisms </summary>
	/// <param name="squad"> New squad </param>
	/// <remarks> Do only use, when necessary! </remarks>
	public void AssignSilent(Squadron squad) {
		_squad = squad;
	}

}

/// <summary>
///		Representation of single targets in teh level editor
/// </summary>
public struct EDTarget : IInteractable
{
	public Vector3 Rotation { get; set; }
	public Vector3 Position { get; set; }
	public Vector3 Velocity { get; set; }
	public Vector3 AngularVelocity { get; set; }
	public string name;
	public bool friendly;
	public float hp;
	public double mass;
	public DSPrefab pref;
	private bool is_none;

	public static readonly EDTarget none = new EDTarget() { is_none = true };

	public bool Exists {
		get { return !is_none; }
	}

	/// <param name="pname"> the name of the target</param>
	/// <param name="pfriendly"> if the target is friendly </param>
	/// <param name="ppref"> The prefab of the target (DataStructure prefab) </param>
	public EDTarget(string pname, bool pfriendly, DSPrefab ppref) {
		name = pname;
		friendly = pfriendly;
		Position = Rotation = Velocity = AngularVelocity = Vector3.zero;
		hp = 0f; mass = 0d;
		pref = ppref;
		is_none = false;
	}

	/// <summary> Returns the datastructure of the squadron </summary>
	public DataStructure GetDS () {
		DataStructure ds = new DataStructure();
		ds.Set("hp", hp);
		ds.Set("mass", mass);
		ds.Set("name", name);
		ds.Set("friendly", friendly);
		ds.Set("position", Position);
		ds.Set("rotation", Rotation);
		ds.Set("velocity", Velocity);
		ds.Set("angular velocity", AngularVelocity);
		ds.Set("object", pref);
		return ds;
	}
}
