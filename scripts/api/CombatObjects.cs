using System.Collections.Generic;
using FileManagement;
using UnityEngine;

/* =========================================================
 * Contains the SceneObjecs, like bullets and missile, which
 * are there to hurt someone.
 * ========================================================= */

public struct Bullet : IPhysicsObject, ISavable
{
	public Ammunition ammo;
	public bool Friendly { get; set; }

	#region Physics dependencies
	public Vector3 Velocity { get; set;	}
	public Vector3 Position {
		get { return objct.transform.position;  }
		set { objct.transform.position = value;  }
	}
	public Vector3 Acceleration {
		get { return Vector3.zero; }
		set {
			DeveloppmentTools.Log("Cannot set acceleration of bullet");
		}
	}
	public double Mass {
		get { return ammo.mass; }
		set { ammo.mass = (float) value; }
	}
	public Vector3 AngularVelocity {
		get { return Vector3.zero; }
		set { 
			DeveloppmentTools.Log("Cannot set angular velocity of bullet");
		}
	}
	public Quaternion Orientation {
		get { return objct.transform.rotation; }
		set { objct.transform.rotation = value; }
	}

	public void PhysicsUpdate (float deltatime) {
		Position += Velocity * deltatime;
	}

	public void Torque (Vector3 t) { }
	public void Push (Vector3 force) { }
	#endregion

	public bool Exists {
		get { return objct; }
	}

	public string Name { get; private set; }

	/// <summary> The object, to which the bullet is linked </summary>
	public GameObject objct;

	public Bullet (GameObject obj, Ammunition ammochoice, bool p_friendly, Vector3 vel) {
		ammo = ammochoice;
		Friendly = p_friendly;
		objct = obj;
		Velocity = vel;
		Name = string.Format("Bullet {0}", SceneGlobals.bullet_collection.Count);

		SceneGlobals.bullet_collection.Add(this);
		SceneGlobals.physics_objects.Add(this);
	}

	/// <summary> The kinetic enery released by a bullet hitting something </summary>
	/// <param name="bullet"> The bullet in question </param>
	/// <param name="velocity"> The velocity of the target </param>
	/// <param name="is_friendly"> If the target is friendly /returns>
	public static float KineticDammage (Bullet bullet, Vector3 velocity, bool is_friendly) {
		if (!(is_friendly ^ bullet.Friendly)) {
			return 0;
		}

		Ammunition ammo = bullet.ammo;
		float damage = ammo.default_damage;
		if (ammo.IsKinetic) {
			damage += .5f * ammo.mass * (bullet.Velocity - velocity).sqrMagnitude;
		}

		return damage;
	}

	/// <summary> The pure dammage dealt by the bullet (Explosion dammage not accounted) </summary>
	/// <param name="bullet"> The bullet in question </param>
	/// <param name="tgt"> Whatever the bullet hits </param>
	/// <returns> The dammage, as a float </returns>
	public static float Dammage (Bullet bullet, Target tgt) {
		if (tgt.Friendly == bullet.Friendly) {
			return 0;
		}
		Ammunition ammo = bullet.ammo;
		float damage = ammo.default_damage;
		if (ammo.IsKinetic) {
			damage += .5f * ammo.mass * (bullet.Velocity).sqrMagnitude;
		}

		return damage;
	}

	/// <summary> Destroys the bullet </summary>
	public void Destroy () {
		SceneGlobals.bullet_collection.Remove(this);
		Object.Destroy(objct);
	}

	/// <summary> Lets the bullet explode, if it is explosive anyway </summary>
	public void Explode () {
		if (!ammo.IsExplosive) return;
		new Explosion(ammo.explosion_force, Position);
	}

	public DataStructure Save (DataStructure ds) {
		// Save stuff here
		ds.Set("type", 2);
		ds.Set("ammunition", ammo.name);
		ds.Set("velocity", Velocity);
		ds.Set("position", Position);
		ds.Set("is_friend", Friendly);
		return ds;
	}

	public static Bullet Spawn (Ammunition ammo, Vector3 position, Quaternion rotation, Vector3 velocity, bool friendly) {
		GameObject bullet = ammo.Source;

		//Spawn Objects
		Vector3 bullet_spawn_pos = position;
		GameObject bullet_obj = Object.Instantiate(bullet, bullet_spawn_pos, rotation);
		bullet_obj.name = "Bullet: " + ammo.ToString();

		// Initialize the bullet
		Bullet bullet_inst = new Bullet(bullet_obj, ammo, friendly, velocity);

		BulletAttachment bullet_script = Loader.EnsureComponent<BulletAttachment>(bullet_obj);
		bullet_script.instance = bullet_inst;

		return bullet_inst;
	}

	public static bool operator == (Bullet a, object b) {
		return Equals(a, b);
	}

	public static bool operator != (Bullet a, object b) {
		return !Equals(a, b);
	}
}

public struct Hull : IPhysicsObject
{
	public GameObject objct;
	public double mass;

	#region Physics dependencies
	public bool Exists {
		get { return objct; }
	}

	public bool Friendly {
		get { return true; }
	}

	public Vector3 Velocity { get; set;	}
	public Vector3 Position {
		get { return objct.transform.position;  }
		set { objct.transform.position = value;  }
	}
	public Vector3 Acceleration {
		get { return Vector3.zero; }
		set { DeveloppmentTools.Log("Cannot set Acceleration of Hull"); }
	}
	public double Mass {
		get { return mass; }
		set { mass = (float) value; }
	}
	public Vector3 AngularVelocity { get; set; }
	public Quaternion Orientation {
		get { return objct.transform.rotation; }
		set { objct.transform.rotation = value; }
	}

	public void PhysicsUpdate (float deltatime) {
		Position += Velocity * deltatime;
		try {
			Orientation *= Quaternion.Euler(AngularVelocity * deltatime);
		} catch (System.Exception) {
			FileReader.FileLog("Angular Velocity: " + AngularVelocity, FileLogType.error);
		}
	}

	public void Torque (Vector3 t) { }
	public void Push (Vector3 force) { }
	#endregion

	public Hull(GameObject p_object, double p_mass, Vector3 p_velocity) {
		objct = p_object;
		mass = p_mass;
		Velocity = p_velocity;
		AngularVelocity = Vector3.zero;
	}
}

public class Missile : SceneObject, ITargetable
{
	public override string Name {
		// Provisory
		get; protected set;
	}

	public override float Importance {
		get { return 1f; }
	}

	private double _mass;
	public override double Mass {
		get { return _mass; }
		protected set { _mass = value; }
	}

	public Target Associated { get; private set; }

	/// <summary> The target, the object are following </summary>
	public IAimable AimTarget { get; set; }
	/// <summary> The acceleration of the missile </summary>
	public float EngineAcceleration { get; set; }
	/// <summary> If the missile is released </summary>
	public bool Released { get; set; }
	/// <summary> The warhead, the missile is equiped with </summary>
	public Warhead Head { get; set; }

	public const float missile_explosion_force = 50f;

	public DSPrefab source;

	private ParticleSystem ps;
	private float counter;
	private float explosion_force;

	/// <param name="obj"> The object representing the missile </param>
	public Missile (GameObject obj, float time, double mass, int id=-1) : base(SceneObjectType.missile, id) {
		Object = obj;
		ps = Loader.EnsureComponent<ParticleSystem>(obj);
		if (ps != null) { ps.Stop(); }
		counter = time;
		Released = false;
		_mass = mass;

		Friendly = false;

		Associated = Target.None;
		SceneGlobals.missile_collection.Add(this);
		Name = string.Format("Missile {0}", SceneGlobals.missile_collection.Count);
	}

	/// <summary> Releases the missile </summary>
	public void Release () {
		Associated = new Target(Object, _mass, true);
		Released = true;
		explosion_force = missile_explosion_force;
		TgtMarker.Instantiate(this, 2);
		if (ps != null) { ps.Play(); }
	}

	/// <summary> Has to update each phisics frame once the missile is release </summary>
	public override void PhysicsUpdate (float deltatime) {
		if (SceneGlobals.Paused | !Released) return;
		Push(Orientation * Vector3.back * (float) Mass * EngineAcceleration);
		if (AimTarget.Exists) {
			Object.transform.rotation = Quaternion.FromToRotation(Vector3.forward, Position - AimTarget.Position);
		}
		counter -= deltatime;
		if (Physics.Raycast(Position, Velocity, Velocity.magnitude * deltatime * 2)) {
			//Explode();
		}
		if (counter <= 0) {
			Acceleration = Vector3.zero;
		}
		base.PhysicsUpdate(deltatime);
	}

	public override DataStructure Save (DataStructure ds) {
		// Save stuff
		ds.Set("type", 1);
		ds.Set("mass", Mass);
		ds.Set("counter", counter);
		ds.Set("position", Position);
		ds.Set("velocity", Velocity);
		ds.Set("orientation", Orientation);
		ds.Set("angular velocity", AngularVelocity);
		ds.Set("acceleration", EngineAcceleration);

		ds.Set("source", source);
		ds.Set("warhead", (ushort) Head);
		ds.Set("flight duration", counter);
		return ds;
	}

	public static Missile SpawnFlying (DataStructure data) {
		GameObject missile_obj = UnityEngine.Object.Instantiate(data.Get<GameObject>("source"));
		missile_obj.transform.position = data.Get<Vector3>("position");
		missile_obj.transform.rotation = data.Get<Quaternion>("orientation");

		Missile missile_instance = new Missile(missile_obj, data.Get<float>("flight duration"), data.Get("mass", .01), data.Get("id", -1)) {
			AimTarget = Target.None,
			EngineAcceleration = data.Get<float>("acceleration"),
			Head = (Warhead) data.Get<ushort>("warhead"),
			Velocity = data.Get<Vector3>("velocity"),
			AngularVelocity = data.Get<Vector3>("angular velocity"),
			source = data.Get<DSPrefab>("source")
		};

		missile_instance.Released = true;
		
		return missile_instance;
	}

	/// <summary> Called on (un)pause </summary>
	/// <param name="pause"> Paused or unpaused? </param>
	public void OnPause(bool pause) {
		if (ps == null) return;
		if (pause) ps.Pause();
		else ps.Play();
	}

	/// <summary> The pure dammage dealt by the missile (Explosion dammage not accounted) </summary>
	/// <param name="is_friendly"> If the target is friendly </param>
	/// <returns> The dammage, as a float </returns>
	public float Dammage (bool is_friendly) {
		switch (Head) {
		case Warhead.explosives:
			return 0;

		default:
			return 0;
		}
	}

	/// <summary> Lets the missile explode </summary>
	public void Explode () {
		if (Head == Warhead.explosives) {
			new Explosion(explosion_force, Position);
		}
		Destroy();
	}

	/// <summary> Destroys the missile </summary>
	public void Destroy () {
		SceneGlobals.missile_collection.Remove(this);
		UnityEngine.Object.Destroy(Object);
	}

	/// <summary> All the possible warheads for missiles </summary>
	public enum Warhead
	{
		explosives,
		locked_explosives,
	}
}

/// <summary>
///		An explosion, triggered on initialization
///		This spawns a particlesystem for the visuals, but also deals dammage to every destroyable object around.
/// </summary>
public class Explosion: ISavable
{
	public string Name {
		get { return "EXPLOSION"; }
	}

	private GameObject template_obj;
	public GameObject explosion_obj;
	private float explosion_force;

	private ParticleSystem particles;
	private float timer;
	public bool exists;
	private const float lifespan = 2f; // In seconds

	/// <param name="force"> How big the explosion gets. This also determins the dammage, it deals</param>
	/// <param name="explosion"></param>
	public Explosion (float force, Vector3 position, GameObject explosion = null) {
		if (explosion == null) {
			template_obj = Resources.Load("prefs/effects/Explosion") as GameObject;
		} else {
			template_obj = explosion;
		}

		explosion_force = force;
		exists = true;
		SceneGlobals.explosion_collection.Add(this);

		Boom(position);
	}

	/// <summary> Lets show the explosion </summary>
	/// <param name="position"> The position, where the explosion takes place </param>
	public void Boom (Vector3 position) {
		float radius = Mathf.Pow(explosion_force / 10, .33f);

		explosion_obj = Object.Instantiate(template_obj, position, Quaternion.identity);

		particles = explosion_obj.GetComponent<ParticleSystem>();
		if (particles == null) {
			DeveloppmentTools.Log("There is no particlesystem on the explosion object");
			return;
		}

		ParticleSystem.EmissionModule ps_emission = particles.emission;
		ParticleSystem.MainModule ps_main = particles.main;
		ps_emission.rateOverTime = Mathf.Pow(explosion_force, .33f) * 100;
		ps_main.startSpeed = Mathf.Pow(explosion_force, .33f) / 2;
		ps_main.startSize = Mathf.Pow(explosion_force, .33f);

		// Detect nearby ships
		List<Ship> nearby_ships = new List<Ship>();
		foreach (Ship ship in SceneGlobals.ship_collection) {
			float threshhold = radius + ship.radius;
			if ((ship.Position - position).sqrMagnitude <= threshhold * threshhold) {
				nearby_ships.Add(ship);
			}
		}

		float dammage_1m = explosion_force * 5;

		foreach (Ship ship in nearby_ships) {
			foreach (ShipPart component in ship.Parts.AllParts) {
				if (component.Exists) {
					float dammage = dammage_1m / Mathf.Max(.5f, (component.OwnObject.transform.position - position).sqrMagnitude);
					float bef_hp = component.HP;
					component.HP -= dammage;
				}
			}
		}

		foreach (DestroyableTarget obj in SceneGlobals.destroyables) {
			float dammage = dammage_1m / Mathf.Max(.5f, (obj.Position - position).sqrMagnitude);
			float bef_hp = obj.HP;
			obj.HP -= dammage;
		}
	}

	/// <summary> Called if the game (un)pauses </summary>
	/// <param name="pause"> If the game pauses or unpauses </param>
	public void OnPause (bool pause) {
		if (pause) {
			particles.Pause();
		} else {
			particles.Play();
		}
	}

	public void Update (float deltatime) {
		timer += deltatime;
		if (timer >= lifespan) {
			Object.Destroy(explosion_obj);
			exists = false;
		}
	}

	public DataStructure Save (DataStructure ds) {
		// Save stuff
		ds.Set("type", 4);
		ds.Set("position", explosion_obj.transform.position);
		ds.Set("timer", timer);
		return ds;
	}
}