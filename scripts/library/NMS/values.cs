using System;
using System.Collections.Generic;

namespace NMS
{
	public enum VType
	{
		integer,
		floating,
		character,
		boolean,
		none,
		function,
	}

	public interface IValue
	{
		byte [] Value { get; set; }
		VType Type { get; set; }
		uint Size { get; }

		IValue Copy ();
	}

	public static class Consts
	{
		public static Dictionary<VType, uint> sizes = new Dictionary<VType, uint>(){
			{ VType.integer, 2u },
			{ VType.floating, 2u },
			{ VType.character, 1u },
			{ VType.boolean, 1u },
			{ VType.none, 0u }
		};
	}

	public struct Array : IValue {
		public byte [] Value { get; set; }
		public VType Type { get; set; }
		/// <summary> How many items there are </summary>
		public uint Length { get; private set; }
		public uint item_size;

		/// <summary> Size in terms of bytes </summary>
		public uint Size { get; private set;  }

		public bool more_dimensions;

		public Array (VType type, uint length) {
			Type = type;
			Length = length;
			item_size = Consts.sizes [type];
			Size = item_size * length;
			Value = new byte [Size];

			more_dimensions = false;
		}

		public Array (VType type, byte [] data) {
			Type = type;
			Size = (uint) data.Length;
			item_size = Consts.sizes [type];
			Length = (uint) Size / item_size;
			Value = data;

			more_dimensions = false;
		}

		public IValue GetIndex (uint index) {
			if (index >= Length || index < 0) {
				Globals.current_os.ThrowError("Index out of range");
			}
			byte [] bytes = new byte [Consts.sizes[Type]];
			uint min = index * item_size, max = (index + 1u) * item_size;
			uint i = 0u;
			for (uint j=0u; j < Length; j++) {
				if (j >= min && j < max) {
					bytes [i++] = Value [j];
				}
			}
			if (more_dimensions) {
				return new Array() {
					Type = this.Type,
					Value = bytes
				};
			}
			return new Single(Type, bytes);
		}

		public void SetIndex (uint index, byte [] value) {
			if (index >= Length || index < 0) {
				Globals.current_os.ThrowError("Index out of range");
			}
			if (value.Length != item_size) {
				Globals.current_os.ThrowError("Does not match type");
			}
			uint min = index * item_size, max = (index + 1u) * item_size;
			uint i = 0u;
			for (uint j=0u; j < Length; j++) {
				if (j >= min && j < max) {
					Value [i++] = value [j];
				}
			}
		}

		public IValue Copy () {
			byte [] data = new byte [Size];
			for (uint i=0u; i < Size; i++) {
				data [i] = Value [i];
			}
			return new Array(Type, data);
		}

		
		public bool Equals (IValue other) {
			if (other.GetType() != typeof(Array)){
				return false;
			}
			if (other.Type != Type) {
				return false;
			}
			for (uint i=0u; i < Size; i++) {
				if (other.Value [i] != Value [i]) {
					return false;
				}
			}
			return true;
		}
		
	}

	public class Single : IValue {
		public byte [] Value { get; set; }
		public VType Type { get; set; }
		public uint Size { get; set; }

		public Single (VType type, byte [] value) {
			Type = type;
			Size = Consts.sizes [type];
			if (value.Length != Size) {
				Globals.current_os.ThrowError("Does not match type");
				Value = new byte [Size];
				goto END;
			}
			Value = value;
		END:
			;
		}

		public static readonly Single none = new Single( VType.none, new byte[0] );

		public IValue Copy () {
			byte [] data = new byte [Size];
			for (uint i=0u; i < Size; i++) {
				data [i] = Value [i];
			}
			return new Single(Type, data);
		}
		
		public bool Equals (IValue other) {
			if (other.GetType() != typeof(Single)){
				return false;
			}
			if (other.Type != Type) {
				return false;
			}
			for (uint i=0u; i < Size; i++) {
				if (other.Value [i] != Value [i]) {
					return false;
				}
			}
			return true;
		}

	}

	public class Function : IValue
	{
		public byte [] Value { get; set; }
		public VType Type { get; set; }
		public uint Size { get; private set; }

		public Interpreter Interpreter { private get; set; }
		public List<string> local_names = new List<string>();
		
		public Function (string [] lines) {
			List<byte> data = new List<byte>();
			for (int i=0; i < lines.Length; i++) {
				for (int j=0; j < lines [i].Length; j++) {
					data.Add((byte) lines [i] [j]);
				}
				data.Add(0x00);
			}
			Value = data.ToArray();
			Size = (uint) data.Count;
			Type = VType.function;
		}

		public Function (byte [] data) {
			Value = data;
			Size = (uint) data.Length;
			Type = VType.function;
		}

		public IValue Execute( IValue[] args ) {
			Single return_value = Single.none;
			Interpreter.recursion++;
			if (args.Length > local_names.Count) {
				//throw new Syntaxerror("To many arguments", 0);
				goto RETURN;
			}
			for (int i=0; i < args.Length; i++) {
				Interpreter.local.Add(local_names [i], args [i]);
			}
			string curr_commamd = "";
			for (int i=0; i < Size; i++) {
				if (Value[i] == 0x00) {
					curr_commamd = "";
					byte [] res;
					Interpreter.Process(curr_commamd, out res);
					if (res.Length != 0) {
						byte[] res2 = new byte[res.Length - 1];
						for (int j=1; j < res.Length; j++) { res2 [j] = res [j]; }
						return_value = new Single((VType) res[0], res2);
						goto RETURN;
					}
				} else {
					curr_commamd += Value [i];
				}
			}
			RETURN:
			Interpreter.recursion--;
			return return_value;
		}

		public IValue Copy () {
			return new Function(Value) {
				local_names = this.local_names
			};
		}

		public bool Equals (IValue other) {
			if (other.GetType() != typeof(Function)){
				return false;
			}
			if (other.Type != Type) {
				return false;
			}
			for (uint i=0u; i < Size; i++) {
				if (other.Value [i] != Value [i]) {
					return false;
				}
			}
			return true;
		}
	}

	public class BuildinFunction : Function
	{
		public enum BuildIns
		{
			print,
			input,
			return_,
		}

		public BuildIns func;

		public BuildinFunction () : base( new byte [0]) { }

		private void Print (IValue[] args) {

		}

		private void Input (IValue[] args) {

		}

		private void Return (IValue[] args) {

		}

		public new IValue Execute (IValue[] args) {
			switch (func) {
			case BuildIns.print:
				Print(args);
				break;
			case BuildIns.input:
				Input(args);
				break;
			case BuildIns.return_:
				Return(args);
				break;
			}
			return Single.none;
		}

		public static readonly BuildinFunction print = new BuildinFunction() { func = BuildIns.print };
		public static readonly BuildinFunction input = new BuildinFunction() { func = BuildIns.input };
		public static readonly BuildinFunction return_ = new BuildinFunction() { func = BuildIns.return_ };
	}
}
