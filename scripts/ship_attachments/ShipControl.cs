using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*
 * This script is ment to go on every active ship, regardless if it is controlled by a player or by AI.
 * It controlls the thrust, maneuvering, the weaponnery and the missiles of a ship and gives Data for the UI.
 * All effects of dammage or losing of other parts are managed here
 */

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
	public Vector3 [] trans_strength = new Vector3[2] { Vector3.zero, Vector3.zero };
	public Vector3 [] torque_strength = new Vector3[2] { Vector3.zero, Vector3.zero };

	/// <summary> The current target of the agent </summary>
    public Target target;
	public Ship myship;
	public PartsCollection parts;

	//PRIVATE VARIABELS
	//------------------
	public bool shooting;

    //Unity components
	private Animator anim;

	/// <summary> How much the ship is steered at each rotation (each component between -1 and 1) </summary>
	public Vector3 inp_thrust_vec = Vector3.zero;
	/// <summary> How much the ship is steered at each direction (each component between -1 and 1) </summary>
	public Vector3 inp_torque_vec = Vector3.zero;

	/// <summary> Where the turrets are aimed at </summary>
	public List<TurretGroup> turret_aims = new List<TurretGroup>();

	//Ammount of fuel used per frame
	private float d_fuel_rcs;

	private bool turn_around;
	private float velocity_to_center;

	private void Start () {
		anim = GetComponent<Animator>();
		target = Target.None;
		
        //fuel
        d_fuel_rcs = thrust * Time.fixedDeltaTime / (RCS_ISP * 9.81f); // per s

		myship.CalculateRadius();
		parts = myship.Parts;
		myship.LateStart();
	}

    ///<summary> Starts or stops regular shooting </summary>
	public void Trigger_Shooting (bool start) {
		foreach (Weapon weapon in parts.GetAll<Weapon>()) {
			weapon.Trigger_Shooting(start);
		}
	}

    //Every physics update
	private void FixedUpdate () {
        //Controls RCS thrust
        if (myship.RCSFuel > 0)
        {

			Vector3 torque_vec = new Vector3 (inp_torque_vec.x * (inp_torque_vec.x > 0? torque_strength[0].x: torque_strength[1].x),
											  inp_torque_vec.y * (inp_torque_vec.y > 0? torque_strength[0].y: torque_strength[1].y),
											  inp_torque_vec.z * (inp_torque_vec.z > 0? torque_strength[0].z: torque_strength[1].z));
			Vector3 thrust_vec = new Vector3 (inp_thrust_vec.x * (inp_thrust_vec.x > 0? trans_strength[0].x: trans_strength[1].x),
											  inp_thrust_vec.y * (inp_thrust_vec.y > 0? trans_strength[0].y: trans_strength[1].y),
											  inp_thrust_vec.z * (inp_thrust_vec.z > 0? trans_strength[0].z: trans_strength[1].z));

			torque_vec /= (float) myship.Mass;

			//	PROVISORESCH
			myship.RCSFuel -= d_fuel_rcs * (inp_thrust_vec.magnitude + inp_torque_vec.magnitude);

			myship.Torque(torque_vec);
            myship.Push(myship.Orientation * thrust_vec);
        }
        else
        {
            inp_thrust_vec = Vector3.zero;
            inp_torque_vec = Vector3.zero;
        }

		foreach (MissileLauncher launcher in parts.GetAll<MissileLauncher>()) {
			foreach (Missile missile in launcher.Flying) {
				missile.PhysicsUpdate();
			}
		}
	}

	public void KillVelocity () {
		inp_torque_vec = HandyTools.CutVector(myship.AngularVelocity * -20f);
	}

    //Every frame
	private void Update () {
		if (myship.Position.sqrMagnitude > 100000000) {
			turn_around = true;
		}
		velocity_to_center = Vector3.Dot(myship.Velocity.normalized, Vector3.zero - myship.Position);
		if (myship.Velocity.magnitude > 50 && velocity_to_center < 0) {
			myship.Throttle = 0;
		}
		foreach (TurretGroup group in turret_aims) {
			group.Update();
			group.Aim();
		}

		myship.Update();

		if (turn_around) {
			if (velocity_to_center < 5) {
				if (Vector3.Angle(transform.forward, Vector3.zero - myship.Position) < 5) {
					myship.Throttle = 1;
				} else {
					inp_torque_vec = RotateTowards(Vector3.zero - myship.Position);
				}
			} else {
				myship.Throttle = 0;
				turn_around = false;
			}
		}
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

	/// <summary>
	///		Rotates towards a given target
	/// </summary>
	/// <param name="tgt"> The position to rotate to </param>
	public Vector3 RotateTowards (Vector3 tgt_direction) {
		Vector3 rot_vec = new Vector3();
		Vector3 fore = transform.forward;
		Vector3 angvel = myship.AngularVelocity;

		float velocity_multiplyer = 100f;

		Quaternion rot = Quaternion.Euler(myship.AngularVelocity * -velocity_multiplyer);
		Vector3 direction = rot * tgt_direction;
		Vector3 diff_vector = (direction.normalized - fore);
		Vector3 pure_diff = Quaternion.Inverse(transform.rotation) * diff_vector;

		// pitch (x)
		float y_diff = pure_diff.y;
		float pitch_multiplyer = -y_diff / Mathf.Abs(y_diff);
		if (angvel.x * pitch_multiplyer > .15f) { return Vector3.zero; }
		rot_vec.x = pitch_multiplyer;
		
		// yaw (y)
		float x_diff = pure_diff.x;
		float yaw_multiplyer = x_diff / Mathf.Abs(x_diff);
		if (angvel.y * yaw_multiplyer > .15f) { return Vector3.zero; }
		rot_vec.y = yaw_multiplyer;

		return rot_vec;
	}

	public void OnDrawGizmos () {
		//Gizmos.DrawWireSphere(myship.Position, myship.radius);
	}

	public void OnGUI () {
		GUI.Label(new Rect(0, 100, 100, 100), velocity_to_center.ToString());
	}
}