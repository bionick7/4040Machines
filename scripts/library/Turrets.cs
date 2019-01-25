using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using FileManagement;

/// <summary> The different choices of targets </summary>
public enum TargetChoice
{
	cursor,
	target
}

/// <summary> A turret of a ship </summary>
public class Turret : ShipPart
{
	public new const string enum_opt = "turret";
	public new const int max_on_ship = 1000;

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

	public string sound_name;

	public float delta_time;
	private Quaternion compens_rot;

	public float heat = 0f;
	public float ooo_time = 0f;
	public float init_ooo_time = 3f;

	public float reload_speed;
	public float muzzle_velocity;
	public Vector3 [] muzzle_positions;
	public Ammunition ammo_type;

	private Vector3 mid_pos = Vector3.zero;
	public Vector3 MidPos {
		get { return mid_pos + Position; }
	}

	private Animator anim;

	public Quaternion BodyRot {
		get { return body.rotation * compens_rot; }
	}

	public Quaternion BarrelRot {
		get { return barrels.rotation * compens_rot; }

	}

	public uint Ammunition {
		get {
			if (ParentShip.AmmoAmounts.ContainsKey(ammo_type))
				return ParentShip.AmmoAmounts [ammo_type];
			return 0;
		}
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
	public Turret (float [] ranges, GameObject obj, float [] turning_rates, float mass, float init_hp=10f) : base(init_hp, obj, mass) {
		min_range_horizontal = ranges [0];
		max_range_horizontal = ranges [1];
		min_range_vertical = ranges [2];
		max_range_vertical = ranges [3];

		group = TurretGroup.Trashbin;

		sockel = Transform.GetChild(0);
		body = sockel.GetChild(0);
		barrels = body.GetChild(0);

		horizontal_rotating_rate = turning_rates [0];
		vertical_rotating_rate = turning_rates [1];

		compens_rot = Quaternion.Inverse(body.rotation * Quaternion.Inverse(sockel.rotation)) * Quaternion.Euler(0, 90, 90);
		Enabled = true;

		anim = obj.GetComponent<Animator>();
	}

	private bool late_started = false;
	private void LateStart () {
		Vector3 muzzle_sum = Vector3.zero;
		for (int i=0; i < muzzle_positions.Length; i++) {
			muzzle_sum += muzzle_positions [i];
		}
		mid_pos = muzzle_sum / muzzle_positions.Length;
		late_started = true;
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
			if (!ParentShip.SubstractAmmo(ammo_type, 1)) { return; }

			// Initialize Bullet
			Bullet.Spawn(
				ammo_type,
				Position + barrels.rotation * muzzle_positions [i],
				BarrelRot,
				SceneGlobals.ReferenceSystem.RelativeVelocity(ParentShip.Velocity) + BarrelRot * Vector3.back * muzzle_velocity,
				ParentShip.Friendly
			);

			// Initialize Hull

			// ---------
			// To Come
			// ---------
		}

		if (anim != null) {
			anim.SetTrigger("fire");
		}
		if (ParentShip.IsPlayer)
			Globals.audio.ShootingSound(sound_name);

		heat += .2f;
		delta_time = 0f;
	}

	public void Shoot (Vector3 tgt, bool is_direction=false) {
		if (delta_time < reload_speed || !Enabled) {
			return;
		}
		if (heat >= 1) {
			ooo_time = init_ooo_time;
			return;
		}

		Vector3 tgt_vec = is_direction ? tgt : tgt - barrels.position;
		RaycastHit hit;
		bool clear_way = true;
		foreach (Vector3 barrel in muzzle_positions) {
			Debug.DrawRay(Position + barrel, tgt_vec);
			if (Physics.Raycast(new Ray(Position + barrel, tgt_vec), out hit)) {
				var aimable = PinLabel.GetAimable(hit.transform);
				if (aimable != null && aimable.Friendly)
					clear_way = false;
			}
		}
		if (Vector3.Angle(BarrelRot * Vector3.back, tgt_vec) < 1 && clear_way) {
			Shoot();
		}
	}

	/// <summary> If the given point is reachable </summary>
	/// <param name="point"> Point in question in worldspace </param>
	/// <returns> True, if reachable, false if not </returns>
	public bool IsReachable (Vector3 point) {
		float angle = Vector3.Angle(Transform.up, point - Transform.position);
		if (angle < min_range_vertical || angle > min_range_horizontal) return false;

		//   ====================
		// || Under Construction ||
		//   ====================

		return true;
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
		if (!late_started) LateStart();
	}

	public override string ToString () {
		return "Turret \"" + name + "\"";
	}

	public override DataStructure Save (DataStructure ds) {
		return ds;
	}

	public static float get_angle_around (Vector3 from, Vector3 to, Vector3 axis) {
		Vector3 start = Vector3.ProjectOnPlane(from, axis);
		Vector3 end = Vector3.ProjectOnPlane(to, axis);
		float angle = Vector3.Angle(start, end);
		return angle;
	}
}


/// <summary> a class to store groups of turrets in </summary>
public class TurretGroup : IEnumerable
{
	private List<Turret> turrets = new List<Turret>();
	/// <summary> All the turrets </summary>
	public Turret[] TurretArray {
		get { return turrets.ToArray(); }
	}

	/// <summary> The official name of the turretgroup </summary>
	public string name;

	public IAimable target = Target.None;
	public bool follow_target = false;
	public bool direction;
	public Ship own_ship;

	public bool automatic = true;

	public Vector3 DefaultTgtPosition {
		get { return own_ship.Transform.forward * 1e9f; }
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

			// Provisory

			return 0;
		}
	}

	/// <summary> The summ of all the ammunition capabilities of the turrets </summary>
	public uint FullAmunition {
		get {

			// Provisory

			return 1;			
		}
	}

	/// <summary> The relative ammount of ammunition in the group (between 0 and 1) </summary>
	public float RelativeAmmunition {
		get { return Ammunition / FullAmunition; }
	}

	//[System.ComponentModel.DefaultValue(true)]
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

	public Vector3 Position {
		get {
			Vector3 res = Vector3.zero;
			for (int i=0; i < Count; i++) {
				res += turrets [i].Position;
			}
			return res / Count;
		}
	}

	public Vector3 target_pos;
	private Vector3 target_dir;
	public Vector3 GetTgtDir (Turret t) {
		if (follow_target) {
			// Aimed at moving target
			if (target.Exists) {
				// Ship Part
				if (target is ShipPart) {
					ShipPart part = target as ShipPart;
					return own_ship.control_script.Predicted(t, part.Position, part.ParentShip.Velocity) - t.MidPos;
				}
				// Ship Part Offset
				else if (target is PartOffsetAim) {
					PartOffsetAim aim = (PartOffsetAim) target;
					return own_ship.control_script.Predicted(t, aim.Position, aim.ParentPart.ParentShip.Velocity) - t.MidPos;
				}
				// Ship
				else if (target is Ship) {
					Ship shp = target as Ship;
					return own_ship.control_script.Predicted(t, shp.Position, shp.Velocity) - t.MidPos;
				}
				// Destroyable Target
				else if (target is DestroyableTarget) {
					DestroyableTarget tgt = target as DestroyableTarget;
					return own_ship.control_script.Predicted(t, tgt.Position, tgt.Velocity) - t.MidPos;
				}
				// PhysicsOffsetAim
				else if (target is PhysicsOffsetAim) {
					PhysicsOffsetAim aim = (PhysicsOffsetAim) target;
					return own_ship.control_script.Predicted(t, aim.Position, aim.ParentPhysicsObject.Velocity) - t.MidPos;
				}
				return target.Position - t.MidPos;
			} else {
				return DefaultTgtPosition;
			}
		}
		if (direction)
			return target_dir;
		return target_pos - Position;
	}
	public void SetTgtDir (Vector3 value) {
		if (follow_target) {
			throw new System.ArgumentException("target position can not be set");
		} else {
			target_dir = value;
		}
	}

	/// <param name="target_"> The target, that the turrets should follow </param>
	/// <param name="turret_array"> The turrets concerned to beginn with </param>
	/// <param name="name_"> The name of the turretgroup </param>
	public TurretGroup (IAimable target_, Turret [] turret_array, string name_) {
		name = name_;
		foreach (Turret turr in turret_array) {
			turr.Group = this;
		}
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
		target_pos = target_point;
	}

	/// <summary> Adds turrets to the group </summary>
	/// <param name="turrets_"> The turrets to add </param>
	public void Add (IEnumerable<Turret> turrets_) {
		turrets.AddRange(turrets_);
	}

	/// <summary> Adds turret to the group </summary>
	/// <param name="turret"> The turret to add </param>
	public void Add (Turret turret) {
		turrets.Add(turret);
	}

	/// <summary> Removes turrets to the group </summary>
	/// <param name="turrets_"> The turrets to remove </param>
	public void Remove (IEnumerable<Turret> turrets_) {
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
				if (!turr.Aim(GetTgtDir(turr), true)) {
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
				turr.Shoot(GetTgtDir(turr), true);
			}
		}
	}

	/// <summary> This has to be called one a frame to keep track of the time </summary>
	public void Update () {
		foreach (Turret turr in turrets) {
			if (turr.Exists) {
				turr.Update();
				if (automatic && target != null && target.Exists) {
					turr.Shoot(GetTgtDir(turr), true);
				}
			} else {
				Remove(turr);
			}
		}
	}

	public IEnumerator GetEnumerator() {
		return new Enumerator(turrets.ToArray());
	}

	public override string ToString () {
		return "TurretGroup: \"" + name + "\"";
	}

	/// <summary> Ment for unused turrets </summary>
	public static readonly TurretGroup Trashbin = new TurretGroup(Vector3.zero, new Turret [0], "bin", true) { own_ship = null};

	public DataStructure Save (DataStructure ds) {
		for (int i=0; i < turrets.Count; i++) {
			Turret turr = turrets[i];
			turr.description_ds.Name = "description" + i.ToString("000");
			ds.children.Add(i.ToString("details0000"), turr.description_ds);
		}
		ds.Set("positions", System.Array.ConvertAll(TurretArray, x => Quaternion.Inverse(own_ship.Orientation) * (x.Position - own_ship.Position)));
		ds.Set("rotations", System.Array.ConvertAll(TurretArray, x => Quaternion.Inverse(own_ship.Orientation) * x.Transform.rotation));
		ds.Set("heat", System.Array.ConvertAll(TurretArray, x => x.heat));
		ds.Set("ooo time", System.Array.ConvertAll(TurretArray, x => x.ooo_time));
		ds.Set("enabled", System.Array.ConvertAll(TurretArray, x => x.Enabled));
		ds.Set("barrel orientation", System.Array.ConvertAll(TurretArray, x => x.BarrelRot));
		return ds;
	}

	public static TurretGroup Load (DataStructure specific_data, Ship parent) {
		string name = specific_data.Name.Substring(5);

		DataStructure[] parts_data;
		if (specific_data.Contains<string[]>("parts")) {
			string[] part_names = specific_data.Get<string[]>("parts");
			parts_data = System.Array.ConvertAll(part_names, x => Globals.parts.Get<DataStructure>(x));
		} else {
			parts_data = System.Array.FindAll(specific_data.AllChildren, x => x.Name.StartsWith("description"));
		}

		int count = specific_data.Get<Vector3[]>("positions").Length;
		Vector3 [] weapon_pos = specific_data.Get<Vector3[]>("positions");
		Quaternion [] weapon_rot = specific_data.Get("rotations", new Quaternion[count]);
		float [] heats = specific_data.Get("heat", new float[count], quiet:true);
		float [] ooo_times = specific_data.Get("ooo time", new float[count], quiet:true);
		bool [] enabled_s = specific_data.Get("enabled", new bool[count], quiet:true);
		Quaternion [] barrel_rotations = specific_data.Get("barrel rotation", new Quaternion[count], quiet:true);

		
		Turret [] weapon_array = new Turret[count];

		//This is for each weapon in it
		for (int i = 0; i < count; i++) {
			DataStructure part_data = parts_data[i];

			// Range
			float [] range = new float[4] {-1f, -1f, -1f, -1f};
			if (part_data.Contains<float[]>("horizontal range")) {
				range [0] = Mathf.Abs(Mathf.Min(part_data.Get<float[]>("horizontal range")));
				range [1] = Mathf.Abs(Mathf.Max(part_data.Get<float[]>("horizontal range")));
			}
			if (part_data.floats32_arr.ContainsKey("vertical range")) {
				range [2] = Mathf.Abs(Mathf.Min(part_data.Get<float[]>("vertical range")));
				range [3] = Mathf.Abs(Mathf.Max(part_data.Get<float[]>("vertical range")));
			}

			float horizontal_rate = part_data.Get<float>("horizontal rotating rate");
			float vertical_rate = part_data.Get<float>("vertical rotating rate");

			//uint ammo = part_data.short_integers["ammunition"];

			float reload_speed = part_data.Get<float>("reload speed");
			float muzzle_velocity = part_data.Get<float>("muzzle velocity");
			Vector3 [] muzzle_positions = part_data.Get<Vector3[]>("barrels");

			GameObject pref_weapon = part_data.Get<GameObject>("source");
		
			Vector3 guns_p = parent.Position + parent.Orientation * weapon_pos[i];
			Quaternion guns_rot = parent.Orientation * weapon_rot[i];
			GameObject turret_object = Object.Instantiate(pref_weapon, guns_p, guns_rot);
			turret_object.transform.SetParent(parent.Transform);
			turret_object.name = string.Format("{0} ({1})", name, i.ToString());

			Turret turret_instance = new Turret(range, turret_object, new float[2] { horizontal_rate, vertical_rate}, part_data.Get<float>("mass"), part_data.Get<System.UInt16>("hp")){
				name = turret_object.name,
				//ammo_count = ammo,
				//full_ammunition = ammo,
				reload_speed = reload_speed,
				muzzle_velocity = muzzle_velocity,
				sound_name = part_data.Get<string>("sound"),
				ammo_type = Globals.ammunition_insts[part_data.Get<string>("ammotype")],
				muzzle_positions = muzzle_positions,
				description_ds = part_data,

				heat = heats[i],
				ooo_time = ooo_times[i],
				Enabled = enabled_s[i],
			};

			weapon_array [i] = turret_instance;

			BulletCollisionDetection turret_behaviour = Loader.EnsureComponent<BulletCollisionDetection>(turret_object);
			turret_behaviour.Part = turret_instance;
		}

		return new TurretGroup(Target.None, weapon_array, name) { own_ship = parent };
	}

	private class Enumerator : IEnumerator {

		private int position = -1;

		private Turret[] array;

		public Turret Current {
			get {
				if (position < array.Length) {
					return array [position];
				} else {
					throw new System.ArgumentOutOfRangeException();
				}
			}
		}

		object IEnumerator.Current {
			get { return Current; }
		}

		public Enumerator(Turret [] p_array) {
			array = p_array;
		}

		public void Dispose () {

		}

		public void Reset () {
			position = -1;
		}

		public bool MoveNext () {
			position++;
			return position < array.Length;
		}
	}
}