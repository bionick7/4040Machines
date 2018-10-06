using System.Collections.Generic;
using UnityEngine;

public class FighterAI : MonoBehaviour
{
	private uint ID { get; set; }
	private Ship own_ship;
	private ShipControl control_script;
	private Target target;

	public bool Active { private get; set; }

	/// <summary> The network, this ship is part of </summary>
	public Network Net { private get; set; }

	/// <summary> All the detected bullets </summary>
	public HashSet<Bullet> bullets = new HashSet<Bullet>();
	/// <summary> All the detected missiles </summary>
	public HashSet<Missile> missiles = new HashSet<Missile>();
	/// <summary> All the detected ships </summary>
	public HashSet<Ship> ennemy_ships = new HashSet<Ship>();

	public List<FuzzyVector> manuvers = new List<FuzzyVector>();
	public List<FuzzyVector> rotation_maneuvers = new List<FuzzyVector>();

	private Vector3 main_pos;
	private Weapon main_weapon;

	/// <summary>
	///		Updates it's environnement
	/// </summary>
	private void UpdateEnvironnement () {
		ennemy_ships.Clear();
		bullets.Clear();
		missiles.Clear();

		foreach (Ship s in SceneData.ship_list) {
			if ((s.Position - own_ship.Position).sqrMagnitude <= 250000) {
				if (s.side != own_ship.side) {
					ennemy_ships.Add(s);
				}
			}
		}
		foreach (Bullet b in SceneData.bullet_list) {
			if ((b.Position - own_ship.Position).sqrMagnitude <= 250000) {
				if (b.side != own_ship.side) {
					bullets.Add(b);
				}
			}
		}
		foreach (Missile m in SceneData.missile_list) {
			if ((m.Position - own_ship.Position).sqrMagnitude <= 250000) {
				if (m.side != own_ship.side) {
					missiles.Add(m);
				}
			}
		}
	}

	/// <summary>
	///		Dodges bullets and missiles
	/// </summary>
	private void Dodge () {
		foreach (Bullet bullet in bullets) {
			if (IsDanger(bullet)) {
				Vector3 vec = Vector3.ProjectOnPlane(bullet.Position - own_ship.Position, bullet.Velocity);
				manuvers.Add(new FuzzyVector(-vec.normalized, 1f));
			}
		}
		foreach (Missile miss in missiles) {
			if (IsDanger(miss)) {
				Vector3 vec = Vector3.ProjectOnPlane(miss.Position - own_ship.Position, miss.Velocity);
				manuvers.Add(new FuzzyVector(-vec.normalized, 1f));
			}
		}
	}

	private bool IsDanger (Bullet bullet) {
		if (bullet.side == own_ship.side || !bullet.Exists) {
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

	private bool IsDanger (Missile missile) {
		if (!missile.Exists || missile.side == own_ship.side || !missile.Released) {
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

	private Vector3 Predicted (Weapon weapon, Target tgt) {
		Vector3 tgt_vel = tgt.virt_ship ? Vector3.zero : tgt.Ship.Velocity;
		float bullet_speed = weapon.BulletSpeed;
		Vector3 predicted_point = tgt.Position - (own_ship.Velocity - tgt_vel) / bullet_speed * Vector3.Distance(own_ship.Position, tgt.Position);
		return predicted_point;
	}

	private Vector3 Predicted (Turret weapon, Target tgt) {
		Vector3 tgt_vel = tgt.virt_ship ? Vector3.zero : tgt.Ship.Velocity;
		float bullet_speed = weapon.muzzle_velocity;
		Vector3 predicted_point = tgt.Position - (own_ship.Velocity - tgt_vel) / bullet_speed * Vector3.Distance(own_ship.Position, tgt.Position);
		return predicted_point;
	}

	/// <summary> 
	///		Looks, if another target is wastly more important, than the curretn one.
	///		If yes, switches targets.
	/// </summary>
	private void SearchTgt () {
		if (ennemy_ships.Count == 0) {
			target = Target.None;
			return;
		}
		Ship default_ship = null;
		foreach (Ship s in ennemy_ships) {
			default_ship = s;
			break;
		}
		Ship leading_ship = !target.Exists ? default_ship : target.Ship;
		bool current_over =  Net.Strength_Ratio(leading_ship.associated_target) > 3;
		if (!current_over && target.Exists) {
			return;
		}
		foreach (Ship ship in ennemy_ships) {
			if (ship.Exists) {
				bool switch_motivation = ship.Importance > leading_ship.Importance || current_over;
				if (switch_motivation ) {
					leading_ship = ship;
				}
			}
		}
		if (leading_ship.Importance > target.Importance || !target.Exists) {
			target = leading_ship.associated_target;
			Net.Switch_Target(ID, target);
		}
	}

	/// <summary>
	///		Aims the turrets at the target
	/// </summary>
	private void AimTurrets () {
		if (!target.Exists) { return; }
		foreach (TurretGroup tg in own_ship.TurretGroups) {
			if (tg.Count > 0) {
				tg.ShootSafe();

				tg.follow_target = false;
				tg.direction = false;
				Vector3 tgt_dir = Predicted(tg[0], target) + HandyTools.RandomVector * target.Ship.radius;
				tg.TargetPos = tgt_dir;
			}
		}
	}

	/// <summary>
	///		Shoots, if in range
	/// </summary>
	private void Shoot(Vector3 dir) {
		if (!target.Exists) { return; }
		foreach (Weapon weapon in own_ship.Parts.GetAll<Weapon>()) {
			bool in_range = Vector3.Angle(weapon.Transform.forward, dir) < 3f;
			if (in_range && weapon.heat < .7 && !weapon.shooting) {
				weapon.Trigger_Shooting(true);
			} else if (!in_range && weapon.shooting) {
				weapon.Trigger_Shooting(false);
			}
		}
	}

	/// <summary>
	///		Applyes all navigation
	/// </summary>
	private void Navigate () {
		Vector3 final_translation = Vector3.zero;
		foreach (FuzzyVector fvec in manuvers) {
			final_translation += fvec.vector * fvec.importance;
		}
		if (final_translation.sqrMagnitude < .1f) {
			control_script.inp_thrust_vec = Vector3.zero;
		} else {
			control_script.inp_thrust_vec = HandyTools.CutVector(final_translation);
		}

		Vector3 final_rotation = Vector3.zero;
		foreach (FuzzyVector fvec in rotation_maneuvers) {
			final_rotation += fvec.vector * fvec.importance;
		}
		if (final_rotation.sqrMagnitude < .1f) {
			control_script.inp_torque_vec = Vector3.zero;
		} else {
			control_script.inp_torque_vec = HandyTools.CutVector(final_rotation);
		}

		rotation_maneuvers.Clear();
		manuvers.Clear();
	}

	private void Start () {
		control_script = GetComponent<ShipControl>();
		own_ship = control_script.myship;
		main_pos = own_ship.Position;
		target = Target.None;

		if (Net == null) {
			GameObject[] net_holders = GameObject.FindGameObjectsWithTag("Network");
			foreach (GameObject net_holder in net_holders) {
				Network net = net_holder.GetComponent<NetworkHost>().Net;
				if (net != null) {
					if (!net.Full && net.side == own_ship.side) {
						Net = net;
					}
				}
			}
		}
		if (Net == null) {
			Net = own_ship.side ? Network.Rogue_0 : Network.Rogue_1;
		}

		ID = Net.AddShip(own_ship, target);
	}

	private void Update () {

		if ((Time.frameCount + ID) % 120 == 0) {
			UpdateEnvironnement();
		}
		if ((Time.frameCount + ID) % 30 == 3 || target == Target.None) {
			Dodge();
			SearchTgt();
		}

		if ((Time.frameCount + ID) % 120 == 1) {
			AimTurrets();
			if (own_ship.Parts.CountReal<Weapon>() > 0) {
				main_weapon = own_ship.Parts.GetAll<Weapon>() [0];
				main_pos = main_weapon.Transform.position;
			} else {
				main_weapon = null;
				main_pos = own_ship.Position;
			}
		}

		if (Time.frameCount > 2) {
			if (target.Exists) {
				Vector3 tgt_pos = main_weapon == null ? target.Position : Predicted(main_weapon, target);
				Vector3 tgt_dir = Quaternion.Euler(HandyTools.RandomVector * 1.5f) * (tgt_pos - main_pos);
				Vector3 rot_vec = control_script.RotateTowards(tgt_dir);
				rotation_maneuvers.Add(new FuzzyVector(rot_vec.normalized, .5f));
				Shoot(tgt_dir);
			}
			Navigate();
		}
	}
}