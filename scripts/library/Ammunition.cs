public struct Ammunition
{
	public bool IsExplosive { get; private set; }
	public bool IsKinetic { get; private set; }
	public bool IsTimed { get; private set; }
	public Caliber Caliber { get; set; }
	public UnityEngine.GameObject Source { get; private set; }
	public Explosion Explosion { get; private set; }
	public float default_damage;
	/// <summary> Mass in metric tonns </summary>
	public float mass;
	public string name;

	public bool IsNone { get; private set; }

	/// <param name="_name"> The name of the ammunition </param>
	/// <param name="explosive"> If the ammunition is explosive </param>
	/// <param name="kinetic"> If the ammunition is kinetic </param>
	/// <param name="timed"> if the ammunition's explosive is timed </param>
	/// <param name="_mass"> the mass in kg </param>
	/// <param name="caliber"> the caliber </param>
	/// <param name="source"> The source gameobject </param>
	/// <param name="exp"> The explosion caused by the ammo </param>
	/// <param name="_default_damage"> the default dammage of the bullet (without anny explosions- or kinetik-effects) </param>
	public Ammunition (string _name, bool explosive, bool kinetic, bool timed, float _mass, Caliber caliber, UnityEngine.GameObject source, Explosion exp, float _default_damage = 0) {
		IsExplosive = explosive;
		IsKinetic = kinetic;
		IsTimed = timed;
		mass = _mass / 1000;
		Source = source;
		Explosion = exp;
		Caliber = caliber;
		default_damage = _default_damage;
		name = _name;
		IsNone = false;
	}

	public static readonly Ammunition None = new Ammunition(){ IsNone=true };

	public override string ToString () {
		return string.Format("ammo {0} for {1}", name, Caliber);
	}

	public static bool operator == (Ammunition a, object b) {
		if (!(b is Ammunition)) return false;
		return Equals(a, b);
	}

	public static bool operator != (Ammunition a, object b) {
		if (!(b is Ammunition)) return true;
		return !Equals(a, b);
	}

	public static readonly System.Collections.Generic.Dictionary<string, Caliber> caliber_names = new System.Collections.Generic.Dictionary<string, Caliber>() {
		{ "8", Caliber.c8mm_gettling },
		{ "12", Caliber.c12_high_velocity },
		{ "40", Caliber.c40mmm_autocannon },
		{ "500", Caliber.c500mm_artillery }
	};
}