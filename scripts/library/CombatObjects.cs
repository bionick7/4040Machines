using System.Collections.Generic;
using UnityEngine;


public struct Bullet
{
	public Ammunition ammo;
	public bool side;

	public Vector3 Velocity { get; set;	}
	public Vector3 Position {
		get {
			return objct.transform.position;
		}
		set {
			objct.transform.position = value;
		}
	}
	public bool Exists {
		get {
			return objct;
		}
	}

	public GameObject objct;

	public Bullet (GameObject obj, Ammunition ammochoice, bool side_, Vector3 vel) {
		ammo = ammochoice;
		side = side_;
		objct = obj;
		Velocity = vel;

		SceneData.bullet_list.Add(this);
	}

	public static float Dammage (Bullet bullet, Vector3 velocity, bool side) {
		if (side == bullet.side) {
			return 0;
		}

		Ammunition ammo = bullet.ammo;
		float damage = ammo.default_damage;
		if (ammo.IsKinetic) {
			damage += .5f * ammo.mass * (bullet.Velocity - velocity).sqrMagnitude;
		}

		return damage;
	}

	public static float Dammage (Bullet bullet, Target tgt) {
		if (tgt.side == bullet.side) {
			return 0;
		}
		Ammunition ammo = bullet.ammo;
		float damage = ammo.default_damage;
		if (ammo.IsKinetic) {
			damage += .5f * ammo.mass * (bullet.Velocity).sqrMagnitude;
		}

		return damage;
	}

	public void Update () {
		Position += Velocity * Time.deltaTime;
	}

	public void Destroy () {
		SceneData.bullet_list.Remove(this);
		Object.Destroy(objct);
	}

	public void Explode () {
		if (!ammo.IsExplosive) return;
		ammo.Explosion.Boom(Position);
	}

	public static bool operator == (Bullet a, object b) {
		return Equals(a, b);
	}

	public static bool operator != (Bullet a, object b) {
		return !Equals(a, b);
	}
}

public class Missile : SceneObject
{
	public override float Importance {
		get {
			return 1f;
		}
	}

	private double _mass;
	public override double Mass {
		get {
			return _mass;
		}
		set {
			_mass = value;
		}
	}

	/// <summary> The target, the object are following </summary>
	public Target Target { get; set; }
	/// <summary> The acceleration of the missile </summary>
	public float EngineAcceleration { get; set; }
	/// <summary> If the missile is released </summary>
	public bool Released { get; set; }
	/// <summary> The warhead, the missile is equiped with </summary>
	public Warhead Head { get; set; }

	private ParticleSystem ps;
	//private Rigidbody rb;
	private float counter;
	private Explosion explosion;

	/// <param name="obj"> The object representing the missile </param>
	public Missile (GameObject obj, float time, double mass) : base(SceneObjectType.missile) {
		Object = obj;
		ps = obj.GetComponent<ParticleSystem>();
		//rb = obj.GetComponent<Rigidbody>();
		if (ps != null) { ps.Stop(); }
		counter = time;
		Released = false;
		_mass = mass;

		explosion = new Explosion(50);
		side = false;

		SceneData.missile_list.Add(this);
	}

	/// <summary> Releases the missile </summary>
	public void Release () {
		Released = true;
		if (ps != null) { ps.Play(); }
	}

	/// <summary> Has to update each phisics frame once the missile is release </summary>
	public void PhysicsUpdate () {
		if (!Released || counter <= 0f) { return; }
		Push(Orientation * Vector3.back * (float) Mass * EngineAcceleration * Time.fixedDeltaTime);
		if (Target.Exists) {
			Object.transform.rotation = Quaternion.FromToRotation(Vector3.forward, Position - Target.Position);
		}
		counter -= Time.fixedDeltaTime;
		if (Physics.Raycast(Position, Velocity, Velocity.magnitude * Time.deltaTime * .5f)) {
			Explode();
		}
	}

	public float Dammage (bool side) {
		switch (Head) {
		case Warhead.explosives:
			return 0;

		default:
			return 0;
		}
	}

	public void Explode () {
		if (Head == Warhead.explosives) {
			explosion.Boom(Position);
		}
		Destroy();
	}

	public void Destroy () {
		SceneData.missile_list.Remove(this);
		UnityEngine.Object.Destroy(Object);
	}

	/// <summary> All the possible warheads for missiles </summary>
	public enum Warhead
	{
		explosives,
		locked_explosives,
	}
}

public class Explosion
{
	private GameObject explosion_obj;
	private float explosion_force;

	private ParticleSystem particles;

	public Explosion (float force, GameObject explosion = null) {
		if (explosion == null) {
			explosion_obj = Resources.Load("prefs/effects/Explosion") as GameObject;
		} else {
			explosion_obj = explosion;
		}

		explosion_force = force;

		particles = explosion_obj.GetComponent<ParticleSystem>();
		if (particles == null) {
			throw new System.NullReferenceException("There is no particlesystem on the explosion object");
		}
		ParticleSystem.EmissionModule ps_emission = particles.emission;
		ParticleSystem.MainModule ps_main = particles.main;
		ps_emission.rateOverTime = Mathf.Pow(explosion_force, .33f) * 10;
		ps_main.startSpeed = Mathf.Pow(explosion_force, .33f) / 2;
		ps_main.startSize = Mathf.Pow(explosion_force, .33f);
	}

	public void Boom (Vector3 position) {
		float radius = Mathf.Pow(explosion_force / 10, .33f);

		GameObject exp = Object.Instantiate(explosion_obj, position, Quaternion.identity);
		Object.Destroy(exp, 2f);

		// Detect nearby ships
		List<Ship> nearby_ships = new List<Ship>();
		foreach (Ship ship in SceneData.ship_list) {
			if ((ship.Position - position).magnitude <= radius + ship.radius) {
				nearby_ships.Add(ship);
			}
		}

		float dammage_1m = explosion_force * 5;

		foreach (Ship ship in nearby_ships) {
			foreach (ShipPart component in ship.Parts.AllParts) {
				float dammage = dammage_1m / Mathf.Max(.5f, (component.OwnObject.transform.position - position).sqrMagnitude);
				component.HP -= dammage;
			}
		}
	}
}