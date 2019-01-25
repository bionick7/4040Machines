using System.Collections.Generic;
using UnityEngine;

public class RCSFiring : MonoBehaviour {
	public GameObject rcs_mesh;
	public float strength;
	public float angular_limitation = 1;
	public Vector3 center_of_mass = Vector3.zero;
	public Vector3 [] positions;
	public Quaternion [] directions;

	private Dictionary<ParticleSystem, float> trans_Up = new Dictionary<ParticleSystem, float>();
	private Dictionary<ParticleSystem, float> trans_Star = new Dictionary<ParticleSystem, float>();
	private Dictionary<ParticleSystem, float> trans_Fore = new Dictionary<ParticleSystem, float>();

	private Dictionary<ParticleSystem, float> rot_pitch = new Dictionary<ParticleSystem, float>();
	private Dictionary<ParticleSystem, float> rot_yaw = new Dictionary<ParticleSystem, float>();
	private Dictionary<ParticleSystem, float> rot_roll = new Dictionary<ParticleSystem, float>();

	private ShipControl control_script;
	private float sound_timer;

	private void Start () {
		ShipControl control_script = GetComponent<ShipControl>();

		// Vectors ment to calculate the total strength of 
		Vector3 rot_strength_pos = Vector3.zero;
		Vector3 rot_strength_neg = Vector3.zero;
		Vector3 trans_strength_pos = Vector3.zero;
		Vector3 trans_strength_neg = Vector3.zero;

		for (int x=0; x < Mathf.Min(positions.Length, directions.Length); x++){
			Quaternion rot = transform.rotation * directions[x];
			float lr=0, ud=0, fa=0;
			Vector3 new_pos = transform.position + (transform.rotation * positions[x]);
			GameObject new_rcs = Instantiate(rcs_mesh, new_pos, rot);
			new_rcs.transform.SetParent(transform, true);
			ParticleSystem new_ps = new_rcs.GetComponent<ParticleSystem> ();
			new_ps.Stop();

			// Calculate the strengths of the concerned rcs port
			lr = Mathf.Cos(Vector3.Angle(directions[x] * Vector3.left, Vector3.forward) * Mathf.Deg2Rad);
			ud = Mathf.Cos(Vector3.Angle(directions [x] * Vector3.up, Vector3.forward) * Mathf.Deg2Rad);
			fa = -Mathf.Cos(Vector3.Angle(directions [x] * Vector3.forward, Vector3.forward) * Mathf.Deg2Rad);

			Vector3 momentum_pos = positions[x] - center_of_mass;

			float pitch_force = -(ud * momentum_pos.z + fa * momentum_pos.y) * angular_limitation;
			float yaw_force = -(lr * momentum_pos.z + fa * momentum_pos.x) * angular_limitation;
			float roll_force = (ud * momentum_pos.x + lr * momentum_pos.y) * angular_limitation;

			// Adding the calculated values to the total_strength vectors
			if (lr > 0) { trans_strength_pos.x += lr; } else { trans_strength_neg.x -= lr; }
			if (ud > 0) { trans_strength_pos.y += ud; } else { trans_strength_neg.y -= ud; }
			if (fa > 0) { trans_strength_pos.z += fa; } else { trans_strength_neg.z -= fa; }
			if (pitch_force > 0) { rot_strength_pos.x += pitch_force; } else { rot_strength_neg.x -= pitch_force; }
			if (yaw_force > 0) { rot_strength_pos.y += yaw_force; } else { rot_strength_neg.y -= yaw_force; }
			if (roll_force > 0) { rot_strength_pos.z += roll_force; } else { rot_strength_neg.z -= roll_force; }

			// Saving the values partially
			trans_Up.Add(new_ps, -ud);
			trans_Star.Add(new_ps, lr);
			trans_Fore.Add(new_ps, -fa);
			rot_pitch.Add(new_ps, pitch_force);
			rot_yaw.Add(new_ps, yaw_force);
			rot_roll.Add(new_ps,roll_force);
		}


		// Gives the strength values for all directions to the control script
		control_script.torque_strength = new Vector3 [2] { rot_strength_pos * strength, rot_strength_neg * strength };
		control_script.trans_strength = new Vector3 [2] { trans_strength_pos * strength, trans_strength_neg * strength };
	}
	
	private void FixedUpdate () {
		if (SceneGlobals.Paused) goto PAUSEDRUNNTIME;
		//Get the steering
		if (control_script == null) {
			control_script = GetComponent<ShipControl>();
		}
		Vector3 transl_inp;
		Vector3 rot_inp;
		transl_inp = control_script.inp_thrust_vec;
		rot_inp = control_script.inp_torque_vec;
		bool firing = false;

		foreach (ParticleSystem ps in trans_Up.Keys) {
			//Calculate the actual intnsity of the given rcs port
			Vector3 transl_power = new Vector3(trans_Star[ps], trans_Up[ps], trans_Fore[ps]) * -1;
			Vector3 rot_power = new Vector3(rot_pitch[ps], rot_yaw[ps], rot_roll[ps]);
			float power = Mathf.Min(Mathf.Max(Vector3.Dot(transl_inp.normalized, transl_power) + Vector3.Dot(rot_inp.normalized, rot_power), -1.0f), 1.0f);

			//Start or stop the effect based on the intensity
			if (power > 0.3f) {
				if (!ps.isPlaying) {
					ps.Play();
					sound_timer = 0;
				}
				if (sound_timer >= .5f) {
					sound_timer = 0;
				}
				firing = true;
			} else {
				if (ps.isPlaying) {
					ps.Stop();
				}
			}
		}
		if (control_script.myship.IsPlayer) {
			if (firing) {
				Globals.audio.RCSPlay();
			} else {
				Globals.audio.RCSStop();
			}
		}
		PAUSEDRUNNTIME:;
	}

	/// <summary> Should be called, if the game is paused/unpaused </summary>
	/// <param name="pause"> If the game is paused or unpaused </param>
	public void OnPause (bool pause) {
		if (pause) {
			foreach (ParticleSystem ps in trans_Up.Keys) ps.Pause(); 
		} else {
			foreach (ParticleSystem ps in trans_Up.Keys) ps.Play();
		}
	}

	private void Update () {
		sound_timer += Time.deltaTime;
	}
}