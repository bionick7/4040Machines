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

/// <summary> Different Parts </summary>
public enum PartsOptions
{
	main,
	weapon,
	docking_port,
	fuel_tank,
	engine,
	power_reciever,
	weapon_cooling,
	life_support,
	structure,
	turret,
	ammobox,
	missilelauncher,
}

public enum Objectives
{
	destroy,
	escort,
	hack,
	none
}

public enum Skills
{
	pilot,
	computer,
	engineering,
	trade,
	diplomacy
}

public enum Sex
{
	male,
	female
}