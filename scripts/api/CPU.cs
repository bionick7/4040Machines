using System;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

// #==================================================================#
// CPU  0   1   2   3   4   5   6   7   8   9   A   B   C   D   E   F "
// #==================================================================#
// " 0 NUL JMP ADD     PRT             TRN SHT SLP                    "
// " 1 SET JEQ SUB     INP             RCS SHF                        "
// " 2 END JGT MUL     CLS             ENG MIS                        "
// " 3 F2I JGE DIV                     HLD                            "
// " 4 I2F                             LCK                            "
// " 5                                                                "
// " 6                                                                "
// " 7                                                                "
// " 8                                                                "
// " 9                                                                "
// " A                                                                "
// " B                                                                "
// " C                                                                "
// " D                                                                "
// " E                                                                "
// " F                                                                "
// #==================================================================#
// RAM  0   1   2   3   4   5   6   7   8   9   A   B   C   D   E   F "
// #==================================================================#
// " 0         RND                     POS AMM                        "
// " 1                                 POS AM*                        "
// " 2             TGD                 POS FEL                        "
// " 3                                 VEL FE*                        "
// " 4                                 VEL MIS                        "
// " 5                                 VEL MI*                        "
// " 6                                 ROT                            "
// " 7                                 ROT                            "
// " 8                                 ROT                            "
// " 9                                 ANG                            "
// " A                                 ANG                            "
// " B                                 ANG                            "
// " C                                                                "
// " D                                                                "
// " E                                                                "
// " F                                                                "
// #==================================================================#

namespace NMS
{	

	public class CPU
	{
		public Ship ship;
		public OS.OperatingSystem os;

		public RAM ram;

		public LowLevelAI AI {
			get { return SceneGlobals.Player.low_ai; }
		}

		public string Input {
			get {
				if (os.console != null && os.console.HasInput) return os.console.ReadLine();
				return string.Empty;
			}
		}

		public string Output {
			get {
				return os.output;
			}
			set {
				os.output = value;
			}
		}

		public CPU (Ship p_ship, OS.OperatingSystem p_os) {
			ship = p_ship;
			os = p_os;

			ram = new RAM(0x1000);
		}

		private void Error (string text, int line) {
			Output += string.Format("Error at line {0}: {1}", line, text);
		}

		/// <summary> Takes an input and processes it like a CPU </summary>
		/// <param name="arguments"> The input as a ulong form bytes. For further information see the remarque </param>
		/// <remarks>
		///		Content of command |	pointers?			\
		///		[-------------]	 [-----] X V V V			|		X -> float flag : indicates, wheter operations are float operations
		///		0 0 0 0 0 0 0 0  0 0 0 0 0 0 0 0			|
		///						 Importance					|
		///		Content of a								|
		///		[------------------------------]			|
		///		0 0 0 0 0 0 0 0  0 0 0 0 0 0 0 0			|
		///													 >- 64-bit integer (ulong) 
		///		Content of b								|
		///		[------------------------------]			|
		///		0 0 0 0 0 0 0 0  0 0 0 0 0 0 0 0			|
		///													|
		///		Content of c								|
		///		[------------------------------]			|
		///		0 0 0 0 0 0 0 0  0 0 0 0 0 0 0 0			/
		/// </remarks>
		public void Execute (ulong [] code) {
			bool running = true;
			var t0 = DateTime.Now;
			for (int i = 0; running & i < code.Length; i++) {
				ulong arguments = code [i];
				ushort arg3 = (arguments >> 0x30) % 2 == 1 ? ram[(int) ((arguments >> 0x00) % 0x10000)] : (ushort) ((arguments >> 0x00) % 0x10000);
				ushort arg2 = (arguments >> 0x31) % 2 == 1 ? ram[(int) ((arguments >> 0x10) % 0x10000)] : (ushort) ((arguments >> 0x10) % 0x10000);
				ushort arg1 = (arguments >> 0x32) % 2 == 1 ? ram[(int) ((arguments >> 0x20) % 0x10000)] : (ushort) ((arguments >> 0x20) % 0x10000);
				ushort arg0 = (ushort) (arguments >> 0x38);
				//UnityEngine.Debug.LogFormat("{0} -> {1:x}", (OS.AssemblyCommands) arg0, arguments);

				float importance = ((arguments >> 0x34) % 16) / 15f;
				bool is_float = (arguments >> 0x33) % 2 == 1;
				switch (arg0) {
				// 0x00 - 0x0f: Simple operations
				case 0x00:
					// NULL - NUL
					goto ENDCOMMAND;
				case 0x01:
					// Assign a -> &b || SET
					ram [arg2] = arg1;
					goto ENDCOMMAND;
				case 0x02:
					// End the programm; No arguments || END
					running = false;
					goto ENDCOMMAND;
				case 0x03:
					// Float &a to Integer || F2I
					ram [arg1] = (ushort) RAM.Short2Float(arg1);
					goto ENDCOMMAND;
				case 0x04:
					// Integer &a to float || I2F
					ram [arg1] = RAM.Float2Short((float) arg1);
					goto ENDCOMMAND;

				// 0x10 - 0x1f: Simple logic
				case 0x10:
					// go to line a || JMP
					i = arg1 - 1;
					goto ENDCOMMAND;
				case 0x11:
					// if a == b: go to line c || JEQ
					if (arg1 == arg2) i = arg3 - 1;
					goto ENDCOMMAND;
				case 0x12:
					// if a > b: go to line c || JGT
					if (is_float) {
						if (arg1 > arg2) i = arg3 - 1;
					} else {
						if (RAM.Short2Float(arg1) > RAM.Short2Float(arg2)) i = arg3 - 1;
					}
					goto ENDCOMMAND;
				case 0x13:
					// if a >= b: go to line c || JGE
					if (is_float) {
						if (arg1 >= arg2) i = arg3 - 1;
					} else {
						if (RAM.Short2Float(arg1) >= RAM.Short2Float(arg2)) i = arg3 - 1;
					}
					goto ENDCOMMAND;

				// 0x20 - 0x2f: Mathematical operations
				case 0x20:
					// Addition a + b -> &c || ADD
					ram [arg3] = is_float ? RAM.Float2Short(RAM.Short2Float(arg1) + RAM.Short2Float(arg2)) : (ushort) (arg1 + arg2);
					//UnityEngine.Debug.LogFormat("{0} + {1} = {2}", arg1, arg2, is_float ? RAM.Float2Short(RAM.Short2Float(arg1) + RAM.Short2Float(arg2)) : (ushort) (arg1 + arg2));
					goto ENDCOMMAND;
				case 0x21:
					// Substraction a - b -> &c || SUB
					ram [arg3] = is_float ? RAM.Float2Short(RAM.Short2Float(arg1) - RAM.Short2Float(arg2)) : (ushort) (arg1 - arg2);
					goto ENDCOMMAND;
				case 0x22:
					// Multiplication a * b -> &c || MUL
					ram [arg3] = is_float ? RAM.Float2Short(RAM.Short2Float(arg1) * RAM.Short2Float(arg2)) : (ushort) (arg1 * arg2);
					goto ENDCOMMAND;
				case 0x23:
					// Integer Division a / b -> &c || DIV
					ram [arg3] = is_float ? RAM.Float2Short(RAM.Short2Float(arg1) / RAM.Short2Float(arg2)) : (ushort) (arg1 / arg2);
					goto ENDCOMMAND;

				// 0x40 - 0x4f: console functions
				case 0x40:
					// Print from &a, length b on console || PRT
					if (arg1 + arg2 > ram.size)
						Error("Buffer overflow", i);

					char[] characters = new char[arg2 * 2];
					for (uint j=0u; j < arg2; j++) {
						ushort nums = ram[arg1 + j];
						characters [2 * j] = (char) (nums >> 8);
						characters [2 * j + 1] = (char) (nums % 0x100);						
					}
					string word = new string(characters);
					Output += word;
					goto ENDCOMMAND;
				case 0x41:
					// Save Input, beginning from a || INP
					string word_ = Input;
					if (arg1 + word_.Length / 2 > ram.size)
						Error("Buffer overflow", i);

					for (uint j=0u; j < word_.Length; j += 2) {
						ram [arg0 + j / 2] = (ushort) (word_ [(int)j] + word_ [(int)j] << 8);
					}
					goto ENDCOMMAND;
				case 0x42:
					// ClearScreen; No arguments || CLS
					Output = string.Empty;
					goto ENDCOMMAND;

				// 0x80 - 0x8f : Movement Control
				case 0x80:
					// Turn with RCS: euler angles: (a / 100, b / 100, c / 100)
					AI.ApplyTurn(new UnityEngine.Vector3(RAM.Short2Float(arg1), RAM.Short2Float(arg2), RAM.Short2Float(arg3)), importance);
					goto ENDCOMMAND;
				case 0x81:
					// Thrust with RCS: Δv-vector: (a / 100, b / 100, c / 100)
					AI.RCSThrust(new UnityEngine.Vector3(RAM.Short2Float(arg1), RAM.Short2Float(arg2), RAM.Short2Float(arg3)));
					goto ENDCOMMAND;
				case 0x82:
					// Engine Acceleration: Δv-vector: (a / 100, b / 100, c / 100)
					AI.Thrust(new UnityEngine.Vector3(RAM.Short2Float(arg1), RAM.Short2Float(arg2), RAM.Short2Float(arg3)));
					goto ENDCOMMAND;
				case 0x83:
					// Hold Orientation: time: a / 10 seconds
					AI.movement_quack.PushBack(AIMouvementCommand.HoldOrientation(RAM.Short2Float(arg1), ship));
					goto ENDCOMMAND;
				case 0x84:
					// Lock target: time: a / 10
					AI.movement_quack.PushBack(AIMouvementCommand.HoldOrientation(RAM.Short2Float(arg1), ship));
					goto ENDCOMMAND;
				// 0x90 - 0x9f : Ship actions
				case 0x90:
					// Shoot turretgroup number a at aim
					AI.action_list.Add(AIActionCommand.ShootTG(ship.TurretGroups[arg1], ship.TurretAim));
					goto ENDCOMMAND;
				case 0x91:
					// Shoot fixed weapon number a at euler_angle rotation (b, c, 0)
					AI.action_list.Add(AIActionCommand.FireMain(ship.TurretAim));
					goto ENDCOMMAND;
				case 0x92:
					// Shoot missile at target_id a
					goto ENDCOMMAND;
				// 0xa0 - 0x9f : Complex/Ship related general commands
				case 0xa0:
					// Wait for a / 10 seconds
					AI.movement_quack.PushBack(AIMouvementCommand.Wait(RAM.Short2Float(arg1)));
					goto ENDCOMMAND;
				}
				ENDCOMMAND:
				if ((DateTime.Now - t0).Seconds > 3) {
					DeveloppmentTools.Log("Simulated CPU processor jumpout");
					return;
				}
			}
		}

		public static ulong[] ParseBase64(string str) {
			ulong[] res = new ulong[str.Length / 8];
			if (!Regex.IsMatch(str, @"^(\w|\d|\.|\,){8,}$")) {
				// Syntax error
				return new ulong[0];
			}
			for (int i=0; i < res.Length; i++) {
				ulong new_comm = 0;
				for (int j=0; j < 8; j++) {
					char act_letter = str[i*8 + j];
					if (47 < act_letter & act_letter < 58) {
						// digit
						new_comm += (ulong) (act_letter - 48 << (8 - j));
					} else if (96 < act_letter & act_letter < 123) {
						// lowercase letters
						new_comm += (ulong) (act_letter - 87 << (8 - j));
					} else if (64 < act_letter & act_letter < 91) {
						// uppercase letters
						new_comm += (ulong) (act_letter - 29 << (8 - j));
					} else if (act_letter == '.') {
						new_comm += (ulong) 0x3e << (8 - j);
					} else if (act_letter == ','){
						new_comm += (ulong) 0x3f << (8 - j);
					} else {
						// Error
					}
				}
				res [i] = new_comm;
			}
			return res;
		}
	}

	public class RAM
	{
		public readonly uint size;
		public ushort[] content;

		public Ship Ship {
			get { return SceneGlobals.Player; }
		}

		public ushort this [int index] {
			get { return Get(index); }
			set { Set(value, index); }
		}

		public ushort this [uint index] {
			get { return Get((int) index); }
			set { Set(value, (int) index); }
		}

		public RAM (uint psize) {
			size = psize;
			content = new ushort [psize];
		}

		private ushort Get (int index) {
			if (index >= 0x100) {
				if (index < size) {
					return content [index];
				}
				return 0;
			} else {
				switch (index) {
				case 0x20: // Random number
					Random rnd = new Random(DateTime.Now.Millisecond);
					return (ushort) (rnd.Next() % 256);

				case 0x31: // Target Distance
					return Float2Short(UnityEngine.Vector3.Distance(Ship.Target.Position, Ship.Position));

				case 0x81: // Pos X
					return Float2Short(Ship.Position.x);
				case 0x82: // Pos Y
					return Float2Short(Ship.Position.y);
				case 0x83: // Pos Z
					return Float2Short(Ship.Position.z);
				case 0x84: // Vel X
					return Float2Short(Ship.Velocity.x);
				case 0x85: // Vel Y
					return Float2Short(Ship.Velocity.y);
				case 0x86: // Vel Z
					return Float2Short(Ship.Velocity.z);
				case 0x87: // Rot X
					return Float2Short(Ship.Orientation.eulerAngles.x);
				case 0x88: // Rot Y
					return Float2Short(Ship.Orientation.eulerAngles.y);
				case 0x89: // Rot Z
					return Float2Short(Ship.Orientation.eulerAngles.z);
				case 0x8a: // Ang X
					return Float2Short(Ship.AngularVelocity.x);
				case 0x8b: // Ang Y
					return Float2Short(Ship.AngularVelocity.y);
				case 0x8c: // Ang Z
					return Float2Short(Ship.AngularVelocity.z);

				case 0x90: // Ammo
					return (ushort) Ship.Ammo;
				case 0x91: // Total Ammo
					return (ushort) Ship.tot_ammo;
				case 0x92: // Fuel
					return Float2Short(Ship.Fuel);
				case 0x93: // Total Fuel
					return Float2Short(Ship.tot_rcs_fuel);
				case 0x94: // Missiles
					return Ship.Missiles;
				case 0x95: // Total Missiles
					return (ushort) Ship.tot_missiles;
				default: return 0;
				}
			}
		}

		private void Set (ushort value, int index) {
			//UnityEngine.Debug.LogFormat(" 255 < {0} < {1}: {2}", index, size, index >= 0x100 && index < size);
			if (index >= 0x100 && index < size) {
				content [index] = value;
			}
		}

		public float GetFloat (int index) {
			return Short2Float(content [index]);
		}

		public void SetFloat (float fl, int index) {
			content [index] = Float2Short(fl);
		}

		public static float Short2Float (ushort val) {
			return val / 256f * (val >> 15 == 1 ? -1 : 1);
		}

		public static ushort Float2Short (float val) {
			return (ushort) ((int) val * 256 + (val < 0 ? 0x8000 : 0x0000));
		}

		public override string ToString () {
			return String.Join("\n", System.Array.ConvertAll(content, x => x.ToString("x")));
		}
	}

	public class Disk
	{
		public string physicalpath;
		public uint size;

		public Disk(string p_physicalpath) {
			physicalpath = p_physicalpath;
		}

		public void Load () {

		}

		public void Save () {

		}
	}
}
