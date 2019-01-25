using System;
using System.Collections.Generic;
using UnityEngine;
using FileManagement;

class DeveloppmentTools
{
	public static string LogIterable <T> (ICollection<T> collection) {
		if (collection == null) {
			return "(Collection is NULL)";
		}
		string tot_string = String.Empty;
		foreach (T item in collection) {
			if (item == null) tot_string += "- NULL\n";
			else tot_string += String.Format("- \"{0}\" ;\n", item.ToString());
		}
		return tot_string;
	}

	public static void DrawAxisSystem (Vector3 origin, float length=20f, float duration=60) {
		Debug.DrawRay(origin, Vector3.right * length, Color.red, duration);
		Debug.DrawRay(origin, Vector3.up * length, Color.green, duration);
		Debug.DrawRay(origin, Vector3.forward * length, Color.blue, duration);
	}

	public static void Log (object message) {
		Debug.Log(message);
		FileReader.FileLog(message.ToString(), FileLogType.runntime);
	}

	public static void LogFormat (string format, params object[] parameters) {
		Debug.LogFormat(format, parameters);
		FileReader.FileLog(string.Format(format, parameters), FileLogType.runntime);
	}

	public static void Testing () {
	}
}

/*
 * assemly code:
 * 	os.cpu.Execute(new ulong [] {
		//XX_PAAAABBBBCCCC
		0x0100000000060000,	// i=0 (pt6)
		0x0100486500000000,	// He   
		0x01006c6c00010000,	// ll      
		0x01006f2000020000,	// o       
		0x0100576f00030000,	// Wo      
		0x0100726c00040000,	// rl      
		0x0100642100050000,	// d!      
		0x4000000000060000,	// Print <-+
		0x2004000600010006, // x += 1  |
		0x13040006000a000b,	// If x=10 |  goto -+
		0x1000000700000000, // goto   -+        |
		0x0200000000000000, // end        <-----+
	});
 * */
