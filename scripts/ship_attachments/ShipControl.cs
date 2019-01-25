using System.Collections.Generic;
using UnityEngine;
using FileManagement;

/* ==========================================================================================================
 * This script is ment to go on every active ship, regardless if it is controlled by a player or by AI.
 * It controlls the thrust, maneuvering, the weaponnery and the missiles of a ship and gives Data for the UI.
 * All effects of dammage or losing of other parts are managed here
 * ========================================================================================================== */

public class ShipControl : MonoBehaviour {

    //PUBLIC VARIABLES
    //------------------

    //Propulsion
	public float thrust = 5.0f;			// in kN
    public float RCS_ISP = 500;			// in s

	//These are turrets, like on bigger ships
	/// <summary> Dictionnary, containing the name of one weapontype and the array of GameObjects representing the weapons</summary>
	public Dictionary<string, Turret []> turrets;

	//Rcs Strength
	/// <remarks> kN </remarks>
	public Vector3 [] trans_strength = new Vector3[2] { Vector3.zero, Vector3.zero };
	/// <remarks> kN </remarks>
	public Vector3 [] torque_strength = new Vector3[2] { Vector3.zero, Vector3.zero };

	/// <summary> The current target of the agent </summary>
    public Target target;
	public IAimable turret_aim;
	public Ship myship;
	public PartsCollection parts;
	public LowLevelAI ai_low;

	public PointTo pt_following = PointTo.none;

	public bool shooting;
	public bool torque_player;
	public bool rcs_thrust_player;
	public bool engine_thrust_player = false;

    //Unity components
	private Animator anim;

	/// <summary> How much the ship is steered at each rotation (each component between -1 and 1) </summary>
	public Vector3 inp_thrust_vec = Vector3.zero;
	/// <summary> How much the ship is steered at each direction (each component between -1 and 1) </summary>
	public Vector3 inp_torque_vec = Vector3.zero;

	/// <summary> Where the turrets are aimed at </summary>
	public List<TurretGroup> turretgroup_list = new List<TurretGroup>();

	/// <summary> Ammount of fuel used per second by one port at full throttle</summary>
	private float d_fuel_rcs;

	private bool turn_around;
	private float velocity_to_center;

	private void Start () {
		anim = GetComponent<Animator>();
		target = Target.None;
		
        //fuel
        d_fuel_rcs = thrust * 1f / (RCS_ISP * 9.81f); // per s

		myship.CalculateRadius();
		parts = myship.Parts;
		myship.LateStart();
	}

    ///<summary> Starts or stops regular shooting </summary>
	public void Trigger_Shooting (bool start) {
		if (SceneGlobals.general.InMap) return;
		foreach (Weapon weapon in parts.GetAll<Weapon>()) {
			weapon.Trigger_Shooting(start);
		}
	}

    // Every physics update
	private void FixedUpdate () {
        //Controls RCS thrust
		if (SceneGlobals.Paused) goto PAUSEDRUNNTIME;
		if (myship.HasRCSFuel) {
			Vector3 torque_vec = new Vector3 (inp_torque_vec.x * (inp_torque_vec.x > 0? torque_strength[0].x: torque_strength[1].x),
											  inp_torque_vec.y * (inp_torque_vec.y > 0? torque_strength[0].y: torque_strength[1].y),
											  inp_torque_vec.z * (inp_torque_vec.z > 0? torque_strength[0].z: torque_strength[1].z));
			Vector3 thrust_vec = new Vector3 (inp_thrust_vec.x * (inp_thrust_vec.x > 0? trans_strength[0].x: trans_strength[1].x),
											  inp_thrust_vec.y * (inp_thrust_vec.y > 0? trans_strength[0].y: trans_strength[1].y),
											  inp_thrust_vec.z * (inp_thrust_vec.z > 0? trans_strength[0].z: trans_strength[1].z));

			torque_vec /= (float) myship.Mass;

			float sigm_thrust = inp_thrust_vec.x + inp_thrust_vec.y + inp_thrust_vec.z + inp_torque_vec.x + inp_torque_vec.y + inp_torque_vec.z;
			myship.DrainRCSFuel(d_fuel_rcs * sigm_thrust * Time.fixedDeltaTime);

			//Debug.Log(thrust_vec);
			myship.Torque(torque_vec);
			myship.Push(myship.Orientation * thrust_vec);
				
		} else {
			inp_thrust_vec = Vector3.zero;
			inp_torque_vec = Vector3.zero;
		}

		PAUSEDRUNNTIME:;
	}

	public void KillVelocity () {
		inp_torque_vec = Navigation.CutVector(myship.AngularVelocity * -20f);
	}

    //Every frame
	private void Update () {
		if (SceneGlobals.Paused) goto PAUSEDRUNNTIME;
		foreach (TurretGroup group in myship.TurretGroups) {
			group.Aim();
		}

		PAUSEDRUNNTIME:
		myship.Update();
	}

	/// <summary>
	///		Fires one missile
	/// </summary>
	public void FireMissile () {
		foreach (MissileLauncher launcher in parts.GetAll<MissileLauncher>()) {
			if (launcher.ReadyCount > 0) {
				launcher.Fire();
				return;
			}
		}
	}

	/// <summary> The point, where to aim, to hit an object </summary>
	/// <param name="weapon"> The gun to aim </param>
	/// <param name="tgt"> The target to shoot </param>
	/// <returns> A Point in 3D-Space </returns>
	public Vector3 Predicted (Weapon weapon, Vector3 tgt_pos, Vector3 tgt_vel) {
		float bullet_speed = weapon.BulletSpeed;
		Vector3 predicted_point = tgt_pos - (myship.Velocity - tgt_vel) / bullet_speed * Vector3.Distance(weapon.Position, tgt_pos);
		return predicted_point;
	}

	/// <summary> The point, where to aim, to hit an object </summary>
	/// <param name="weapon"> The gun to aim </param>
	/// <param name="tgt"> The target to shoot </param>
	/// <returns> A Point in 3D-Space </returns>
	public Vector3 Predicted (Turret weapon, Vector3 tgt_pos, Vector3 tgt_vel) {
		float bullet_speed = weapon.muzzle_velocity;
		Vector3 predicted_point = tgt_pos - (myship.Velocity - tgt_vel) / bullet_speed * Vector3.Distance(weapon.Position, tgt_pos);
		return predicted_point;
	}

	/// <summary> Sets current ship as player </summary>
	public void SetAsPlayer () {
		gameObject.tag = "Player";
		FileReader.FileLog("Changed/Initialized Player", FileLogType.runntime);
		var new_control = Loader.EnsureComponent<PlayerControl> (gameObject);
		SceneGlobals.ReferenceSystem = new ReferenceSystem(myship);
		SceneGlobals.ReferenceSystem.Update();
		if (GetComponent<HighLevelAI>() != null)
			GetComponent<HighLevelAI>().enabled = true;

		if (SceneGlobals.Player != null) {
			var old_control = SceneGlobals.Player.Object.GetComponent<PlayerControl>();
			new_control.positions = old_control.positions;
			new_control.rotations = old_control.rotations;
			new_control.free_rotation = old_control.free_rotation;
			Destroy(old_control);
			GameObject player_obj = SceneGlobals.Player.Object;
			if (player_obj.GetComponent<HighLevelAI>() != null)
				player_obj.GetComponent<HighLevelAI>().enabled = true;
			if (player_obj.GetComponent<AudioSource>() != null)
			player_obj.tag = "Untagged";
		} else {
			DataStructure data = DataStructure.Load(myship.config_path, "data", null).GetChild("player");
			new_control.positions = data.Get<Vector3[]>("cam pos");
			new_control.rotations = data.Get<Quaternion[]>("cam rot");
			new_control.free_rotation = data.Get<bool[]>("free rotate");
		}
	}
}