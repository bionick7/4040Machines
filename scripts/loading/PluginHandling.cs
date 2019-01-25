using PluginContracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

public class PluginHandling
{
	public List<IPlugin> plugins;

	public List<Type> ship_parts = new List<Type>() {
		typeof(Main),
		typeof(Weapon),
		typeof(DockingPort),
		typeof(FuelTank),
		typeof(Engine),
		typeof(PowerSource),
		typeof(WeaponCooling),
		typeof(LifeSupport),
		typeof(Structure),
		typeof(Turret),
		typeof(AmmoBox),
		typeof(MissileLauncher),
		typeof(Armor)
	};

	public void Load () {
		// Find all assemblies
		string[] dll_file_names = null;
		if (Directory.Exists(Globals.plugin_path)) {
			dll_file_names = Directory.GetFiles(Globals.plugin_path, "*.dll");
		}

		List<Type> plugin_types = new List<Type>();

		// load assemblies
		foreach (string dllname in dll_file_names) {
			AssemblyName an = AssemblyName.GetAssemblyName(dllname);
			Assembly assembly = Assembly.Load(an);

			if (assembly != null) {
				foreach (Type type in assembly.GetTypes()) {
					if (type.IsInterface || type.IsAbstract)
						goto CONTINUE_;
					if (type.GetInterface(typeof(IPlugin).FullName) != null) {
						plugin_types.Add(type);
					}
				}
			}
			CONTINUE_:;
		}

		// Convert assemblies into plugins
		plugins = new List<IPlugin>(plugin_types.Count);
		foreach (Type type in plugin_types) {
			IPlugin plugin = (IPlugin)Activator.CreateInstance(type);
			plugins.Add(plugin);
			plugin.DoOnLoad();
		}
	}

	public void LoadShipParts () {
		// Find all assemblies
		string path = Globals.plugin_path + "/ShipParts";

		string[] dll_file_names = null;
		if (Directory.Exists(path)) {
			dll_file_names = Directory.GetFiles(path, "*.dll");
		} else {
			DeveloppmentTools.Log("No such path: " + path);
		}

		// load assemblies
		foreach (string dllname in dll_file_names) {
			AssemblyName an = AssemblyName.GetAssemblyName(dllname);
			Assembly assembly = Assembly.Load(an);

			if (assembly != null) {
				foreach (Type type in assembly.GetTypes()) {
					if (type.IsInterface || type.IsAbstract)
						goto CONTINUE_;
					if (type.BaseType == typeof(ShipPart)) {
						ship_parts.Add(type);
					}
				}
			}
			CONTINUE_:;
		}

	}

	public void Update () {
		foreach (IPlugin plugin in plugins) {
			plugin.DoOnUpdate();
		}
	}

	public static object GetConstant (Type type, string constant_name) {
		var fieldInfos = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        FieldInfo field_info = Array.Find(fieldInfos, fi => fi.IsLiteral && !fi.IsInitOnly && fi.Name == constant_name);
		return field_info.GetRawConstantValue();
	}
}
