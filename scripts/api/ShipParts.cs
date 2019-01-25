using System.Collections.Generic;
using UnityEngine;
using FileManagement;

/* ========================================
 * The parts, of wich a ship is made out of
 * ======================================== */

/// <summary> Basis for any part, a ship can have </summary>
public abstract class ShipPart : DestroyableObject, IAimable
{
	public const string enum_opt = "structure";
	public const int max_on_ship = 100;

	public string parttype;

	protected ShipControl parent_script;
	protected PartsCollection parent_collection;

	public float InitHP { get; protected set; }
	public bool main_component = true;

	public Vector3 Position {
		get { return Transform.position; }
	}

	public new bool Friendly {
		get { return ParentShip.Friendly; }
	}

	public DataStructure description_ds;

	public float Mass { get; protected set; }

	public Ship ParentShip { get; private set; }
	public GameObject ParentObject { get; private set; }
	
	public Transform Transform { get; set; }

	public static List<System.Type> PartTypes = new List<System.Type>();

	public ShipPart (float initial_health, GameObject obj, float mass) {
		OwnObject = obj;
		Transform = obj.transform;
		Mass = mass;

		ParentObject = obj.transform.root.gameObject;
		parent_script = ParentObject.GetComponent<ShipControl>();
		if (parent_script == null) {
			throw new System.NullReferenceException("No ShipControl here");
		}
		HP = InitHP = initial_health;
		ParentShip = parent_script.myship;
		parent_collection = ParentShip.Parts;
		parent_collection.Add(this);
	}

	public virtual void PausedUpdate () {

	}

	public virtual void Update () { }

	public override void Destroy () {
		base.Destroy();
		parent_collection.Remove(this);
		ParentShip.UpdateMass();
	}

	public virtual void OnPause (bool p) {

	}

	/// <summary> Returns a part </summary>
	/// <param name="initial_health"> The initial HP of the part </param>
	/// <param name="obj"> The object, this part is based on </param>
	/// <param name="type"> What kind of part is it? </param>
	/// <param name="mass"> The mass of teh part </param>
	public static ShipPart Get (float initial_health, GameObject obj, string type, float mass) {
		switch (type) {
		case "Main":
			return new Main(initial_health, obj, mass);

		case "Weapon":
			return new Weapon(initial_health, obj, mass);

		case "DockinPort":
			return new DockingPort(initial_health, obj, mass);

		case "FuelTank":
			return new FuelTank(initial_health, obj, mass);

		case "Engine":
			return new Engine(initial_health, obj, mass);

		case "PowerReciever":
			return new PowerSource(initial_health, obj, mass);

		case "WeaponCooling":
			return new WeaponCooling(initial_health, obj, mass);

		case "LifeSupport":
			return new LifeSupport(initial_health, obj, mass);

		default:
		case "Structure":
			return new Structure(initial_health, obj, mass);

		case "Turret":
			return new Turret(new float [4] { -1, -1, -1, -1 }, obj, new float [2] { 10, 10 }, initial_health, mass);

		case "AmmoBox":
			return new AmmoBox(initial_health, obj, mass);
		}
	}

	public override string ToString () {
		return string.Format("Shippart: {0}", OwnObject.ToString());
	}

	public virtual DataStructure Save (DataStructure ds) {
		if (description_ds != null) {
			ds.Set("hp", HP);
			ds.Set("main component", main_component);
			ds.Set("position", Quaternion.Inverse(ParentShip.Orientation) * (Position - ParentShip.Transform.position));
			ds.Set("rotation", Quaternion.Inverse(ParentShip.Orientation) * Transform.rotation);
			ds.Set("type", parttype);
			description_ds.Name = "description";
			ds.children.Add("description", description_ds);
		}
		return ds;
	}

	public virtual string Name () {
		return parttype;
	}

	public virtual string Description () {
		return ToString();
	}
}

/// <summary> The main part. If this dies, the whole ship dies </summary>
public class Main : ShipPart
{
	public new const string enum_opt = "main";
	public new const int max_on_ship = 20;

	public Main (float init_health, GameObject obj, float mass) : base(init_health, obj, mass) { }

	public override void Destroy () {
		base.Destroy();
		ParentShip.Destroy();
	}

	public override string Description () {
		return string.Format("Main Part {0:0.0} / {1:0.0} HP", HP, InitHP);
	}
}

/// <summary> Fixed weapons mounted on the ship </summary>
public class Weapon : ShipPart
{
	public new const string enum_opt = "weapon";
	public new const int max_on_ship = 100;

	public float explosion_force;

	public float heat = 0;
	public float ooo_time = 0;
	public float init_ooo_time = 3f;

	/// <summary> The object of the hull </summary>
	public GameObject empty_hull;
	/// <summary> The reload speed in seconds </summary>
	public float ReloadSpeed { get; set; }
	/// <summary> Where the bullets come out </summary>
	public Vector3 ShootPos { get; set; }
	/// <summary> Where the hulls come out </summary>
	public Vector3 EjectPos { get; set; }
	/// <summary> The muzzle velocity of the bullet </summary>
	public float BulletSpeed { get; set; }
	/// <summary> The velocity with wich the hull is ejected </summary>
	public float HullSpeed { get; set; }

	public Animator animator;

	private float reload_timer;
	public bool shooting;

	public Weapon (float init_health, GameObject obj, float mass, float p_explosion_force = 0f) : base(init_health, obj, mass) {
		explosion_force = p_explosion_force;
		animator = obj.GetComponent<Animator>();
	}

	public override void Destroy () {
		base.Destroy();
		new Explosion(explosion_force, OwnObject.transform.position);
	}

	/// <summary> Has to get called every frame once </summary>
	public override void Update () {
		base.Update();
		reload_timer += Time.deltaTime;

		if (heat > 0) {
			heat -= Time.deltaTime * .5f;
		}
		if (ooo_time > 0) {
			ooo_time -= Time.deltaTime;
		}

		if (shooting) {
			Shoot();
		}
	}

	public override void OnPause (bool p) {
		base.OnPause(p);
		if (animator != null)
			animator.SetBool("firing", !p & shooting);
	}

	/// <summary> Fires the weapon once </summary>
	public void Shoot () {
		if (reload_timer < ReloadSpeed * 2) { return; }
		Ammunition current_ammo = ParentShip.CurrentAmmo;
		if (!ParentShip.SubstractAmmo(current_ammo, 1u) || ooo_time > 0) { return; }
		if (heat > 1) {
			ooo_time = init_ooo_time;
			return;
		}

		// Spawn the bullet
		Bullet.Spawn(
			current_ammo,
			Transform.position + (Transform.rotation * ShootPos), 
			Transform.rotation,
			SceneGlobals.ReferenceSystem.RelativeVelocity(ParentShip.Velocity) + Transform.forward * BulletSpeed, 
			ParentShip.Friendly
		);

		//Spawn Objects
		Vector3 hull_spawn_pos = Transform.position + (Transform.rotation * EjectPos/2);
		GameObject hull_obj = Object.Instantiate(empty_hull, hull_spawn_pos, Transform.rotation * Quaternion.Euler(-90, 0, 0));
		hull_obj.name = "Hull: " + current_ammo.ToString();

		// Initialize the hull
		const double hull_mass = 2e-4;  // 200g
		Hull hull_inst = new Hull(hull_obj, hull_mass, SceneGlobals.ReferenceSystem.RelativeVelocity(ParentShip.Velocity));
		hull_inst.Velocity -= HullSpeed * EjectPos.normalized;
		hull_inst.AngularVelocity = HandyTools.RandomVector * 10f;

		HullAttachment hull_script = Loader.EnsureComponent<HullAttachment>(hull_obj);
		hull_script.instance = hull_inst;

		// Plays the sound 
		if (ParentShip.IsPlayer)
			Globals.audio.ShootingSound("gettling");

		reload_timer = 0.0f;

		heat += .05f;
	}

	public void Trigger_Shooting (bool start) {
		if (animator != null)
			animator.SetBool("firing", start);
		shooting = start;
	}

	public override string Description () {
		return string.Format("Weapon {0:0.0} / {1:0.0} HP", HP, InitHP);
	}

	public static Weapon GetFromDS (DataStructure part_data, DataStructure specific_data, Transform parent) {
		GameObject weapon_obj = part_data.Get<GameObject>("source");
		Vector3 position = specific_data.Get<Vector3>("position");
		Quaternion rotation = specific_data.Get<Quaternion>("rotation");

		GameObject act_weapon_obj = Object.Instantiate(weapon_obj);
		act_weapon_obj.transform.position = parent.position + parent.rotation * position;
		act_weapon_obj.transform.rotation = parent.rotation * rotation;
		act_weapon_obj.transform.SetParent(parent, true);

		Weapon weapon_instance = new Weapon((float) part_data.Get<ushort>("hp"), act_weapon_obj, part_data.Get<float>("mass")) {
			empty_hull = part_data.Get<GameObject>("hullpref"),
			BulletSpeed = part_data.Get<float>("bulletspeed"),
			HullSpeed = part_data.Get<float>("hullspeed"),
			ShootPos = part_data.Get<Vector3>("bulletpos"),
			EjectPos = part_data.Get<Vector3>("hullpos"),
			ReloadSpeed = part_data.Get<float>("reloadspeed"),
			description_ds = part_data,
		};

		weapon_instance.HP = specific_data.Get("hp", weapon_instance.InitHP, quiet: true);
		weapon_instance.main_component = specific_data.Get("main component", true, quiet: true);

		weapon_instance.heat = specific_data.Get("heat", 0f, quiet: true);
		weapon_instance.ooo_time = specific_data.Get("ooo time", 0f, quiet: true);
		weapon_instance.reload_timer = specific_data.Get("reload timer", 0f, quiet: true);
		weapon_instance.shooting = specific_data.Get("shooting", false, quiet: true);

		BulletCollisionDetection weapon_behaviour = Loader.EnsureComponent<BulletCollisionDetection>(act_weapon_obj);
		weapon_behaviour.Part = weapon_instance;

		return weapon_instance;
	}

	public override DataStructure Save (DataStructure ds) {
		ds.Set("heat", heat);
		ds.Set("ooo time", ooo_time);
		ds.Set("reload timer", reload_timer);
		ds.Set("shooting", shooting);
		return base.Save(ds);
	}
}

/// <summary> Docking port, to dock to other ships </summary>
public class DockingPort : ShipPart
{
	public new const string enum_opt = "docking_port";
	public new const int max_on_ship = 20;

	public DockingPort (float init_health, GameObject obj, float mass) : base(init_health, obj, mass) { }

	public override string Description () {
		return string.Format("Docking port {0:0.0} / {1:0.0} HP", HP, InitHP);
	}
}

/// <summary> Holds fule (rcs or mian) </summary>
public class FuelTank : ShipPart
{
	public new const string enum_opt = "fuel_tank";
	public new const int max_on_ship = 100;

	public float Fuel { get; set; }
	public float TotFuel { get; set; }
	public bool isrcs;
	public bool ismain;

	public float explosion_force;

	public FuelTank (float init_health, GameObject obj, float mass, float p_explosion_force=0f) : base(init_health, obj, mass) {
		explosion_force = p_explosion_force;
		Fuel = 0f;
	}

	public override string ToString () {
		return "Fueltank(" + Fuel.ToString() + "t)";
	}

	public override void Destroy () {
		base.Destroy();
		new Explosion(explosion_force, ParentObject.transform.position);
	}

	public override string Description () {
		return ToString();
	}

	public static FuelTank GetFromDS (DataStructure part_data, DataStructure specific_data, Transform parent) {
		GameObject tank_obj = Object.Instantiate(part_data.Get<GameObject>("source"));
		tank_obj.transform.position = parent.position + parent.rotation * specific_data.Get<Vector3>("position");
		tank_obj.transform.rotation = parent.rotation * specific_data.Get<Quaternion>("rotation");
		tank_obj.transform.SetParent(parent, true);

		FuelTank tank_instance = new FuelTank((float) part_data.Get<ushort>("hp"), tank_obj, part_data.Get<float>("mass")) {
			isrcs = part_data.Get<bool>("rcs"),
			ismain = part_data.Get<bool>("main"),
			TotFuel = part_data.Get<float>("fuel"),
			description_ds = part_data,
		};

		tank_instance.Fuel = specific_data.Get("fuel amount", tank_instance.TotFuel, quiet: true);
		tank_instance.HP = specific_data.Get("hp", tank_instance.InitHP, quiet: true);
		tank_instance.main_component = specific_data.Get("main", true, quiet: true);	

		BulletCollisionDetection tank_behaviour = Loader.EnsureComponent<BulletCollisionDetection>(tank_obj);
		tank_behaviour.Part = tank_instance;

		return tank_instance;
	} 

	public override DataStructure Save (DataStructure ds) {
		ds.Set("fuel amount", Fuel);
		return base.Save(ds);
	}
}

/// <summary> Engine of the Spaceship, drives it forward </summary>
public class Engine : ShipPart
{
	public new const string enum_opt = "engine";
	public new const int max_on_ship = 100;

	public float MaxThrust { get; set; }
	public float SpecificImpulse { get; set; }
	public Vector3 Direction {
		get { return - Transform.forward; }
	}

	public float FuelDrain {
		get { return (MaxThrust * Time.deltaTime) / (SpecificImpulse * 9.81f); }
	}

	private float _throttle;

	public float Throttle {
		get { return _throttle; }
		set {
			if (value < 0 || value > 1) {
				throw new System.ArgumentException(" value must be betwen 0 and 1 ");
			}
			_throttle = value;
		}
	}

	public float Thrust {
		get { return ParentShip.Fuel > 0f ? Throttle * MaxThrust: 0f; }
	}

	private ParticleSystem particlesystem;
	private ParticleSystem.EmissionModule emitter;

	public Engine (float init_health, GameObject obj, float mass, float maxthrust=0) : base(init_health, obj, mass) {
		MaxThrust = maxthrust;
		SpecificImpulse = 900f;

		particlesystem = OwnObject.GetComponentInChildren<ParticleSystem>();
		if (particlesystem == null) {
			throw new System.NotImplementedException("No particle system on here");
		} else {
			emitter = particlesystem.emission;
		}
	}

	public override void OnPause (bool p) {
		base.OnPause(p);
		if (p) particlesystem.Pause();
		else particlesystem.Play();
	}

	public override DataStructure Save (DataStructure ds) {
		ds.Set("throttle", Throttle);
		return base.Save(ds);
	}

	public override void PausedUpdate () {
		base.PausedUpdate();
		if (particlesystem != null) {
			emitter.rateOverTime = 100f * Throttle;
		}
	}

	public override void Update () {
		if (!Exists) return;
		base.Update();
		if (particlesystem.isPaused) particlesystem.Play();

		ParentShip.DrainFuel(FuelDrain * Throttle);
		ParentShip.Push(Direction * Thrust);
	}
	
	public static Engine GetFromDS (DataStructure part_data, DataStructure specific_data, Transform parent) {
		GameObject engine_obj = Object.Instantiate(part_data.Get<GameObject>("source"));
		engine_obj.transform.position = parent.position + parent.rotation * specific_data.Get<Vector3>("position");
		engine_obj.transform.rotation = parent.rotation * specific_data.Get<Quaternion>("rotation");
		engine_obj.transform.SetParent(parent, true);

		float hp = (float) part_data.Get<ushort>("hp");

		Engine engine_instance = new Engine(hp, engine_obj, part_data.Get<float>("mass"), part_data.Get<float>("thrust")) {
			SpecificImpulse = part_data.Get<float>("isp"),
			description_ds = part_data,
		};

		engine_instance.HP = specific_data.Get("hp", engine_instance.InitHP, quiet: true);
		engine_instance.main_component = specific_data.Get("main component", true, quiet: true);
		engine_instance.Throttle = specific_data.Get("throttle", 0, quiet: true);


		BulletCollisionDetection engine_behaviour = Loader.EnsureComponent<BulletCollisionDetection>(engine_obj);
		engine_behaviour.Part = engine_instance;

		return engine_instance;
	}

	public override string Description () {
		return string.Format("Engine {0:0.0} / {1:0.0} HP", HP, InitHP);
	}
}

/// <summary> Ensures, the ship get power, can be a solar panel, a reactor or an antenna </summary>
public class PowerSource : ShipPart
{
	public new const string enum_opt = "power_reciever";
	public new const int max_on_ship = 20;

	public Source source;

	public bool Radioactive {
		get { return source == Source.fission || source == Source.radioisotopes; }
	}

	public PowerSource (float init_health, GameObject obj, float mass) : base(init_health, obj, mass) { }

	public enum Source {
		antenna,
		battery,
		photovoltaic,
		fuel_cell,
		fission,
		fusion,
		radioisotopes
	}

	public override string Description () {
		return string.Format("PowerSource {0:0.0} / {1:0.0} HP", HP, InitHP);
	}
}

/// <summary> Cools the weapon, so it doesn't overheat </summary>
public class WeaponCooling : ShipPart
{
	public new const string enum_opt = "weapon_cooling";
	public new const int max_on_ship = 50;

	public WeaponCooling (float init_health, GameObject obj, float mass) : base(init_health, obj, mass) { }

	public override string Description () {
		return string.Format("Weapon Cooling {0:0.0} / {1:0.0} HP", HP, InitHP);
	}
}

/// <summary> Life support for the pilot/crew </summary>
public class LifeSupport : ShipPart
{
	public new const string enum_opt = "life_support";
	public new const int max_on_ship = 50;

	public LifeSupport (float init_health, GameObject obj, float mass) : base(init_health, obj, mass) { }

	public override string Description () {
		return string.Format("Life Support {0:0.0} / {1:0.0} HP", HP, InitHP);
	}
}

/// <summary> Just there, doaes nothing </summary>
public class Structure : ShipPart
{
	public new const string enum_opt = "structure";
	public new const int max_on_ship = 100;

	public Structure (float init_health, GameObject obj, float mass) : base(init_health, obj, mass) { }

	public override string Description () {
		return string.Format("Structure {0:0.0} / {1:0.0} HP", HP, InitHP);
	}
}

/// <summary> Contains the ammunition for the fixed wepons </summary>
public class AmmoBox : ShipPart
{
	public new const string enum_opt = "ammobox";
	public new const int max_on_ship = 100;

	public Ammunition AmmoType { get; set; }

	private uint _ammunition = 0u;

	/// <summary> What kind of bullets does this Box hold? </summary>
	public uint Ammunition {
		get { return _ammunition; }
		set {
			if (_ammunition == 0u) {
				FullAmmunition = value;
			}
			_ammunition = value;
		}
	}

	public uint FullAmmunition { get; private set; }
	

	public AmmoBox (float init_health, GameObject obj, float mass) : base(init_health, obj, mass) { }

	public static AmmoBox GetFromDS(DataStructure part_data, DataStructure specific_data, Transform parent) {
		GameObject box_obj = Object.Instantiate(part_data.Get<GameObject>("source"));
		box_obj.transform.position = parent.position + parent.rotation * specific_data.Get<Vector3>("position");
		box_obj.transform.rotation = parent.rotation * specific_data.Get<Quaternion>("rotation");
		box_obj.transform.SetParent(parent, true);

		if (!Globals.ammunition_insts.ContainsKey(part_data.Get<string>("ammotype"))) throw new System.Exception(string.Format("No such ammo: {0}", part_data.Get<string>("ammotype")));

		AmmoBox box_instance = new AmmoBox((float) part_data.Get<ushort>("hp"), box_obj, part_data.Get<float>("mass")) {
			AmmoType = Globals.ammunition_insts[part_data.Get<string>("ammotype")],
			Ammunition = part_data.Get<System.UInt16>("ammo"),
			description_ds = part_data,
		};

		box_instance.HP = specific_data.Get("hp", box_instance.InitHP, quiet: true);
		box_instance.main_component = specific_data.Get("main component", true, quiet: true);

		box_instance.Ammunition = (uint) specific_data.Get("ammo", (int) box_instance.FullAmmunition, quiet: true);

		BulletCollisionDetection box_behaviour = Loader.EnsureComponent<BulletCollisionDetection>(box_obj);
		box_behaviour.Part = box_instance;

		return box_instance;
	}

	public override DataStructure Save (DataStructure ds) {
		ds.Set("ammo", (int) Ammunition);
		return base.Save(ds);
	}

	public override string Description () {
		return string.Format("Ammunition Box {0:0.0} / {1:0.0} HP", HP, InitHP);
	}
}

/// <summary> Spawns and launches missiles </summary>
public class MissileLauncher : ShipPart
{
	public new const string enum_opt = "missilelauncher";
	public new const int max_on_ship = 100;

	public Quaternion orientation = Quaternion.identity;
	public Vector3 [] Positions { get; set; }
	public DSPrefab missile_source;
	public float acceleration;
	public float flight_duration;
	public Missile.Warhead warhead;
	public float missile_mass;

	/// <summary> List of all the missiles spawned on the launcher </summary>
	private Missile [] missiles;

	/// <summary> List of all the missiles currently on the launcher </summary>
	public Missile [] Ready {
		get {
			List<Missile> ready = new List<Missile>();
			foreach (Missile missile in missiles) {
				if (!missile.Released && missile.Exists) {
					ready.Add(missile);
				}
			}
			return ready.ToArray();
		}
	}

	/// <summary> The number of ready missiles </summary>
	public uint ReadyCount {
		get {
			uint count = 0u;
			foreach (Missile missile in missiles) {
				if (!missile.Released && missile.Exists) {
					count++;
				}
			}
			return count;
		}
	}

	/// <summary> List of all the released missiles </summary>
	public Missile [] Flying {
		get {
			List<Missile> flying = new List<Missile>();
			foreach (Missile missile in missiles) {
				if (missile.Released && missile.Exists) {
					flying.Add(missile);
				}
			}
			return flying.ToArray();
		}
	}

	public MissileLauncher (float init_health, GameObject obj, float mass) : base(init_health, obj, mass) {

	}

	/// <summary> Spawns all the missiles </summary>
	public void Spawn () {
		missiles = new Missile [Positions.Length];
		for (int i=0; i < Positions.Length; i++) {
			GameObject missile_obj = Object.Instantiate(missile_source.obj);
			missile_obj.transform.position = OwnObject.transform.position + OwnObject.transform.rotation * Positions [i];
			missile_obj.transform.rotation = OwnObject.transform.rotation * orientation;
			missile_obj.transform.SetParent(OwnObject.transform, true);

			Missile missile_instance = new Missile(missile_obj, flight_duration, missile_mass) {
				AimTarget = Target.None,
				EngineAcceleration = acceleration,
				Head = warhead,
				source = missile_source
			};
			missiles [i] = missile_instance;
		}
	}

	/// <summary> Fires one missile </summary>
	/// <returns> True, if the firing was succesfull </returns>
	public bool Fire () {
		if (Ready.Length == 0) {
			return false;
		}
		Missile missile = Ready [0];
		missile.Release();
		missile.Object.transform.SetParent(null);
		// IDK why this works...
		// "2" is confusing
		// Could be a problem with physics
		missile.Velocity = 2 * ParentShip.Velocity;
		return true;
	}

	public override void Update () {
		base.Update();
	}

	public static MissileLauncher GetFromDS (DataStructure part_data, DataStructure specific_data, Transform parent) {
		if (part_data.Get<GameObject>("source") == null) {
			Debug.Log(part_data);
		}

		GameObject launcher_obj = Object.Instantiate(part_data.Get<GameObject>("source"));
		launcher_obj.transform.position = parent.position + parent.rotation * specific_data.Get<Vector3>("position");
		launcher_obj.transform.rotation = parent.rotation * specific_data.Get<Quaternion>("rotation");
		launcher_obj.transform.SetParent(parent, true);

		MissileLauncher launcher_instance = new MissileLauncher((float) part_data.Get<ushort>("hp"), launcher_obj, part_data.Get<float>("mass")) {
			missile_source = part_data.Get<DSPrefab>("missile source"),
			missile_mass = part_data.Get<float>("missile mass"),
			Positions = specific_data.Get("positions", part_data.Get<Vector3[]>("positions"), quiet: true),
			orientation = part_data.Get<Quaternion>("orientation"),
			acceleration = part_data.Get<float>("acceleration"),
			flight_duration = part_data.Get<float>("duration"),
			description_ds = part_data,
		};

		launcher_instance.HP = specific_data.Get("hp", launcher_instance.InitHP, quiet: true);
		launcher_instance.main_component = specific_data.Get("main component", true, quiet: true);

		BulletCollisionDetection launcher_behaviour = Loader.EnsureComponent<BulletCollisionDetection>(launcher_obj);
		launcher_behaviour.Part = launcher_instance;

		launcher_instance.Spawn();
		return launcher_instance;
	}

	public override DataStructure Save (DataStructure ds) {
		ds.Set("positions", System.Array.ConvertAll(Ready, x => Quaternion.Inverse(Transform.rotation) * (x.Position - Position)));
		return base.Save(ds);
	}

	public override string Description () {
		return string.Format("Missile Launcher {0:0.0} / {1:0.0} HP", HP, InitHP);
	}
}

/// <summary>
///		A specific point relative to a part
/// </summary>
public struct PartOffsetAim: IAimable
{
	public ShipPart ParentPart { get; set; }

	public Vector3 offset_position;

	public Vector3 Position {
		get { return ParentPart.Transform.rotation * -offset_position + ParentPart.Transform.position; }
	}

	public bool Exists {
		get { return ParentPart.Exists; }
	}

	public bool Friendly {
		get { return ParentPart.Friendly; }
	}

	public PartOffsetAim (Vector3 p_position, ShipPart p_part) {
		ParentPart = p_part;
		offset_position = Quaternion.Inverse(p_part.Transform.rotation) * (p_part.Transform.position - p_position);
	}
}