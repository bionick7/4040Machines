using System.Collections.Generic;
using UnityEngine;

public class Network : SceneObject {

	private Dictionary<Target, List<uint>> assault_dict = new Dictionary<Target, List<uint>>();
	private Dictionary<uint, Target> tgt_dict = new Dictionary<uint, Target>();
	public uint fleetsize;
	public uint act_fleetsize = 0u;
	private Ship[] ship_arr;

	public bool Full {
		get {
			return ship_arr [(int) fleetsize - 1] != null;
		}
	}

	public override float Importance {
		get {
			throw new System.NotImplementedException();
		}
	}

	public override double Mass {
		get {
			return 0;
		}
		set {
			throw new System.NotImplementedException();
		}
	}

	// Indexer
	public Ship this[uint index] {
		get { return ship_arr [(int) index]; }
		set { ship_arr [(int) index] = value; }
	}

	public Network(uint size, bool side_) : base(SceneObjectType.network){
		side = side_;
		fleetsize = size;
		ship_arr = new Ship[(int)size];
	}

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

	public uint AddShip(Ship ship, Target ship_target){
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

	public static Network Rogue_0 = new Network(100000u, true);
	public static Network Rogue_1 = new Network(100000u, false);
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

public struct HandyTools
{
	public static Vector3 CutVector (Vector3 vec, float min=-1, float max=1) {
		return new Vector3(Mathf.Min(Mathf.Max(vec.x, min), max),
						   Mathf.Min(Mathf.Max(vec.y, min), max),
						   Mathf.Min(Mathf.Max(vec.z, min), max));
	}

	public static Vector3 RandomVector {
		get {
			return new Vector3(Random.Range(-1, 1), Random.Range(-1, 1), Random.Range(-1, 1));
		}
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