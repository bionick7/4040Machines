using System.Collections.Generic;
using UnityEngine;

/* ===============================================================================
 * The Script for the AI of fighter ships.
 * Gets attached directly to the ship.
 * Manages navigation, fixed weapons, turrets and communication with other vessels
 * =============================================================================== */

public class LowLevelAI : MonoBehaviour
{
	private uint ID { get; set; }
	public Ship own_ship;
	private ShipControl control_script;

	public bool Active { private get; set; }

	public Network net;

	/// <summary> All the detected bullets </summary>
	public HashSet<Bullet> bullets = new HashSet<Bullet>();
	/// <summary> All the detected missiles </summary>
	public HashSet<Missile> missiles = new HashSet<Missile>();
	/// <summary> All the detected ships </summary>
	public HashSet<Ship> ennemy_ships = new HashSet<Ship>();

	public Navigation navigator;

	public Quack<AIMouvementCommand> movement_quack = new Quack<AIMouvementCommand>();
	public List<AIActionCommand> action_list = new List<AIActionCommand>();

	public bool HasHigherAI { get; set; }

	private Vector3 main_pos;
	private Weapon main_weapon;
	public HighLevelAI high_level;

	/// <summary> Checks, if a bullet represents a danger for the vessel </summary>
	private bool IsDanger (Bullet bullet) {
		if (bullet.Friendly == own_ship.Friendly || !bullet.Exists) {
			return false;
		}
		if ((bullet.Position - own_ship.Position).sqrMagnitude > 40000) {
			return false;
		}
		Vector3 position_diff = own_ship.Position - bullet.Position;
		if (Vector3.Angle(bullet.Velocity, position_diff) > Mathf.Atan((own_ship.radius + 5) / position_diff.magnitude) * Mathf.Rad2Deg) {
			return false;
		}
		return true;
	}

	/// <summary> Checks, if a missile re4presents a danger for the vessel </summary>
	private bool IsDanger (Missile missile) {
		if (!missile.Exists || missile.Friendly == own_ship.Friendly || !missile.Released) {
			return false;
		}
		if ((missile.Position - own_ship.Position).magnitude > 100) {
			return false;
		}
		if (Vector3.Dot(own_ship.Velocity - missile.Velocity, own_ship.Position - missile.Position) > -20) {
			return false;
		}
		return true;
	}

	#region public
	/// <summary>
	///		Updates it's environnement
	/// </summary>
	public void UpdateEnvironnement () {
		ennemy_ships.Clear();
		bullets.Clear();
		missiles.Clear();

		foreach (Ship s in SceneGlobals.ship_collection) {
			if (!s.Exists) continue;
			if ((s.Position - own_ship.Position).sqrMagnitude <= 250000) {
				if (s.Friendly != own_ship.Friendly) {
					ennemy_ships.Add(s);
				}
			}
		}
		foreach (Bullet b in SceneGlobals.bullet_collection) {
			if (!b.Exists) continue;
			if ((b.Position - own_ship.Position).sqrMagnitude <= 250000) {
				if (b.Friendly ^ own_ship.Friendly) {
					bullets.Add(b);
				}
			}
		}
		foreach (Missile m in SceneGlobals.missile_collection) {
			if (!m.Exists) continue;
			if ((m.Position - own_ship.Position).sqrMagnitude <= 250000) {
				if (m.Friendly != own_ship.Friendly) {
					missiles.Add(m);
				}
			}
		}
	}

	/// <summary>
	///		Dodges bullets and missiles
	/// </summary>
	public void Dodge () {
		foreach (Bullet bullet in bullets) {
			if (IsDanger(bullet)) {
				Vector3 vec = Vector3.ProjectOnPlane(bullet.Position - own_ship.Position, bullet.Velocity);
				navigator.AddTranslation(-vec.normalized, 1f);
			}
		}
		foreach (Missile miss in missiles) {
			if (IsDanger(miss)) {
				Vector3 vec = Vector3.ProjectOnPlane(miss.Position - own_ship.Position, miss.Velocity);
				navigator.AddTranslation(-vec.normalized, 1f);
			}
		}
	}

	/// <summary> Aims all the turrets at the target </summary>
	/// <param name="tgtpos"> The target position in world space </param>
	public void AimAllTurrets (Vector3 tgtpos) {
		foreach (TurretGroup tg in own_ship.TurretGroups) {
			AimOneTurretGroup(tg, tgtpos);
		}
	}

	/// <summary> Aims one specific turretgroup at a given position </summary>
	/// <param name="group"> The turretgroup to aim </param>
	/// <param name="tgtpos"> The target position in world space </param>
	public void AimOneTurretGroup (TurretGroup group, Vector3 tgtpos) {
		if (group.Count > 0) {
			group.ShootSafe();

			group.follow_target = false;
			group.direction = false;
			Vector3 tgt_dir = tgtpos;
			group.target_pos = tgt_dir;
		}
	}

	/// <summary> Fires off all the turrets on the ship </summary>
	/// <param name="safe"> If the group shoul check, if it is aimed right </param>
	public void ShootAllTurrets (bool safe=true) {
		foreach (TurretGroup tg in own_ship.TurretGroups) {
			ShootOneTurretGroup(tg, safe);
		}
	}

	/// <summary> Fires off one turretgroup </summary>
	/// <param name="group"> The concerned group </param>
	/// <param name="safe"> If the group shoul check, if it is aimed right </param>
	public void ShootOneTurretGroup (TurretGroup group, bool safe=true) {
		group.ShootSafe();
	}

	/// <summary>
	///		Shoots, if in range
	/// </summary>
	public void Shoot (Vector3 dir) {
		foreach (Weapon weapon in own_ship.Parts.GetAll<Weapon>()) {
			bool in_range = Vector3.Angle(weapon.Transform.forward, dir) < 3f;
			if (in_range && weapon.heat < .7 && !weapon.shooting) {
				weapon.Trigger_Shooting(true);
			} else if (!in_range && weapon.shooting) {
				weapon.Trigger_Shooting(false);
			}
		}
	}

	/// <summary> Calculates the torque, that has to be applied to the ship, to turn </summary>
	/// <param name="tgt_dir"> the direction to turn to in world space </param>
	/// <returns> The torqu in eulerangle, degrees </returns>
	public Vector3 CalculateTurn (Vector3 tgt_dir) {
		// The angular velocity in world space
		Vector3 angvel = own_ship.AngularVelocity;
		float angular_acceleration = own_ship.CalculateRotationStrength(Vector3.Cross(own_ship.Transform.forward, tgt_dir).normalized);
		Quaternion rot = Quaternion.identity;
		if (angvel != Vector3.zero) {
			Vector3 rot_ang = -angvel * 4 * angvel.magnitude / angular_acceleration;
			//Debug.DrawRay(own_ship.Position, Quaternion.Euler(rot_ang) * Vector3.forward * 10, Color.blue);
			if (Vector3.Dot(Vector3.forward, tgt_dir) < 0) {
				rot_ang.x *= -1;
			}
			rot = Quaternion.Euler(rot_ang);
		}
		
		// direction is the direction, we are aiming for
		Vector3 direction = rot * tgt_dir;
		float clock_angle = Vector3.SignedAngle(transform.right, Vector3.ProjectOnPlane(direction, transform.forward), transform.forward) * Mathf.Deg2Rad;
		
		float x_acc = Mathf.Sin(clock_angle) < 0 ? control_script.torque_strength [0].x : control_script.torque_strength [1].x;
		float y_acc = Mathf.Cos(clock_angle) > 0 ? control_script.torque_strength [0].y : control_script.torque_strength [1].y;
		/*
		Debug.DrawRay(own_ship.Position, direction.normalized * 10, Color.red);
		Debug.DrawRay(own_ship.Position, tgt_dir.normalized * 10, Color.black);
		Debug.DrawRay(own_ship.Position, rot * Vector3.back * 10, Color.blue);
		Debug.DrawRay(own_ship.Position, direction, Color.red);
		Vector3 ind = Quaternion.Euler(0, 0, clock_angle * Mathf.Rad2Deg) * Vector3.right;
		Debug.DrawRay(own_ship.Position, transform.rotation * ind * 10);
		Debug.DrawRay(own_ship.Position, transform.rotation * new Vector3(-Mathf.Sin(clock_angle) * x_acc, Mathf.Cos(clock_angle) * y_acc).normalized * 20, Color.green);
		*/
		return new Vector3(-Mathf.Sin(clock_angle) * x_acc, Mathf.Cos(clock_angle) * y_acc).normalized;

		/*
		Vector3 rot_vec = Vector3.zero;
		Vector3 fore = transform.forward;
		// Angular velocity in radians
		Vector3 angvel = Quaternion.Inverse(transform.rotation) * own_ship.AngularVelocity;
		Vector3 angvel_rad = angvel.normalized * Mathf.Deg2Rad;

		float angular_acceleration_x = own_ship.CalculateRotationStrength(Vector3.right);
		float angular_acceleration_y = own_ship.CalculateRotationStrength(Vector3.up);
		float angular_acceleration_z = own_ship.CalculateRotationStrength(Vector3.forward);

		float tot_angle = Vector3.Angle(fore, tgt_dir);

		// Roll (z)
		float z_angle = Vector3.SignedAngle(
			transform.up,
			Vector3.ProjectOnPlane(Vector3.up, transform.right),
			transform.forward
		);
		print(z_angle);
		float counter_velocity_z = 1.5f * angvel.z * Mathf.Abs(angvel.z) / angular_acceleration_z;
		z_angle -= counter_velocity_z;
		rot_vec.z = z_angle / tot_angle;

		//if (z_angle > 10) return rot_vec;


		// Pitch (x)
		Vector3 tg_project = Vector3.ProjectOnPlane(tgt_dir, Vector3.up);
		float target_inclination = Vector3.SignedAngle(
			tgt_dir,
			tg_project,
			Vector3.Cross(tgt_dir, tg_project)
		);
		Vector3 pl_project = Vector3.ProjectOnPlane(fore, Vector3.up);
		float player_inclination = Vector3.SignedAngle(
			fore,
			pl_project,
			Vector3.Cross(fore, pl_project)
		);

		float x_angle = target_inclination - player_inclination;
		float counter_velocity_x = 1.5f * angvel.x * Mathf.Abs(angvel.x) / angular_acceleration_x;
		x_angle -= counter_velocity_x;
		rot_vec.x = z_angle > 10 ? -counter_velocity_x / Mathf.Abs(counter_velocity_x) : x_angle / tot_angle;

		// Yaw (y)
		float y_angle = Vector3.SignedAngle(
			transform.forward,
			Vector3.ProjectOnPlane(tgt_dir, transform.up),
			transform.up
		);
		float counter_velocity_y = 1.5f * angvel.y * Mathf.Abs(angvel.y) / angular_acceleration_y;
		y_angle -= counter_velocity_y;
		rot_vec.y = z_angle > 10 ? -counter_velocity_y / Mathf.Abs(counter_velocity_y) : y_angle / tot_angle;

		Debug.DrawRay(own_ship.Position, tgt_dir.normalized * 10, Color.black);

		Debug.DrawRay(own_ship.Position, transform.rotation * Quaternion.Euler(x_angle, 0, 0) * Vector3.forward * 10, Color.red);
		Debug.DrawRay(own_ship.Position, transform.rotation * Quaternion.Euler(0, y_angle, 0) * Vector3.forward * 10, Color.green);

		return rot_vec;
		*/
	}

	/// <summary> Turns in a given direction </summary>
	/// <param name="tgt_dir">  to turn to in world space </param>
	public void ApplyTurn (Vector3 tgt_dir, float importance) {
		navigator.AddRotation(CalculateTurn(tgt_dir), importance);
	}

	/// <summary> Attacks a given target, giving the Ai full autonomy over movement </summary>
	/// <param name="tg"> The concerned target </param>
	public void Attack (Target tg) {
		movement_quack.PushBack(AIMouvementCommand.StayLocked(double.PositiveInfinity, own_ship, tg));
		action_list.Add(AIActionCommand.FireMain(tg));
	}

	/// <summary> Attacks a given target, but only with the turrets </summary>
	/// <param name="tg"> The concerned target </param>
	public void TurretAttack (Target tg) {
		//action_list.Add(AIActionCommand.);
	}

	public void Point2 (Vector3 direction) {
		movement_quack.PushBack(AIMouvementCommand.Turn(Quaternion.FromToRotation(own_ship.Transform.forward, direction), own_ship, true));
	}

	public void RCSThrust (Vector3 delta_v, bool prioritize=false) {
		if (prioritize) {
			movement_quack.PushFront(AIMouvementCommand.RCSAccelerate(delta_v, own_ship));
		} else {
			movement_quack.PushBack(AIMouvementCommand.RCSAccelerate(delta_v, own_ship));
		}
	}

	public void Thrust (Vector3 delta_v, bool prioritize=false) {
		if (prioritize) {
			movement_quack.PushFront(AIMouvementCommand.EngineAccelerate(delta_v, own_ship));
		} else {
			movement_quack.PushBack(AIMouvementCommand.EngineAccelerate(delta_v, own_ship));
		}
	}

	public void Point2Command (int code) {
		Quaternion turn = Quaternion.identity;
		switch (code) {
		case -3:
			//TG vel -
			turn = Quaternion.FromToRotation(own_ship.Transform.forward, own_ship.Velocity - own_ship.Target.Velocity);
			break;
		case -2:
			//TG -
			// In Progress
			movement_quack.PushBack(AIMouvementCommand.StayLocked(double.PositiveInfinity, own_ship, own_ship.Target));
			break;
		case -1:
			//Vel -
			turn = Quaternion.FromToRotation(own_ship.Transform.forward, -SceneGlobals.ReferenceSystem.RelativeVelocity(own_ship.Velocity));
			break;
		case 1:
			//Vel +
			turn = Quaternion.FromToRotation(own_ship.Transform.forward, SceneGlobals.ReferenceSystem.RelativeVelocity(own_ship.Velocity));
			break;
		case 2:
			//TG +
			movement_quack.PushBack(AIMouvementCommand.StayLocked(double.PositiveInfinity, own_ship, own_ship.Target));
			return;
		case 3:
			//TG Vel +
			turn = Quaternion.FromToRotation(own_ship.Transform.forward, own_ship.Target.Velocity - own_ship.Velocity);
			break;
		case 4:
			// Cancel (same as idle)
			Idle();
			return;
		default:
			return;
		}
		movement_quack.PushBack(AIMouvementCommand.Turn(turn, own_ship));
	}

	public void Idle () {
		movement_quack.Clear();
	}

	public void Flee () {
		Quaternion turn = Quaternion.FromToRotation(Vector3.forward, own_ship.Position);
		movement_quack.PushFront(new AIMouvementCommand [] {
			AIMouvementCommand.Turn(turn, own_ship, true),
			AIMouvementCommand.EngineAccelerate(own_ship.Position * 100, own_ship)
		});
	}

	/// <summary> Matches Velocity with target </summary>
	/// <param name="tg"> The concerned target </param>
	public void MatchVelocity (Target tg) {
		Vector3 d_v = tg.Velocity - own_ship.Velocity;
		if (d_v.sqrMagnitude > 4) {
			Quaternion turn = Quaternion.Inverse(own_ship.Orientation) * Quaternion.FromToRotation(Vector3.forward, d_v);
			movement_quack.PushBack(AIMouvementCommand.Turn(turn, own_ship));
			movement_quack.PushBack(AIMouvementCommand.EngineAccelerate(d_v, own_ship));
		} else {
			movement_quack.PushBack(AIMouvementCommand.RCSAccelerate(d_v, own_ship));
		}
	}

	/// <summary> Matches velocities with target the closest to target as possible </summary>
	/// <param name="tg"> The concerned target </param>
	public void MatchVelocityNearTarget (Target tg) {
		Vector3 d_v = tg.Velocity - own_ship.Velocity;
		if (Vector3.Dot(own_ship.Position - tg.Position, d_v) < 0) {
			MatchVelocity(tg); //				->	 ->
			return; // has to be catching up -> Δx · Δv > 0
		}
		float relative_velocity = d_v.magnitude;
		float acceleration = own_ship.MaxAcceleration;
		if (relative_velocity > 4) {
			float breaking_distance = .5f * d_v.sqrMagnitude / acceleration;
			float full_distance = Vector3.Project(tg.Position - own_ship.Position, d_v).magnitude;

			float plus_dist = 0;
			if (!tg.virt_ship) {
				plus_dist = tg.Ship.radius;
			}

			Quaternion turn = Quaternion.FromToRotation(Vector3.forward, d_v);
			movement_quack.PushBack(AIMouvementCommand.Turn(turn, own_ship, true));
			movement_quack.PushBack(AIMouvementCommand.HoldOrientation((full_distance - breaking_distance - plus_dist) / relative_velocity, own_ship));
			movement_quack.PushBack(AIMouvementCommand.EngineAccelerate(d_v, own_ship));
		} else {
			float breaking_time = relative_velocity / acceleration;
			float breaking_distance = .5f * d_v.sqrMagnitude / acceleration;
			float full_distance = Vector3.Project(tg.Position - own_ship.Position, d_v).magnitude;

			movement_quack.PushBack(AIMouvementCommand.RCSAccelerate(d_v, own_ship));
		}
	}
	#endregion

	public void Start_ () {
		control_script = GetComponent<ShipControl>();
		own_ship = control_script.myship;
		main_pos = own_ship.Position;
		navigator = new Navigation(control_script);
		own_ship.low_ai = this;

		high_level = Loader.EnsureComponent<HighLevelAI>(gameObject);
		own_ship.high_ai = high_level;
		high_level.Start_(this);
		high_level.enabled = HasHigherAI;
	}

	private void Update () {
		if (SceneGlobals.Paused) return;

		if ((Time.frameCount + ID) % 120 == 0) {
			UpdateEnvironnement();
		}

		if (movement_quack.Count > 0) {
			AIMouvementCommand curr_Mcommand = movement_quack.Get();
			if (curr_Mcommand.IsAccomplished(own_ship)) {
				movement_quack.ClearTop();
			} else {
				curr_Mcommand.Execute(ref navigator, own_ship);
			}
		}

		action_list.RemoveAll(x => x.IsAccomplished(own_ship));
		foreach (AIActionCommand command in action_list) {
			command.Execute(ref navigator, own_ship);
		}

		if (Time.frameCount > 2) {
			navigator.Navigate();
		}

		high_level.Update_();
	}

	public string Execute (string command) {
		string [] arguments = command.Split(' ');
		switch (arguments [0]) {
		case "turn":
			Vector3 vec = new Vector3(float.Parse(arguments[1]),
									  float.Parse(arguments[2]),
									  float.Parse(arguments[3]));
			if (arguments.Length >= 5 && arguments [4] == "F") {
				movement_quack.PushFront(AIMouvementCommand.Turn(Quaternion.Euler(vec), own_ship));
			} else {
				movement_quack.PushBack(AIMouvementCommand.Turn(Quaternion.Euler(vec), own_ship));
			}
			return string.Format("Turn planned sucessfully ({0:0.00}, {1:0.00}, {2:0.00})", vec.x, vec.y, vec.z);

		case "rcs":
			Vector3 vec1 = new Vector3(float.Parse(arguments[1]),
									   float.Parse(arguments[2]),
									   float.Parse(arguments[3]));
			RCSThrust(vec1, arguments.Length >= 5 && arguments [4] == "F");
			return string.Format("RCS Acceleration planned sucessfully ({0:0.00}, {1:0.00}, {2:0.00})", vec1.x, vec1.y, vec1.z);

		case "main":
			Vector3 vec2 = new Vector3(float.Parse(arguments[1]),
									   float.Parse(arguments[2]),
									   float.Parse(arguments[3]));
			Thrust(vec2, arguments.Length >= 5 && arguments [4] == "F");
			return string.Format("Engine Acceleration planned sucessfully ({0:0.00}, {1:0.00}, {2:0.00})", vec2.x, vec2.y, vec2.z);

		default:
			return string.Format("Command {0} not known (nms intern)", arguments [0]);
		}
	}

	public string ShowCommands () {
		string res = string.Format("Showing Quack ({0})...\n", movement_quack.Count);
		foreach (AIMouvementCommand comm in movement_quack) {
			res += " > " + comm.ToString() + "\n";
		}
		return res;
	}

	public static ulong[] Quack2MachineCode (Quack<AIMouvementCommand> quack) {
		ulong[] res = new ulong[quack.Count];

		for (uint i=0u; i < quack.Count; i++) {
			res [i] = quack [i].ToCode();
		}

		return res;
	}
}