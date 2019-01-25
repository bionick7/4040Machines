/* ================================================
 * 
 * Plugin Contracts provide the possibility to Add
 * behaviours to the game
 * 
 * ================================================ */


namespace PluginContracts
{
	public interface IPlugin
	{
		/// <summary>
		///		The name of the plugin 
		///		(specified in the dll)
		/// </summary>
		string Name { get; }

		/// <summary>
		///		The function, what the plugin does
		///		(specified in the dll)
		/// </summary>
		void DoOnUpdate ();

		void DoOnLoad ();
	}
}