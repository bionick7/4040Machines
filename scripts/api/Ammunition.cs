/* ============================================================
 * An ammunition has several properties, which help determining
 * the dammage dealt whith it.
 * An ammunition can be defined through a configuration file
 * ============================================================ */

public struct Ammunition
{
	public bool IsExplosive { get; private set; }
	public bool IsKinetic { get; private set; }
	public bool IsTimed { get; private set; }
	public Caliber Caliber { get; set; }
	public UnityEngine.GameObject Source { get; private set; }
	public float explosion_force { get; private set; }
	public float default_damage;
	/// <summary> Mass in metric tonns </summary>
	public float mass;
	public string name;

	public UnityEngine.Texture2D dammage_texture;

	/// <summary> Bool, that is used to differantiate between "normal" and "null" ammunitions </summary>
	public bool IsNone { get; private set; }

	/// <param name="_name"> The name of the ammunition </param>
	/// <param name="explosive"> If the ammunition is explosive </param>
	/// <param name="kinetic"> If the ammunition is kinetic </param>
	/// <param name="timed"> if the ammunition's explosive is timed </param>
	/// <param name="_mass"> the mass in kg </param>
	/// <param name="caliber"> the caliber </param>
	/// <param name="source"> The source gameobject </param>
	/// <param name="p_explosion_force"> The explosion caused by the ammo </param>
	/// <param name="_default_damage"> the default dammage of the bullet (without anny explosions- or kinetik-effects) </param>
	public Ammunition (string _name, bool explosive, bool kinetic, bool timed, float _mass, Caliber caliber, UnityEngine.GameObject source, float p_explosion_force, float _default_damage = 0) {
		IsExplosive = explosive;
		IsKinetic = kinetic;
		IsTimed = timed;
		mass = _mass / 1000;
		Source = source;
		explosion_force = p_explosion_force;
		Caliber = caliber;
		default_damage = _default_damage;
		name = _name;
		IsNone = false;
		if (kinetic) {
			dammage_texture = Globals.impact_textures.GetTexture(ImpactTextures.TextureTemplate.ap_hole);
		} else if (explosive) {
			int diameter = 0; // In milimeter
			switch (caliber) {
			case Caliber.c8mm_gettling:
				diameter = 8;
				break;
			case Caliber.c12_high_velocity:
				diameter = 12;
				break;
			case Caliber.c40mmm_autocannon:
				diameter = 40;
				break;
			case Caliber.c500mm_artillery:
				diameter = 500;
				break;
			default:
				break;
			}
			// One pixel represents a milimeter
			dammage_texture = Globals.impact_textures.GetTexture(ImpactTextures.TextureTemplate.he_hole, diameter, diameter);
		} else {
			dammage_texture = new UnityEngine.Texture2D(10, 10, UnityEngine.TextureFormat.Alpha8, false);
		}
	}

	/// <summary> Default ammunition </summary>
	public static readonly Ammunition None = new Ammunition(){ IsNone=true };

	// Override from System.Object
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

	/// <summary> Different calibers have a string correspondence, because of config files </summary>
	public static readonly System.Collections.Generic.Dictionary<string, Caliber> caliber_names = new System.Collections.Generic.Dictionary<string, Caliber>() {
		{ "8", Caliber.c8mm_gettling },
		{ "12", Caliber.c12_high_velocity },
		{ "40", Caliber.c40mmm_autocannon },
		{ "500", Caliber.c500mm_artillery }
	};
}