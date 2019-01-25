using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NMS.OS
{
	public class OperatingSystem
	{
		private int assemblycommandnum = 0;

		public ConsoleBehaviour console = null;
		public static Queue<Error> errors = new Queue<Error>();

		public CPU cpu;
		public RAM ram;
		public Disk disc;

		public string output;

		public Ship Attached { get; set; }

		public OperatingSystem (ConsoleBehaviour cons, Ship ship, uint ram_size=0x1000) {
			console = cons;
			Attached = ship;

			ram = new RAM(ram_size);
			cpu = new CPU(ship, this);
			Boot();

			assemblycommandnum = System.Enum.GetValues(typeof(AssemblyCommands)).Length;
		}

		public void Boot () {
			/*
			cpu.Execute(CompileNMS(@"
CLS
SET 0 x101
SET s== x100
LABEL:
ADD 1 &x101 x101
JEQ &x101 19 :LABEL2
JMP :LABEL
LABEL2:
PRT x100 18
SET s W 276
SET sel 277
SET sco 278
SET sme 279
SET s t 280
SET so  281
SET sth 282
SET se  283
SET sNM 284
SET sS- 285
SET sOp 286
SET ser 287
SET sat 288
SET sin 289
SET sgS 290
SET sys 291
SET ste 291
SET sm  293
PRT 276 18
PRT 1 18
END
			"));
			*/
		}

		public static ulong[] CompileNMS (string code) {
			RegexOptions options = RegexOptions.Multiline;

			string[] lines = code.Split('\n');
			Dictionary<string, ushort> labels = new Dictionary<string, ushort>();
			for (ushort i=0; i < lines.Length; i++) {
				if (Regex.IsMatch(lines [i], "^.+:$")) {
					labels.Add(lines [i].Substring(0, lines [i].Length - 1), i);
				}
			}
			
			lines = System.Array.FindAll(lines, x => Regex.IsMatch(x, @"^[A-Z]{3}( &?(s..|d?\d{1,3}|x[0-9a-fA-F]{1,3}|:.+:)){0,3}"));
			ulong[] res = new ulong[lines.Length];
			for (int i = 0; i < lines.Length; i++) {
				string line = lines[i];
				string cmd_string = Regex.Match(line, "^[A-Z]{3}").Value;
				Match[] args = new Match[] { Match.Empty, Match.Empty, Match.Empty };
				Regex.Matches(line, @"&?(s..|d?\d{1,3}|x[0-9a-fA-F]{1,3}|:.+)", options).CopyTo(args, 0);
				string[] args_str = System.Array.ConvertAll(args, x => x.Success ? x.Value : string.Empty);
				ulong line_command = 0;

				byte command = 0;
				for (byte j=0; j < 255; j++) {
					if (((AssemblyCommands) j).ToString() == cmd_string)
						command = j;
				}
				line_command += ((ulong) command) << 56;

				if (args_str [0].StartsWith("&")) {
					args_str [0] = args_str [0].Substring(1);
					line_command += 0x0004000000000000;
				}
				if (args_str [1].StartsWith("&")) {
					args_str [1] = args_str [1].Substring(1);
					line_command += 0x0002000000000000;
				}
				if (args_str [2].StartsWith("&")) {
					args_str [2] = args_str [2].Substring(1);
					line_command += 0x0001000000000000;
				}
				ushort[] num = new ushort[3] { 0, 0, 0 };

				// Parse the numbers
				for (int j=0; j < 3; j++) {
					if (args_str [j].Length == 0) goto END;
					switch(args_str[j][0]) {
					case 'd':
						// Decimal
						if (args_str [j].Length <= 1) goto END;
						num [j] = ushort.Parse(args_str [j].Substring(1));
						break;
					case 'x':
						// Hexadecimal
						if (args_str [j].Length <= 1) goto END;
						num [j] = ushort.Parse(args_str [j].Substring(1), System.Globalization.NumberStyles.HexNumber);
						//UnityEngine.Debug.LogFormat("{0} -> {1}", j, num [j]);
						break;
					case 's':
						// ASCII
						if (args_str [j].Length == 2) {
							num [j] = (ushort) ((args_str [j] [1] << 8) + ' ');
						} else if (args_str [j].Length == 3) {
							num [j] = (ushort) ((args_str [j] [1] << 8) + args_str [j] [2]);
						}
						break;
					case ':':
						num [j] = labels.ContainsKey(args_str [j].Substring(1)) ? labels [args_str [j].Substring(1)] : (ushort) 0;
						break;
					default:
						// Default is  Decimal
						num [j] = 0;
						ushort.TryParse(args_str [j], out num[j]);
						break;
					}
					END:;
				}

				line_command += (ulong) num [0] << 32;
				line_command += (ulong) num [1] << 16;
				line_command += num [2];
				
				//UnityEngine.Debug.LogFormat("{0:x} | {1:x}", num[1], line_command);

				res [i] = line_command;
			}
			return res;
		}

		public void ShowConsole () {
			if (console == null) return;
			SceneGlobals.Paused = true;
			console.ConsolePos = ConsolePosition.shown;
		}

		public void Update () {
			if (console != null) {
				if (console.HasInput && console.current_conversation == Conversation.Null) {
					string input = console.ReadLine();
					console.WriteLine(">>>" + input);

					string prefix = input.Split(' ')[0];
					switch (prefix) {
					case "exit":
						console.ConsolePos = ConsolePosition.hidden;
						break;
					case "cls":
						cpu.Execute(new ulong [] { 0x0042000000000000 });
						break;
					case "cpu":
						ulong cpu_inp = System.UInt64.Parse(input.Substring(4, 16), System.Globalization.NumberStyles.HexNumber);
						cpu.Execute(new ulong [] { cpu_inp });
						break;
					case "nms":
						if (SceneGlobals.Player != null) {
							console.WriteLine(SceneGlobals.Player.low_ai.Execute(input.Substring(prefix.Length + 1)));
						}
						break;
					case "show_nms":
						if (SceneGlobals.Player != null) {
							console.WriteLine(SceneGlobals.Player.low_ai.ShowCommands());
						}
						break;
					default:
						console.WriteLine(string.Format("Command {0} not known", prefix));
						break;
					}
				}
				while (errors.Count > 0) {
					console.WriteLine(errors.Dequeue().Display());
				}
			}

		}
		
		public void ThrowError (string message, uint line = 0u, ErrorType type = ErrorType.default_, string file = "") {
			errors.Enqueue(new Error() { message = message, line = line, file = file, type = type });
		}
		
	}

	public enum AssemblyCommands{
		NUL = 0x00,
		SET = 0x01,
		F2I = 0x02,
		I2F = 0x03,
		END = 0x04,

		JMP = 0x10,
		JEQ = 0x11,
		JGT = 0x12,
		JGE = 0x13,

		ADD = 0x20,
		SUB = 0x21,
		MUL = 0x22,
		DIV = 0x23,

		PRT = 0x40,
		INP = 0x41,
		CLS = 0x42,

		TRN = 0x80,
		RCS = 0x81,
		ENG = 0x82,
		HLD = 0x83,
		LCK = 0x84,

		SHT = 0x90,
		SHF = 0x91,
		MIS = 0x92,

		SLP = 0xa0
	}
}
