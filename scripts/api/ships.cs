using System.Collections.Generic;
using System.Collections;
using System;
using FileManagement;
using UnityEngine;

public class Ship : SceneObject,
ITargetable, IMarkerParentObject
{
	public Target Associated { get; set; }

	public Target Target {
		get { return control_script.target; }
		set { control_script.target = value; }
	}

	public IAimable TurretAim {
		get { return control_script.turret_aim; }
		set { control_script.turret_aim = value; }
	}

	public MapTgtMarker Marker { get; set; }

	public TurretGroup [] TurretGroups {
		get { return control_script.turretgroup_list.ToArray(); }
	}

	public Vector3 offset;
	public LowLevelAI low_ai = null;
	public HighLevelAI high_ai = null;
	public NMS.OS.OperatingSystem os = null;
	public override string Name { get; protected set; }

	public ShipControl control_script;
	public bool virt = false;
	/// <summary> Configuration path, in case it is needed again </summary>
	public string config_path;

	public RCSFiring rcs_script;

	#region properties

	public bool IsPlayer {
		get { return this == SceneGlobals.Player; }
	}

	public override float Importance {
		get {
			if (SceneGlobals.general.ignore_player && IsPlayer) return 0f;
			float imp = Strength / (HP * HP);
			return imp;
		}
	}

	public override double Mass { get; protected set; }

	new public Vector3 Position {
		get {
			if (!Exists) throw NotExist;
			return Object.transform.position + Object.transform.rotation * offset;
		}
		set {
			if (!Exists) throw NotExist;
			Object.transform.position = value - Quaternion.Inverse(Object.transform.rotation) * offset;
		}
	}

	/// <summary> returns the sum of the health of all the ship's components  </summary>
	public float HP {
		get {
			float hp = 0;
			foreach (ShipPart part in Parts.AllParts) {
				hp += part.HP;
			}
			return hp;
		}
	}

	/// <summary> Returns the power of the Ship: More powerfull ships get taken more seriously </summary>
	public float Strength {
		get {
			float st = HP;
			return st;
		}
	}

	/// <summary> The parts of the ship </summary>
	public PartsCollection Parts { get; private set; }

	/// <summary> The Ammunition currently selected </summary>
	public Ammunition CurrentAmmo { get; set; }

	public Dictionary<Ammunition, uint> AmmoAmounts {
		get {
			Dictionary<Ammunition, uint> res = new Dictionary<Ammunition, uint>();
			foreach (AmmoBox box in Parts.GetAll<AmmoBox>()) {
				if (box.Ammunition != 0u) {
					if (res.ContainsKey(box.AmmoType)) res [box.AmmoType] += box.Ammunition;
					else res.Add(box.AmmoType, box.Ammunition);
				}
			}
			return res;
		}
	}

	public float Throttle {
		get {
			if (Parts.CountReal(typeof(Engine)) == 0) { return 0; }
			return Parts.GetAll<Engine>() [0].Throttle;
		}
		set {
			foreach (Engine engine in Parts.GetAll<Engine>()) {
				engine.Throttle = value;
			}
		}
	}

	/// <summary> The maximal acceleration of the craft </summary>
	public float MaxAcceleration {
		get {
			float force = 0;
			foreach (Engine eng in Parts.GetAll<Engine>()) {
				force += eng.MaxThrust;
			}
			return force / (float) Mass;
		}
	}

	public float Fuel {
		get {
			float fuel = 0f;
			foreach (FuelTank tank in Parts.GetAll<FuelTank>()) {
				if (tank.ismain) {
					fuel += tank.Fuel;
				}
			}
			return fuel;
		}
		set {
			FuelTank [] tanks = Parts.GetMainTanks();
			float fuel = value / (float) tanks.Length;
			foreach (FuelTank tank in tanks) {
				tank.Fuel = fuel;
			}
		}
	}

	public bool HasFuel { get; private set; }

	public float RCSFuel {
		get {
			float fuel = 0f;
			foreach (FuelTank tank in Parts.GetAll<FuelTank>()) {
				if (tank.isrcs) {
					fuel += tank.Fuel;
				}
			}
			return fuel;
		}
		set {
			FuelTank [] tanks = Parts.GetRCSTanks();
			float fuel = value / (float) tanks.Length;
			foreach (FuelTank tank in tanks) {
				tank.Fuel = fuel;
			}
		}
	}

	public bool HasRCSFuel { get; private set; }

	/// <summary> How much of the current ammunition is left </summary>
	public uint Ammo {
		get {
			return GetAmmo(CurrentAmmo);
		}
		set {
			// Not recommanded
			SubstractAmmo(CurrentAmmo, Ammo - value);
		}
	}

	public ushort Missiles {
		get {
			ushort _missiles = 0;
			foreach (MissileLauncher launcher in Parts.GetAll<MissileLauncher>()) {
				_missiles += (ushort) launcher.ReadyCount;
			}
			return _missiles;
		}
	}

	public float tot_hp, tot_fuel, tot_rcs_fuel, tot_ammo, tot_missiles;

	public float HPRatio { get { return HP / tot_hp; } }
	public float FuelRatio { get { return Fuel / tot_fuel; } }
	public float RCSFuelRatio { get { return RCSFuel / tot_rcs_fuel; } }
	public float AmmoRatio { get { return (float) Ammo / tot_ammo; } }
	public float MissileRatio { get { return Missiles / tot_missiles; } }

	#endregion

	///<summary> maximal radius around the ship </summary>
	public float radius;

	/// <param name="ship"> The gameobject representing the ship </param>
	public Ship (GameObject ship_obj, bool friendly, string name, int id=-1) : base(SceneObjectType.ship, id){
		Friendly = friendly;
		Object = ship_obj;		
		Name = name + ID.ToString("[0000]");
		Associated = new Target(this);

		Parts = new PartsCollection();

		MapCore.Active.ObjectSpawned(this);
		SceneGlobals.ship_collection.Add(this);

		os = new NMS.OS.OperatingSystem(null, this);
		os.Attached = this;
	}

	/// <summary> 
	///		Gets executed after initialisation.
	///		In the first frame of the game, but after the LateStart of the General execution
	///	</summary>
	public void LateStart () {
		List<Ammunition> ammo_list = new List<Ammunition>(AmmoAmounts.Keys);
		CurrentAmmo = ammo_list.Count > 0 ? ammo_list [0] : Ammunition.None;
		InitializeArrows(offset);

		HasFuel = HasRCSFuel = true;
		UpdateMass();

		tot_hp = HP;
		tot_fuel = Fuel;
		tot_rcs_fuel = RCSFuel;
		tot_ammo = Ammo;
		tot_missiles = Missiles;
	}

	/// <summary>
	///		To calculate the radius (Should be called in the beginning).
	///		Not a property, because this is pretty resource intesitive.
	///	</summary>
	public void CalculateRadius () {
		radius = 0;
		foreach (ShipPart part in Parts.AllParts) {
			MeshRenderer coll = part.OwnObject.GetComponent<MeshRenderer>();
			if (coll != null) {
				float extend = Mathf.Max((coll.bounds.min - Position).magnitude, (coll.bounds.max - Position).magnitude);
				if (extend > radius) {
					radius = extend;
				}
			}
		}
	}

	public void UpdateMass () {
		double mass = 0d;
		foreach (ShipPart part in Parts.AllParts) {
			mass += part.Mass;
			if (part is FuelTank) {
				mass += ((FuelTank) part).Fuel;
			}
		}
		Mass = mass;
	}

	/// <summary> Drains a given ammount of fuel </summary>
	/// <param name="ammount"> The ammount of fuel drained (in t) </param>
	public void DrainFuel (float ammount) {
		float fuel_before = Fuel;
		if (ammount > fuel_before) {
			Fuel = 0;
			HasFuel = false;
			Mass -= fuel_before;
		} else {
			Fuel = fuel_before - ammount;
			Mass -= ammount;
		}
	}

	/// <summary> Drains a given ammount of rcs fuel </summary>
	/// <param name="ammount"> The ammount of fuel drained (in t) </param>
	public void DrainRCSFuel (float ammount) {
		float fuel_before = RCSFuel;
		if (ammount > fuel_before) {
			Fuel = 0;
			HasRCSFuel = false;
			Mass -= fuel_before;
		} else {
			RCSFuel = fuel_before - ammount;
			Mass -= ammount;
		}
	}

	/// <summary> Removes a certain ammount of ammunition over the ammoboxes </summary>
	/// <param name="value"> The ammount of ammunition to remove </param>
	/// <returns> If there is enough ammo left </returns>
	public bool SubstractAmmo (Ammunition ammotype, uint value=1u) {
		AmmoBox [] boxes = System.Array.FindAll(Parts.GetAll<AmmoBox>(), x => x.AmmoType == ammotype);
		if (boxes.Length == 0) return false;
		while (true) {
			foreach (AmmoBox box in boxes) {
				if (value == 0u) return true;
				if (GetAmmo(ammotype) == 0u) return false;
				if (box.Ammunition > 0u) {
					box.Ammunition--;
					value--;
				}
			}
		}
	}

	public uint GetAmmo (Ammunition ammo_type) {
		uint _ammo = 0u;
		foreach (AmmoBox box in Parts.GetAll<AmmoBox>()) {
			if (box.AmmoType == ammo_type) {
				_ammo += box.Ammunition;
			}
		}
		return _ammo;
	}

	public float CalculateStrengthInDirection (Vector3 direction) {
		float res = 0;
		res += direction.x > 0 ? control_script.trans_strength [0].x : control_script.trans_strength [1].x;
		res += direction.y > 0 ? control_script.trans_strength [0].y : control_script.trans_strength [1].y;
		res += direction.z > 0 ? control_script.trans_strength [0].z : control_script.trans_strength [1].z;
		return res;
	}

	/// <summary>
	///		Returns angular acceleration in °*s^-2
	/// </summary>
	/// <param name="direction"> The axis aroud which the acceleration should be mesured </param>
	public float CalculateRotationStrength (Vector3 direction) {
		float res = 0;
		direction.Normalize();
		res += direction.x * direction.x > 0 ? control_script.torque_strength [0].x : control_script.torque_strength [1].x;
		res += direction.y * direction.y > 0 ? control_script.torque_strength [0].y : control_script.torque_strength [1].y;
		res += direction.z * direction.z > 0 ? control_script.torque_strength [0].z : control_script.torque_strength [1].z;
		return res / (float) Mass * 2;
	}

	/// <summary> Called every frame </summary>
	public void Update () {
		foreach (ShipPart part in Parts.AllParts) {
			part.PausedUpdate();
		}
		if (SceneGlobals.Paused) return;
		foreach (ShipPart part in Parts.AllParts) {
			if (!(part is Turret))
				part.Update();
		}
		foreach (TurretGroup group in TurretGroups) {
			group.Update();
		}
	}

	/// <summary> Destroys the ship </summary>
	public void Destroy () {
		Associated.OnDestroy();
		SceneGlobals.ship_collection.Remove(this);
		foreach (MissileLauncher ml in Parts.GetAll<MissileLauncher>()) {
			foreach (Missile m in ml.Ready) {
				m.Destroy();
			}
		}
		UnityEngine.Object.Destroy(Object);
	}

	/// <summary> Called if the game (un)pauses </summary>
	/// <param name="pause"> If the game pauses or unpauses </param>
	public void OnPause (bool pause) {
		rcs_script.OnPause(pause);
		foreach (ShipPart part in Parts.AllParts) {
			part.OnPause(pause);
		}
	}

	public override void PhysicsUpdate (float p_deltatime) {
		deltatime = p_deltatime;

		Velocity += Acceleration;
		Transform.position += Velocity * deltatime;

		AngularVelocity += AngularAcceleration;
		try {
			Transform.position += Orientation * offset;
			Orientation *= Quaternion.Euler(AngularVelocity * deltatime);
			Transform.position -= Orientation * offset;
		} catch (System.Exception) {
			DeveloppmentTools.Log("Angular Velocity: " + AngularVelocity);
		}
	}

	public override DataStructure Save (DataStructure ds) {
		// Save stuff
		ds.Set("config path", config_path);
		ds.Set<ushort>("type", 0);
		ds.Set("player", IsPlayer);
		ds.Set("friendly", Friendly);
		ds.Set("parent network", high_ai.Net.ID);
		ds.Set("code", LowLevelAI.Quack2MachineCode(low_ai.movement_quack));
		foreach (ShipPart part in Parts) {
			DataStructure sub_ds = new DataStructure(part.Name(), ds);
			part.Save(sub_ds);
		}
		foreach (TurretGroup group in TurretGroups) {
			DataStructure tg_datastructure = new DataStructure("turr-" + group.name, ds);
			group.Save(tg_datastructure);
		}
		return base.Save(ds);
	}

	public override string ToString() {
		return "Ship: " + Name;
	}

	public static explicit operator Target (Ship s) {
		return s.Associated;
	}

	public static explicit operator PartsCollection (Ship s) {
		return s.Parts;
	}
}

public class PartsCollection: 
IEnumerable
{
	public uint Count {
		get {
			uint count = 0u;
			foreach (ShipPart [] parts in lists.Values) {
				foreach (ShipPart part in parts) {
					if (part != null) {
						count++;
					}
				}
			}
			return count;
		}
	}

	/// <summary>
	///		Returns all the parts in the collection
	/// </summary>
	public ShipPart [] AllParts {
		get {
			List<ShipPart> partslist = new List<ShipPart>();
			foreach (ShipPart [] parts in lists.Values) {
				foreach (ShipPart part in parts) {
					if (part != null) {
						partslist.Add(part);
					}
				}
			}
			return partslist.ToArray();
		}
	}
	
	private Dictionary<Type, ShipPart []> lists = new Dictionary<Type, ShipPart []> ();

	public PartsCollection () {
		foreach (Type type in Globals.plugins.ship_parts) {
			int size = (int) PluginHandling.GetConstant(type, "max_on_ship");
			lists.Add(type, (ShipPart []) Array.CreateInstance(type, size));
		}
	}

	public ShipPart [] this [Type index] {
		get {
			return GetParts(index);
		}
	}

	public ShipPart this [int index] {
		get {
			return AllParts [index];
		}
		set {
			AllParts [index] = value;
		}
	}

	private ShipPart [] GetParts (Type type) {
		ShipPart [] arr = lists [type];
		List<ShipPart> ret_list = new List<ShipPart>();
		foreach (ShipPart part in arr) {
			if (part != null) {
				ret_list.Add(part);
			}
		}
		return ret_list.ToArray();
	}

	/// <summary>
	///		Adds a Part to the collection
	/// </summary>
	/// <param name="part"> The part to be added </param>
	public void Add (ShipPart part) {
		ShipPart [] arr = lists[part.GetType()];
		for (int i = 0; i < arr.Length; i++) {
			if (arr [i] == null) {
				arr [i] = part;
				return;
			}
		}
		throw new OverflowException("Array is full");
	}

	/// <summary>
	///		Removes a part from the collection
	/// </summary>
	/// <param name="part"> The part to be removed </param>
	public void Remove (ShipPart part) {
		ShipPart [] arr = lists[part.GetType()];
		for (int i = 0; i < arr.Length; i++) {
			if (arr [i] == part) {
				arr [i] = null;
				return;
			}
		}
	}

	/// <summary>
	///		Counts all non-null items for a type
	/// </summary>
	/// <param name="type"> the requested type as PartsOption </param>
	/// <returns> unsigned integer: the number of non-null types </returns>
	public uint CountReal (Type type) {
		uint counter = 0u;
		ShipPart [] arr = lists[type];
		foreach (ShipPart part in arr) {
			if (part != null) { counter++; }
		}
		return counter;
	}

	/// <typeparam name="T"> The requested type as type </typeparam>
	public uint CountReal<T> () where T : ShipPart{
		return CountReal(typeof(T));
	}

	/// <summary>
	///		Returns an array with all Main FuelTanks
	/// </summary>
	public FuelTank [] GetMainTanks () {
		List<FuelTank> tanks = new List<FuelTank>();
		foreach (FuelTank tank in GetParts(typeof(FuelTank))) {
			if (tank.ismain) {
				tanks.Add(tank);
			}
		}
		return tanks.ToArray();
	}

	/// <summary>
	///		Returns an array with all RCs Fuel tanks
	/// </summary>
	public FuelTank [] GetRCSTanks () {
		List<FuelTank> tanks = new List<FuelTank>();
		foreach (FuelTank tank in GetParts(typeof(FuelTank))) {
			if (tank.isrcs) {
				tanks.Add(tank);
			}
		}
		return tanks.ToArray();
	}

	/// <summary> Get all the Shipparts of a certain type </summary>
	/// <typeparam name="T"> The type searched for  </typeparam>
	/// <returns> An array of the parts </returns>
	public T [] GetAll<T> () where T : ShipPart {
		return Array.ConvertAll(GetParts(typeof(T)), item => (T) item);
	}

	public ShipPart [] GetAll (Type type) {
		return GetParts(type);
	}

	public override string ToString () {
		return DeveloppmentTools.LogIterable(AllParts);
	}

	public IEnumerator GetEnumerator () {
		return new PartEnumerator(AllParts);
	}

	private class PartEnumerator : IEnumerator
	{
		private int position = -1;

		private ShipPart[] array;

		public ShipPart Current {
			get {
				if (position < array.Length) {
					return array [position];
				} else {
					throw new ArgumentOutOfRangeException();
				}
			}
		}

		object IEnumerator.Current {
			get { return Current; }
		}

		public PartEnumerator(ShipPart[] parray) {
			array = parray;
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
