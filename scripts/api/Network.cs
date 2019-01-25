using System.Collections.Generic;
using FileManagement;
using UnityEngine;

/* ========================================================================
 * A Network allows the Vessels in the scene to communicate with each other
 * ======================================================================== */

public class Network : SceneObject, ISavable {

	private Dictionary<Target, List<uint>> assault_dict = new Dictionary<Target, List<uint>>();
	private Dictionary<uint, Target> tgt_dict = new Dictionary<uint, Target>();
	public uint fleetsize;
	public uint act_fleetsize = 0u;
	private Ship[] ship_arr;

	public bool Full {
		get { return ship_arr [(int) fleetsize - 1] != null; }
	}

	#region SceneObject requirements
	// Required by SceneObject
	public override string Name {
		get; protected set;
	}

	// Importance is required by SceneObject, but not needed here
	public override float Importance {
		get {
			DeveloppmentTools.Log("Network has no implementation for \"Importance\"");
			return 0;
		}
	}

	// Required by SceneObject
	public override double Mass {
		get { return 0; }
		protected set {
			DeveloppmentTools.Log("Cannot set mass of Network");
		}
	}
	#endregion

	// Indexer
	public Ship this[uint index] {
		get { return ship_arr [(int) index]; }
		set { ship_arr [(int) index] = value; }
	}

	public Network(uint size, bool p_friendly, string name, int id=-1) : base(SceneObjectType.network, id){
		Friendly = p_friendly;
		fleetsize = size;
		ship_arr = new Ship[(int)size];
		Name = string.Format("\"{0}\"-Network", name);
	}

	/// <summary> Should be called from a AI agent, if it decides to switch targets </summary>
	/// <param name="agentID"> The ID of the AI agent </param>
	/// <param name="tgt"> The target, which it chooses </param>
	public void Switch_Target(uint agentID, Target tgt){
		assault_dict[tgt_dict[agentID]].Remove(agentID);
		tgt_dict[agentID] = tgt;
		if (assault_dict.ContainsKey(tgt)){
			assault_dict[tgt].Add(agentID);
		}
		else {
			assault_dict.Add(tgt, new List<uint>(){agentID});
		}
	}

	/// <summary> Should by called by a ship, to register a new AI agent </summary>
	/// <param name="ship"> The ship in question </param>
	/// <param name="ship_target"> The current target of the ship </param>
	/// <returns> The ID of the agent </returns>
	public uint AddAgent(Ship ship, Target ship_target){
		uint id = act_fleetsize++;
		ship_arr[(int) id] = ship;
		if (assault_dict.ContainsKey(ship_target)){
			assault_dict[ship_target].Add(id);
		} else{
			assault_dict[ship_target] = new List<uint> () {id};
		}
		tgt_dict[id] = ship_target;
		return id;
	}

	/// <summary> Gets the strength ration for a given target </summary>
	/// <param name="tgt"> The ennemy target to get the strength ration</param>
	/// <returns> 
	///		Ratio allied strength/ ennemy strength 
	///		Returns 0, if ennemy target is not beeing attacked
	///	</returns>
	public float Strength_Ratio(Target tgt){
		if (tgt.virt_ship){return 100f;}
		if (!assault_dict.ContainsKey(tgt)){return 0f;}
		float ally_forces = 0f;
		//Debug.Log(assault_dict [tgt].Count);
		foreach (uint ally_agent in assault_dict[tgt]){
			ally_forces += ship_arr[ally_agent].Strength;
		}
		return ally_forces/tgt.Ship.Strength;
	}

	public override void PhysicsUpdate (float p_deltatime) {
		// Don't need to wast time here
		return;
	}

	public override DataStructure Save (DataStructure ds) {
		// -----------------------------------
		// Do saving stuff here, if necessary
		// -----------------------------------
		return ds;
	}

	public static Network Rogue_0 = new Network(100000u, true, "friendly rogue", 1);
	public static Network Rogue_1 = new Network(100000u, false, "hostile rogue", 2);
}

/// <summary>
///		A Vector3 with float "importance".
/// </summary>
public struct FuzzyVector {
	public Vector3 vector;
	public float importance;

	/// <summary> Other way of writing "FuzzyVector(Vector3.zero, 0)" </summary>
	public static readonly FuzzyVector zero = new FuzzyVector(Vector3.zero, 0f);

	/// <param name="vec"> The vector </param>
	/// <param name="imp">The float between [0, 1]</param>
	public FuzzyVector(Vector3 vec, float imp){
		vector = vec;
		importance = imp;
	}

	public static FuzzyVector operator* (FuzzyVector Fvec, float a){
		return new FuzzyVector(Fvec.vector * a, Fvec.importance);
	}

	public static implicit operator Vector3 (FuzzyVector vec) {
		return vec.vector;
	}
}

/// <summary>
///		A bunch of usefull tools
/// </summary>
public struct HandyTools
{
	/// <summary>
	///		Returns a random Vector with all components between
	///		-1 and 1.
	/// </summary>
	public static Vector3 RandomVector {
		get { return new Vector3(Random.Range(-1, 1), Random.Range(-1, 1), Random.Range(-1, 1)); }
	}

	public static float AngleAround (Quaternion rotation, Vector3 axis) {
		Vector3 start = Vector3.right;
		Vector3 end = rotation * Vector3.right;
		return Vector3.Angle(Vector3.ProjectOnPlane(start, axis), Vector3.ProjectOnPlane(end, axis));
	}

	public static Vector3 VecAbs (Vector3 vec) {
		return new Vector3(Mathf.Abs(vec.x), Mathf.Abs(vec.y), Mathf.Abs(vec.z));
	}
}