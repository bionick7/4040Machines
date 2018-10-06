using System.Collections.Generic;
using UnityEngine;

public class Ship : SceneObject
{
	public Target associated_target;
	public Target Target { get; set; }

	public TurretGroup [] TurretGroups {
		get { return control_script.turret_aims.ToArray(); }
	}

	public string name;
	public Vector3 offset;

	private ShipControl control_script;
	public bool virt = false;

	#region properties

	public bool IsPlayer {
		get { return Object == PlayerObj(); }
	}

	public override float Importance {
		get {
			if (SceneData.general.ignore_player && IsPlayer) return 0f;
			float imp = Strength / (HP * HP);
			return imp;
		}
	}

	new public Vector3 Position {
		get {
			if (!Exists) throw not_exist;
			return Object.transform.position + Object.transform.rotation * offset;
		}
		set {
			if (!Exists) throw not_exist;
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

	/// <summary> The current Ammunition </summary>
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
			if (Parts.CountReal(PartsOptions.engine) == 0) { return 0; }
			return Parts.GetAll<Engine>() [0].Throttle;
		}
		set {
			foreach (Engine engine in Parts.GetAll<Engine>()) {
				engine.Throttle = value;
			}
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

	public uint Ammo {
		get {
			uint _ammo = 0u;
			foreach (AmmoBox box in Parts.GetAll<AmmoBox>()) {
				if (box.AmmoType == CurrentAmmo) {
					_ammo += box.Ammunition;
				}
			}
			return _ammo;
		}
		set {
			// Not recommanded
			SubstractAmmo(CurrentAmmo, Ammo - value);
		}
	}

	public float Missiles {
		get {
			float _missiles = 0;
			foreach (MissileLauncher launcher in Parts.GetAll<MissileLauncher>()) {
				_missiles += launcher.ReadyCount;
			}
			return _missiles;
		}
	}

	private float tot_hp, tot_fuel, tot_rcs_fuel, tot_ammo, tot_missiles;

	public float HPRatio { get { return HP / tot_hp; } }
	public float FuelRatio { get { return Fuel / tot_fuel; } }
	public float RCSFuelRatio { get { return RCSFuel / tot_rcs_fuel; } }
	public float AmmoRatio { get { return (float) Ammo / tot_ammo; } }
	public float MissileRatio { get { return Missiles / tot_missiles; } }

	public override double Mass {
		get {
			double mass = 0d;
			foreach (ShipPart part in Parts.AllParts) {
				mass += part.Mass;
				if (part is FuelTank) {
					mass += ((FuelTank) part).Fuel;
				}
			}
			return mass;
		}
		set { }
	}

	#endregion

	///<summary> maximal radius around the ship </summary>
	public float radius;

	/// <param name="ship"> The gameobject representing the ship </param>
	public Ship (GameObject ship_obj) : base(SceneObjectType.ship){
		Object = ship_obj;
		associated_target = new Target(this);
		control_script = ship_obj.GetComponent<ShipControl>();

		name = Object.name;
		Parts = new PartsCollection();
		Target = Target.None;

		SceneData.ship_list.Add(this);
	}

	public void LateStart () {
		List<Ammunition> ammo_list = new List<Ammunition>(AmmoAmounts.Keys);
		CurrentAmmo = ammo_list.Count > 0 ? ammo_list [0] : Ammunition.None;

		tot_hp = HP;
		tot_fuel = Fuel;
		tot_rcs_fuel = RCSFuel;
		tot_ammo = Ammo;
		tot_missiles = Missiles;
	}

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

	/// <summary> Removes a certain ammount of ammunition over the ammoboxes </summary>
	/// <param name="value"> The ammount of ammunition to remove </param>
	/// <returns> If there is any ammo left </returns>
	public bool SubstractAmmo (Ammunition ammotype, uint value=1u) {
		while (true) {
			foreach (AmmoBox box in Parts.GetAll<AmmoBox>()) {
				if (Ammo == 0u) return false;
				if (value == 0u) return true;
				if (box.Ammunition > 0u && box.AmmoType == ammotype) {
					box.Ammunition--;
					value--;
				}
			}
		}
	}

	public void Update () {
		foreach (ShipPart part in Parts.AllParts) {
			part.Update();
		}
	}

	public void Destroy () {
		SceneData.ship_list.Remove(this);
		foreach (MissileLauncher ml in Parts.GetAll<MissileLauncher>()) {
			foreach (Missile m in ml.Ready) {
				m.Destroy();
			}
		}
		UnityEngine.Object.Destroy(Object);
	}

	public override string ToString() {
		return "Ship: " + name;
	}
}

public class PartsCollection
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

	public ShipPart [] this [PartsOptions index] {
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

	private ShipPart [] GetParts (PartsOptions type) {
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
	///		This Dictionnaary summarizes, which Partsoptions belong to which types
	/// </summary>
	public static Dictionary<System.Type, PartsOptions> Types_Options = new Dictionary<System.Type, PartsOptions>() {
		{ typeof(Main), PartsOptions.main },
		{ typeof(Weapon), PartsOptions.weapon},
		{ typeof(DockingPort), PartsOptions.docking_port },
		{ typeof(FuelTank), PartsOptions.fuel_tank },
		{ typeof(Engine), PartsOptions.engine },
		{ typeof(PowerReciever), PartsOptions.power_reciever },
		{ typeof(WeaponCooling), PartsOptions.weapon_cooling },
		{ typeof(LifeSupport), PartsOptions.life_support },
		{ typeof(Structure), PartsOptions.structure },
		{ typeof(Turret), PartsOptions.turret },
		{ typeof(AmmoBox), PartsOptions.ammobox },
		{ typeof(MissileLauncher), PartsOptions.missilelauncher }
	};

	public static Dictionary<PartsOptions, System.Type> Options_Types = new Dictionary<PartsOptions, System.Type>() {
		{ PartsOptions.main, typeof(Main) },
		{ PartsOptions.weapon, typeof(Weapon) },
		{ PartsOptions.docking_port, typeof(DockingPort) },
		{ PartsOptions.fuel_tank, typeof(FuelTank) },
		{ PartsOptions.engine, typeof(Engine) },
		{ PartsOptions.power_reciever, typeof(PowerReciever) },
		{ PartsOptions.weapon_cooling, typeof(WeaponCooling) },
		{ PartsOptions.life_support, typeof(LifeSupport) },
		{ PartsOptions.structure, typeof(Structure) },
		{ PartsOptions.turret, typeof(Turret) },
		{ PartsOptions.ammobox, typeof(AmmoBox) },
		{ PartsOptions.missilelauncher, typeof(MissileLauncher) }
	};

	private Dictionary<PartsOptions, ShipPart []> lists = new Dictionary<PartsOptions, ShipPart []> () {
		{ PartsOptions.main, new Main [20] },
		{ PartsOptions.weapon, new Weapon [100] },
		{ PartsOptions.docking_port, new DockingPort [20] },
		{ PartsOptions.fuel_tank, new FuelTank [100] },
		{ PartsOptions.engine, new Engine [100] },
		{ PartsOptions.power_reciever, new PowerReciever [20] },
		{ PartsOptions.weapon_cooling, new WeaponCooling [50] },
		{ PartsOptions.life_support, new LifeSupport [50] },
		{ PartsOptions.structure, new Structure [100] },
		{ PartsOptions.turret, new Turret [1000] },
		{ PartsOptions.ammobox, new AmmoBox [100] } ,
		{ PartsOptions.missilelauncher, new MissileLauncher [100] }
	};

	/// <summary>
	///		Adds a Part to the collection
	/// </summary>
	/// <param name="part"> The part to be added </param>
	public void Add (ShipPart part) {
		ShipPart [] arr = lists[part.parttype];
		for (int i = 0; i < arr.Length; i++) {
			if (arr [i] == null) {
				arr [i] = part;
				return;
			}
		}
		throw new System.OverflowException("Array is full");
	}

	/// <summary>
	///		Removes a part from the collection
	/// </summary>
	/// <param name="part"> The part to be removed </param>
	public void Remove (ShipPart part) {
		ShipPart [] arr = lists[part.parttype];
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
	public uint CountReal (PartsOptions type) {
		uint counter = 0u;
		ShipPart [] arr = lists[type];
		foreach (ShipPart part in arr) {
			if (part != null) { counter++; }
		}
		return counter;
	}

	/// <typeparam name="T"> The requested type as type </typeparam>
	public uint CountReal<T> () where T : ShipPart{
		return CountReal(Types_Options[typeof(T)]);
	}

	/// <summary>
	///		Returns an array with all Main FuelTanks
	/// </summary>
	public FuelTank [] GetMainTanks () {
		List<FuelTank> tanks = new List<FuelTank>();
		foreach (FuelTank tank in GetParts(PartsOptions.fuel_tank)) {
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
		foreach (FuelTank tank in GetParts(PartsOptions.fuel_tank)) {
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
		PartsOptions parttype = Types_Options [typeof(T)];
		return System.Array.ConvertAll(GetParts(parttype), item => (T) item);
	}

	public ShipPart [] GetAll (PartsOptions type) {
		return GetParts(type);
	}
}