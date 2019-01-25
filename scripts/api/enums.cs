/* ===============================================
 * All significant enumerations are stored in here
 * =============================================== */

/// <summary> If and how the console is shown </summary>
public enum ConsolePosition
{
	shown,
	hidden,
	lower,
}

///<summary> The calibers of weaponnery </summary>
public enum Caliber
{
	c8mm_gettling,
	c12_high_velocity,
	c40mmm_autocannon,
	c500mm_artillery
}

/// <summary> Where does the scene play? (Possibilities; not all implemented) </summary>
public enum Sceneries
{
	mercury,
	venus,
	earth,
	moon,
	mars,
	ceres,
	random_astroid,
	jupiter,
	io,
	ganmeyde,
	saturn,
	titan,
	uranus,
	neptune,
	trans_neptunian
}

/*
/// <summary> Different Parts </summary>
public enum PartsOptions
{
	main = 0,
	weapon = 1,
	docking_port = 2,
	fuel_tank = 3,
	engine = 4,
	power_reciever = 5,
	weapon_cooling = 6,
	life_support = 7,
	structure = 8,
	turret = 9,
	ammobox = 10,
	missilelauncher = 11,
	armor = 12,
}
*/

/// <summary> Objectives for a mission </summary>
public enum Objectives
{
	destroy,
	escort,
	hack,
	none
}

/// <summary>  </summary>
public enum Skills
{
	pilot,
	computer,
	engineering,
	trade,
	diplomacy
}

/// <summary> The type of sceneobjects </summary>
public enum SceneObjectType
{
	ship,
	target,
	missile,
	network,
	none,
}

/// <summary> Where a ship can point to </summary>
public enum PointTo
{
	velocity_p,
	velocity_n,
	target_p,
	target_n,
	tg_velocity_p,
	tg_velocity_n,
	none
}