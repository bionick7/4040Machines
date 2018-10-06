using UnityEngine;
using System.Collections.Generic;

/// <summary> The different choices of targets </summary>
public enum TargetChoice
{
	cursor,
	target
}

/// <summary> A turret of a ship </summary>
public class Turret : ShipPart
{
	public GameObject unity_object;
	public string name;

	private float min_range_horizontal;
	private float max_range_horizontal;
	private float min_range_vertical;
	private float max_range_vertical;

	private float horizontal_rotating_rate;
	private float vertical_rotating_rate;

	private Transform sockel;
	private Transform body;
	private Transform barrels;

	private float delta_time;
	private Quaternion compens_rot;

	public float heat = 0f;
	public float ooo_time = 0f;
	public float init_ooo_time = 3f;

	public uint ammo_count;
	public uint full_ammunition;
	public float reload_speed;
	public float muzzle_velocity;
	public Vector3 [] muzzle_positions;
	public Ammunition ammo_type;

	private Animator anim;
	private AudioSource audio;

	public Quaternion BodyRot {
		get { return body.rotation * compens_rot; }
	}

	public Quaternion BarrelRot {
		get { return barrels.rotation * compens_rot; }
	}

	public Vector3 Position {
		get { return unity_object.transform.position; }
		set { unity_object.transform.position = value; }
	}

	public float RelativeAmmunition {
		get { return ammo_count / full_ammunition; }
	}

	[System.ComponentModel.DefaultValue(true)]
	public bool Enabled { get; set; }

	private TurretGroup group;
	public TurretGroup Group {
		get {
			return group;
		}
		set {
			group.Remove(this);
			value.Add(this);

			group = value;
		}
	}

	/// <summary> Initializer </summary>
	/// <param name="ranges">
	///		rotating limitations of the turret, first min,
	///		the max, first horizontal, then vertical, no limitation is written
	///		as -1
	/// </param>
	/// <param name="obj"> UnityEngine.GameObject, that representates the turret </param>
	/// <param name="turning_rates"> 
	///		how fast to turs in °/s horiontal first, then vertical
	///	</param>
	public Turret (float [] ranges, GameObject obj, float [] turning_rates, float mass, float init_hp=10f) : base(init_hp, obj, PartsOptions.turret, mass) {
		min_range_horizontal = ranges [0];
		max_range_horizontal = ranges [1];
		min_range_vertical = ranges [2];
		max_range_vertical = ranges [3];

		unity_object = obj;
		group = TurretGroup.Trashbin;

		sockel = unity_object.transform.GetChild(0);
		body = sockel.GetChild(0);
		barrels = body.GetChild(0);

		horizontal_rotating_rate = turning_rates [0];
		vertical_rotating_rate = turning_rates [1];

		compens_rot = Quaternion.Inverse(body.rotation * Quaternion.Inverse(sockel.rotation)) * Quaternion.Euler(0, 90, 90);
		Enabled = true;

		anim = obj.GetComponent<Animator>();
		audio = obj.GetComponent<AudioSource>();
	}

	/// <summary>
	///		Aims the turret at a specific point in space
	/// </summary>
	/// <param name="target">
	///		point in space
	/// </param>
	/// <param name="is_direction"> 
	///		if this is true, not the point in space will be targrtrd, but the weapons will be aligned to a certain direction
	/// </param>
	/// <returns> If aiming went sucsessfull </returns>
	public bool Aim (Vector3 target, bool is_direction) {
		Vector3 direction = is_direction ? target : target - body.position;
		if (ParentShip.name == "WorkhorseH1") {
			Debug.DrawRay(body.position, direction);
		}

		// HORIZONTAL ROTATION

		// turrt rotation in degrees
		float horizontal_deg = Mathf.Asin((Quaternion.Inverse(BodyRot) * (BodyRot * Vector3.back - sockel.rotation * Vector3.forward)).y) * Mathf.Rad2Deg;

		Vector3 diff_vec_hori = Quaternion.Inverse(BarrelRot) * (direction.normalized - BodyRot * Vector3.back);

		// This in which direction to rotate
		float mult_horizontal = Mathf.Abs(diff_vec_hori.x) < .02f ? 0f : -diff_vec_hori.x / Mathf.Abs(diff_vec_hori.x);

		// Checking limits
		if (!((min_range_horizontal > 0 && (horizontal_deg <= -min_range_horizontal && mult_horizontal < 0)) ||
			  (max_range_horizontal > 0 && (horizontal_deg >= max_range_horizontal && mult_horizontal > 0)))) {

			// This is how much to rotate
			float horizontal_turning_rate = horizontal_rotating_rate * mult_horizontal * Time.deltaTime;

			// Finally rotate
			body.Rotate(BodyRot * Vector3.up, horizontal_turning_rate, Space.World);
		}

		// VERTICAL ROTATION

		// barrel elevation in dergrees
		float vertical_deg = -Mathf.Asin((Quaternion.Inverse(BarrelRot) * (BodyRot * Vector3.back - BarrelRot * Vector3.back)).y) * Mathf.Rad2Deg;

		Vector3 diff_vec_vert = Quaternion.Inverse(BodyRot) * (direction.normalized - BarrelRot * Vector3.back);
		// This in which direction to rotate
		float mult_vertical = Mathf.Abs(diff_vec_vert.y) < .02f ? 0f : diff_vec_vert.y / Mathf.Abs(diff_vec_vert.y);

		//Debug.Log(vertical_deg);

		// Checking limits
		if (!((min_range_vertical > 0 && vertical_deg <= -min_range_vertical && mult_vertical < 0) ||
			  (max_range_vertical > 0 && vertical_deg >= max_range_vertical && mult_vertical > 0))) {

			// This is how much to rotate
			float vertical_turning_rate = vertical_rotating_rate * mult_vertical * Time.deltaTime;

			barrels.Rotate(BodyRot * Vector3.right, vertical_turning_rate, Space.World);
		}
		return true;
	}

	public void Shoot () {
		if (delta_time < reload_speed || !Enabled) {
			return;
		}
		if (heat >= 1) {
			ooo_time = init_ooo_time;
			return;
		}
		for (int i = 0; i < muzzle_positions.Length; i++) {
			if (ammo_count <= 0u) { return; }
			Vector3 spawn_pos = unity_object.transform.position + barrels.rotation * muzzle_positions [i];
			GameObject bullet = ammo_type.Source;

			GameObject bullet_obj = Object.Instantiate(bullet, spawn_pos, BarrelRot);

			ammo_count--;

			Bullet bullet_inst = new Bullet(bullet_obj, Data.ammunition_insts["40mm armor piercing"], ParentShip.side,
											ParentShip.Velocity + BarrelRot * Vector3.back * muzzle_velocity);

			BulletDamage bullet_script = Loader.EnsureComponent<BulletDamage>(bullet_obj);
			bullet_script.instance = bullet_inst;
		}

		if (anim != null) {
			anim.SetTrigger("fire");
		}
		if (audio != null) {
			audio.PlayOneShot(audio.clip, Random.Range(.7f, 1f));
		}

		heat += .2f;
		delta_time = 0f;
	}

	public void Shoot (Vector3 tgt, bool is_direction=false) {
		if (is_direction) {
			if (Vector3.Angle(BarrelRot * Vector3.back, tgt) < 5) {
				Shoot();
			}
		} else {
			if (Vector3.Angle(BarrelRot * Vector3.back, tgt - barrels.position) < 5) {
				Shoot();
			}
		}
	}

	/// <summary> This has to be called one a frame to keep track of the time </summary>
	public override void Update () {
		base.Update();
		delta_time += Time.deltaTime;
		if (heat > 0) {
			heat = Mathf.Max(0f, heat - .2f * Time.deltaTime);
		}
		if (ooo_time > 0) {
			ooo_time = Mathf.Min(0f, ooo_time - .2f * Time.deltaTime);
		}
	}

	public override string ToString () {
		return "Turret \"" + name + "\"";
	}

	public static float get_angle_around (Vector3 from, Vector3 to, Vector3 axis) {
		Vector3 start = Vector3.ProjectOnPlane(from, axis);
		Vector3 end = Vector3.ProjectOnPlane(to, axis);
		float angle = Vector3.Angle(start, end);
		return angle;
	}
}


/// <summary> a class to store groups of turrets in </summary>
public class TurretGroup
{
	private List<Turret> turrets = new List<Turret>();
	/// <summary> All the turrets </summary>
	public List<Turret> TurretList {
		get { return turrets; }
	}

	/// <summary> The official name of the turretgroup </summary>
	public string name;

	public Target target = Target.None;
	public bool follow_target = false;
	public bool direction;
	public Ship parentship;

	public Vector3 DefaultTgtPosition {
		get {
			return parentship.Transform.forward * float.MaxValue;
		}
	}

	/// <summary> How many turrets are contained within this group </summary>
	public uint Count {
		get {
			return (uint) turrets.Count;
		}
	}

	public Turret this [uint index] {
		get { return turrets [(int) index]; }
		set { turrets [(int) index] = value; }
	}

	/// <summary> The summ of all the current ammunition currently in the turrets </summary>
	public uint Ammunition {
		get {
			uint ammo = 0u;
			foreach (Turret turr in turrets) {
				ammo += turr.ammo_count;
			}
			return ammo;
		}
	}

	/// <summary> The summ of all the ammunition capabilities of the turrets </summary>
	public uint FullAmunition {
		get {
			uint full_ammo = 0u;
			foreach (Turret turr in turrets) {
				full_ammo += turr.full_ammunition;
			}
			return full_ammo;
		}
	}

	/// <summary> The relative ammount of ammunition in the group (between 0 and 1) </summary>
	public float RelativeAmmunition {
		get { return Ammunition / FullAmunition; }
	}

	[System.ComponentModel.DefaultValue(true)]
	public bool Enabled {
		get {
			bool enabled = false;
			foreach (Turret t in turrets) {
				if (t.Enabled) { enabled = true; }
			}
			return enabled;
		}
		set {
			foreach (Turret t in turrets) {
				t.Enabled = value;
			}
		}
	}

	private Vector3 target_pos;
	/// <summary> The target of the group </summary>
	public Vector3 TargetPos {
		get {
			if (follow_target) {
				if (target.Exists && !target.is_none) {
					return target.Position;
				}
				else {
					return DefaultTgtPosition;
				}
			}
			return target_pos;			
		}
		set {
			if (follow_target) {
				throw new System.ArgumentException("target position can not be set");
			} else {
				target_pos = value;
			}
		}
	}

	/// <param name="target_"> The target, that the turrets should follow </param>
	/// <param name="turret_array"> The turrets concerned to beginn with </param>
	/// <param name="name_"> The name of the turretgroup </param>
	public TurretGroup (Target target_, Turret [] turret_array, string name_) {
		name = name_;
		foreach (Turret turr in turret_array) {
			turr.Group = this;
		}
		TargetPos = target_.Position;
		target = target_;
		follow_target = true;
		Enabled = true;
	}

	/// <param name="target_point"> The point, the turrets should be targeted to</param>
	/// <param name="turret_array"> The turrets concerned to beginn with </param>
	/// <param name="name_"> The name of the turretgroup </param>
	/// <param name="is_direction"> If this is true, not the point in space, but the general direction will be targetet </param>
	public TurretGroup (Vector3 target_point, Turret [] turret_array, string name_, bool is_direction = false) {
		name = name_;
		direction = is_direction;
		foreach (Turret turr in turret_array) {
			turr.Group = this;
		}
		TargetPos = target_point;
	}

	/// <summary> Adds turrets to the group </summary>
	/// <param name="turrets_"> The turrets to add </param>
	public void Add (Turret [] turrets_) {
		turrets.AddRange(turrets_);
	}

	/// <summary> Adds turrets to the group </summary>
	/// <param name="turrets_"> The turrets to add </param>
	public void Add (List<Turret> turrets_) {
		turrets.AddRange(turrets_);
	}

	/// <summary> Adds turret to the group </summary>
	/// <param name="turret"> The turret to add </param>
	public void Add (Turret turret) {
		turrets.Add(turret);
	}

	/// <summary> Removes turrets to the group </summary>
	/// <param name="turrets_"> The turrets to remove </param>
	public void Remove (Turret [] turrets_) {
		foreach (Turret turr in turrets_) {
			turrets.Remove(turr);
		}
	}

	/// <summary> Removes turrets to the group </summary>
	/// <param name="turrets_"> The turrets to remove </param>
	public void Remove (List<Turret> turrets_) {
		foreach (Turret turr in turrets_) {
			turrets.Remove(turr);
		}
	}

	/// <summary> Removes turret to the group </summary>
	/// <param name="turret"> The turret to remove </param>
	public void Remove (Turret turret) {
		turrets.Remove(turret);
	}

	/// <summary> Returns true if the turret is in the group </summary>
	/// <param name="turret"> The turret to search for </param>
	public bool Contains (Turret turret) {
		return turrets.Contains(turret);
	}

	/// <summary> Aims the turrets to the target </summary>
	/// <returns> If aiming was successfull </returns>
	public bool Aim () {
		bool successful = true;
		foreach (Turret turr in turrets) {
			if (turr.Exists) {
				if (!turr.Aim(TargetPos, direction)) {
					successful = false;
				}
			}
		}
		return successful;
	}

	/// <summary> Lets all of the turrets shoot </summary>
	public void Shoot () {
		foreach (Turret turr in turrets) {
			if (turr.Exists) {
				turr.Shoot();
			}
		}
	}

	/// <summary> Shoots only, if the turrets are inline with the target </summary>
	public void ShootSafe () {
		foreach (Turret turr in turrets) {
			if (turr.Exists) {
				turr.Shoot(TargetPos, direction);
			}
		}
	}

	/// <summary> This has to be called one a frame to keep track of the time </summary>
	public void Update () {
		foreach (Turret turr in turrets) {
			if (turr.Exists) {
				turr.Update();
			} else {
				Remove(turr);
			}
		}
	}

	public override string ToString () {
		return "TurretGroup: \"" + name + "\"";
	}

	/// <summary> Ment for unused turrets </summary>
	public static readonly TurretGroup Trashbin = new TurretGroup(Vector3.zero, new Turret [0], "bin", true) { parentship = null};
}