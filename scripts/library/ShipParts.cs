using System.Collections.Generic;
using UnityEngine;

public abstract class ShipPart : DestroyableObject
{
	public const PartsOptions enum_opt = PartsOptions.structure;

	public PartsOptions parttype;

	protected ShipControl parent_script;
	protected PartsCollection parent_collection;

	public float InitHP { get; protected set; }
	public bool main_component = true;

	public float Mass { get; protected set; }

	public Ship ParentShip { get; private set; }
	public GameObject ParentObject { get; private set; }

	//protected Rigidbody ParentRigidbody { get; private set; }
	public Transform Transform { get; set; }

	public ShipPart (float initial_health, GameObject obj, PartsOptions type, float mass) {
		OwnObject = obj;
		Transform = obj.transform;
		parttype = type;
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
		//ParentRigidbody = ParentObject.GetComponent<Rigidbody>();
		side = ParentShip.side;
	}

	/// <summary> If hit by a bullet </summary>
	/// <param name="hit"> The bullet </param>
	public new void Hit (Bullet hit) {
		float dammage = Bullet.Dammage(hit, ParentShip.Velocity, ParentShip.side);
		HP -= dammage;

		if (HP <= 0f && main_component) {
			Destroy();
		}
	}

	/// <summary> If hit by a bullet </summary>
	/// <param name="hit"> The bullet </param>
	public new void Hit (Missile hit) {
		Object.Destroy(hit.Object);
		float dammage = hit.Dammage(ParentShip.side);
		HP -= dammage;

		hit.Explode();

		if (HP <= 0f && main_component) {
			Destroy();
		}
	}

	public virtual new void Destroy () {
		Object.Destroy(OwnObject);
	}

	public virtual void Update () {
		if (!OwnObject) {
			parent_collection.Remove(this);
		}
	}

	public static ShipPart Get (float initial_health, GameObject obj, PartsOptions type, float mass) {
		/*
		System.Type systype = PartsCollection.Options_Types[type];
		System.Reflection.ConstructorInfo constructor = systype.GetConstructor(new System.Type [4] { typeof(float), typeof(GameObject) ,typeof(PartsOptions), typeof(float) } );
		ShipPart instance = (ShipPart) constructor.Invoke(new object[4] { initial_health, obj, type, mass });
		return instance;
		*/

		switch (type) {
		case PartsOptions.main:
			return new Main(initial_health, obj, mass);

		case PartsOptions.weapon:
			return new Weapon(initial_health, obj, mass);

		case PartsOptions.docking_port:
			return new DockingPort(initial_health, obj, mass);

		case PartsOptions.fuel_tank:
			return new FuelTank(initial_health, obj, mass);

		case PartsOptions.engine:
			return new Engine(initial_health, obj, mass);

		case PartsOptions.power_reciever:
			return new PowerReciever(initial_health, obj, mass);

		case PartsOptions.weapon_cooling:
			return new WeaponCooling(initial_health, obj, mass);

		case PartsOptions.life_support:
			return new LifeSupport(initial_health, obj, mass);

		case PartsOptions.structure:
			return new Structure(initial_health, obj, mass);

		case PartsOptions.turret:
			return new Turret(new float [4] { -1, -1, -1, -1 }, obj, new float [2] { 10, 10 }, initial_health, mass);

		case PartsOptions.ammobox:
			return new AmmoBox(initial_health, obj, mass);

		default:
			return null;
		}
	}

	public override string ToString () {
		return string.Format("Shippart: {0}", OwnObject.ToString());
	}
}

/// <summary> The main part. If this dies, the whole ship dies </summary>
public class Main : ShipPart
{
	public new const PartsOptions enum_opt = PartsOptions.main;

	public Main (float init_health, GameObject obj, float mass) : base(init_health, obj, PartsOptions.main, mass) { }

	public override void Destroy () {
		base.Destroy();
		ParentShip.Destroy();
	}
}

/// <summary> Fixed weapons mounted on the ship </summary>
public class Weapon : ShipPart
{
	public new const PartsOptions enum_opt = PartsOptions.weapon;

	public Explosion explosion;

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

	private AudioSource audio;

	public Animator animator;

	private float reload_timer;
	public bool shooting;

	public Weapon (float init_health, GameObject obj, float mass, float explosion_force = 0f) : base(init_health, obj, PartsOptions.weapon, mass) {
		explosion = new Explosion(explosion_force);
		animator = obj.GetComponent<Animator>();
		audio = obj.GetComponent<AudioSource>();
	}

	public override void Destroy () {
		base.Destroy();
		explosion.Boom(OwnObject.transform.position);
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

	/// <summary> Fires the weapon once </summary>
	public void Shoot () {
		if (reload_timer < ReloadSpeed * 2) { return; }
		Ammunition current_ammo = ParentShip.CurrentAmmo;
		if (!ParentShip.SubstractAmmo(current_ammo, 1u) || ooo_time > 0) { return; }
		if (heat > 1) {
			ooo_time = init_ooo_time;
			return;
		}

		GameObject bullet = current_ammo.Source;

		//Spawn Objects
		Vector3 bullet_spawn_pos = Transform.position + (Transform.rotation * ShootPos);
		Vector3 hull_spawn_pos = Transform.position + (Transform.rotation * EjectPos/2);
		GameObject bullet_obj = Object.Instantiate(bullet, bullet_spawn_pos, Transform.rotation);
		GameObject hull_obj = Object.Instantiate(empty_hull, hull_spawn_pos, Transform.rotation * Quaternion.Euler(-90, 0, 0));
		Object.Destroy(hull_obj, 3f);

		//Match velocitys with ship
		Rigidbody rb_hull = hull_obj.GetComponent<Rigidbody>();

		Bullet bullet_inst = new Bullet(bullet_obj, current_ammo, ParentShip.side,
										ParentShip.Velocity + Transform.forward * BulletSpeed);

		BulletDamage bullet_script = Loader.EnsureComponent<BulletDamage>(bullet_obj);
		bullet_script.instance = bullet_inst;
		rb_hull.velocity = ParentShip.Velocity;


		//Add force to hull, bullet and ship
		float hull_force = rb_hull.mass * HullSpeed / Time.fixedDeltaTime;
		rb_hull.AddRelativeForce(EjectPos * hull_force);
		rb_hull.AddTorque(new Vector3(Random.value, Random.value, Random.value) * 10f);

		// Plays the sound 
		if (audio != null) {
			audio.PlayOneShot(audio.clip , .2f);
		}

		reload_timer = 0.0f;

		heat += .05f;
	}

	public void Trigger_Shooting (bool start) {
		if (animator != null) {
			animator.SetBool("firing", start);
		}
		shooting = start;
	}
}

/// <summary> Docking port, to dock to other ships </summary>
public class DockingPort : ShipPart
{
	public new const PartsOptions enum_opt = PartsOptions.docking_port;

	public DockingPort (float init_health, GameObject obj, float mass) : base(init_health, obj, PartsOptions.docking_port, mass) { }
}

/// <summary> Holds fule (rcs or mian) </summary>
public class FuelTank : ShipPart
{
	public new const PartsOptions enum_opt = PartsOptions.fuel_tank;

	public float Fuel { get; set; }
	public float TotFuel { get; set; }
	public bool isrcs;
	public bool ismain;

	public Explosion explosion;

	public FuelTank (float init_health, GameObject obj, float mass, float explosion_force=0f) : base(init_health, obj, PartsOptions.fuel_tank, mass) {
		explosion = new Explosion(explosion_force);
		Fuel = 0f;
	}

	public override string ToString () {
		return "Fueltank(" + Fuel.ToString() + "t)";
	}

	public override void Destroy () {
		base.Destroy();
		explosion.Boom(ParentObject.transform.position);
	}
}

/// <summary> Engine of the Spaceship, drives it forward </summary>
public class Engine : ShipPart
{
	public new const PartsOptions enum_opt = PartsOptions.engine;

	public float MaxThrust { get; set; }
	public float SpecificImpulse { get; set; }
	public Vector3 Direction { get; set; }

	public float FuelDrain {
		get {
			return (MaxThrust * Time.deltaTime) / (SpecificImpulse * 9.81f);
		}
	}

	private float _throttle;

	public float Throttle {
		get {
			return _throttle;
		}
		set {
			if (value < 0 || value > 1) {
				throw new System.ArgumentException(" value must be betwen 0 and 1 ");
			}
			_throttle = value;
		}
	}

	public float Thrust {
		get {
			return ParentShip.Fuel > 0f ? Throttle * MaxThrust: 0f;
		}
	}

	private ParticleSystem particlesystem;
	private ParticleSystem.EmissionModule emitter;

	public Engine (float init_health, GameObject obj, float mass, float maxthrust=0) : base(init_health, obj, PartsOptions.engine, mass) {
		MaxThrust = maxthrust;
		SpecificImpulse = 900f;
		Direction = Vector3.forward;

		particlesystem = OwnObject.GetComponentInChildren<ParticleSystem>();
		if (particlesystem == null) {
			throw new System.NotImplementedException("No particle system on here");
		} else {
			emitter = particlesystem.emission;
		}
	}

	public override void Update () {
		base.Update();

		ParentShip.Fuel -= FuelDrain * Throttle;
		ParentShip.Push(Direction * Thrust);

		if (particlesystem != null) {
			emitter.rateOverTime = 100f * Throttle;
		}
	}
}

/// <summary> Ensures, the ship get power, can be a solar panel, a reactor or an antenna </summary>
public class PowerReciever : ShipPart
{
	public new const PartsOptions enum_opt = PartsOptions.power_reciever;

	public Source source;

	public bool Radioactive {
		get {
			return source == Source.fission || source == Source.radioisotopes;
		}
	}

	public PowerReciever (float init_health, GameObject obj, float mass) : base(init_health, obj, PartsOptions.power_reciever, mass) { }

	public enum Source {
		antenna,
		battery,
		photovoltaic,
		fuel_cell,
		fission,
		fusion,
		radioisotopes
	}
}

/// <summary> Cools the weapon, so it doesn't overheat </summary>
public class WeaponCooling : ShipPart
{
	public new const PartsOptions enum_opt = PartsOptions.weapon_cooling;

	public WeaponCooling (float init_health, GameObject obj, float mass) : base(init_health, obj, PartsOptions.weapon_cooling, mass) { }
}

/// <summary> Life support for the pilot </summary>
public class LifeSupport : ShipPart
{
	public new const PartsOptions enum_opt = PartsOptions.life_support;

	public LifeSupport (float init_health, GameObject obj, float mass) : base(init_health, obj, PartsOptions.life_support, mass) { }
}

/// <summary> Just there, doaes nothing </summary>
public class Structure : ShipPart
{
	public new const PartsOptions enum_opt = PartsOptions.structure;

	public Structure (float init_health, GameObject obj, float mass) : base(init_health, obj, PartsOptions.structure, mass) { }
}

/// <summary> Contains the ammunition for the fixed wepons </summary>
public class AmmoBox : ShipPart
{
	public new const PartsOptions enum_opt = PartsOptions.ammobox;

	public Ammunition AmmoType { get; set; }

	private uint _ammunition = 0u;
	public uint Ammunition {
		get {
			return _ammunition;
		}
		set {
			if (_ammunition == 0u) {
				FullAmmunition = value;
			}
			_ammunition = value;
		}
	}

	public uint FullAmmunition { get; private set; }
	

	public AmmoBox (float init_health, GameObject obj, float mass) : base(init_health, obj, PartsOptions.ammobox, mass) { }
}

/// <summary> Spawns and launches missiles </summary>
public class MissileLauncher : ShipPart
{
	public new const PartsOptions enum_opt = PartsOptions.missilelauncher;

	public Quaternion orientation = Quaternion.identity;
	public Vector3 [] Positions { get; set; }
	public GameObject missile_source;
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

	public MissileLauncher (float init_health, GameObject obj, float mass) : base(init_health, obj, PartsOptions.missilelauncher, mass) {

	}

	public void Spawn () {
		missiles = new Missile [Positions.Length];
		for (int i=0; i < Positions.Length; i++) {
			GameObject missile_obj = Object.Instantiate(missile_source);
			missile_obj.transform.position = OwnObject.transform.position + OwnObject.transform.rotation * Positions [i];
			missile_obj.transform.rotation = OwnObject.transform.rotation * orientation;
			missile_obj.transform.SetParent(OwnObject.transform, true);

			//Rigidbody rb = missile_obj.AddComponent<Rigidbody>();
			//rb.mass = missile_mass;
			//rb.drag = rb.angularDrag = 0f;
			//rb.useGravity = false;

			Missile missile_instance = new Missile(missile_obj, flight_duration, missile_mass) {
				Target = Target.None,
				EngineAcceleration = acceleration,
				Head = warhead
			};
			missiles [i] = missile_instance;
		}
	}

	public bool Fire () {
		if (Ready.Length == 0) {
			return false;
		}
		Missile missile = Ready [0];
		missile.Release();
		missile.Object.transform.SetParent(null);
		missile.Target = ParentShip.Target;
		missile.Velocity = ParentShip.Velocity;
		return true;
	}

	public override void Update () {
		base.Update();
	}
}