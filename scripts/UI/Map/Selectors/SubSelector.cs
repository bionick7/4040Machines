using UnityEngine;
using System.Collections.Generic;

public class SubSelector : SelectorLike
{
	public SelectorOptions options;

	public SelectorParent Parent {
		get {
			if (targets.Length > 0) return SelectorParent.target;
			return SelectorParent.selector;
		}
	}

	public void InitSub () {
		options_str = options.StringsArr;
		options_icons = options.IconArr;
		options_flags = options.FlagArr;
		option_functions = options.FunctionArr;
		Init();
	}
}

/// <summary>
///		SelectorOptions is ment to store, which things are shown by the selector,
///		and which aren't.
/// </summary>
public struct SelectorOptions
{
	private SelectorData source_data;

	public string[] options_possible;
	public Sprite[] icons_possible;
	public int   [] flags_possible;
	public int   [] functions_possible;

	public SelectionClass selection_class;

	public const byte options_length = 16;

	public string[] StringsArr {
		// Can get ameliorated
		get {
			var res = new string[Count];
			int i = 0;
			for (int j=0; j < options_length; j++) {
				if ((value << j) % 0x10000 >= 0x8000)
					res[i++] = options_possible [j];
			}
			return res;
		}
	}

	public Sprite[] IconArr {
		get {
			var res = new Sprite[Count];
			for (int j=0, i=0; j < options_length; j++) {
				if ((value << j) % 0x10000 >= 0x8000) {
					res [i++] = icons_possible [j];
				}
			}
			return res;
		}
	}

	public int[] FlagArr {
		get {
			var res = new int[Count];
			for (int j=0, i=0; j < options_length; j++) {
				if ((value << j) % 0x10000 >= 0x8000) {
					res [i++] = flags_possible [j];
				}
			}
			return res;
		}
	}

	public int[] FunctionArr {
		get {
			var res = new int[Count];
			for (int j=0, i=0; j < options_length; j++) {
				if ((value << j) % 0x10000 >= 0x8000) {
					res [i++] = functions_possible [j];
				}
			}
			return res;
		}
	}

	public ushort value;

	/// <summary> Returns true, if the given possibility is on </summary>
	/// <param name="indexer"> The index of the option </param>
	public bool this [byte indexer] {
		get {
			if (indexer < 0 | indexer >= options_length)
				throw new System.ArgumentOutOfRangeException(string.Format("indexer ({0}) must not be bigger than 31", indexer));
			return value << indexer >= 0x8000;
		}
	}

	public byte Count {
		get {
			byte res = 0;
			for (byte i=0; i < options_length; i++) {
				if ((value << i) % 0x10000 >= 0x8000) res++;
			}
			return res;
		}
	}

	public SelectorOptions (ushort p_options, SelectionClass p_class) {
		source_data = Globals.selector_data;
		value = p_options;
		selection_class = p_class;
		switch (p_class) {
		case SelectionClass.Reference:
			options_possible = source_data.reference_options;
			icons_possible = source_data.reference_icons;
			flags_possible = source_data.reference_flags;
			functions_possible = source_data.reference_function_pointers;
			break;
		case SelectionClass.Target:
			options_possible = source_data.target_options;
			icons_possible = source_data.target_icons;
			flags_possible = source_data.target_flags;
			functions_possible = source_data.target_function_pointers;
			break;
		case SelectionClass.Info:
			options_possible = source_data.info_options;
			icons_possible = source_data.info_icons;
			flags_possible = source_data.info_flags;
			functions_possible = source_data.info_function_pointers;
			break;
		default:
		case SelectionClass.Command:
			options_possible = source_data.command_options;
			icons_possible = source_data.command_icons;
			flags_possible = source_data.command_flags;
			functions_possible = source_data.command_function_pointers;
			break;
		}
	}

	/// <summary> Returns you the "position" a string, if present </summary>
	/// <param name="str"> The string in question </param>
	public byte Place (string str) {
		for (byte i=0; i < options_length; i++) {
			if ((value << i) % 0x10000 >= 0x8000) {
				if (options_possible [i] == str) return i;
			}
		}
		throw new KeyNotFoundException(string.Format("{0} not in options", str));
	}

	public override string ToString () {
		return string.Format("<SelectorOption: HEX {0:x}>", value);
	}
}

public enum SelectionClass {
	Reference = 0,
	Target    = 1,
	Info      = 2,
	Command   = 3
}