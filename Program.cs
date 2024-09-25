using Kaolin.Flow;
using Kaolin.Flow.Core;
using Miniscript;

class Program : Engine
{
	public Program(Interpreter interpreter, string path, bool isDebugging) : base(interpreter, path, isDebugging)
	{
		Inject();
	}
	public static void Main(string[] args)
	{
		if (args.Length > 0 && args[0] == "--test")
		{
			HostInfo.name = "Test harness";

			if (args.Length > 1 && args[1] == "--integration")
			{
				var file = args.Length < 3 || string.IsNullOrEmpty(args[2]) ? "../../../TestSuite.txt" : args[2];
				Print("Running test suite.\n");
				RunTestSuite(Path.GetFullPath(file));
				return;
			}

			Print("Miniscript test harness.\n");

			Print("Running unit tests.\n");
			UnitTest.Run();

			Print("\n");

			const string quickTestFilePath = "../../../QuickTest.ms";

			if (File.Exists(quickTestFilePath))
			{
				Print("Running quick test.\n");
				var stopwatch = new System.Diagnostics.Stopwatch();
				stopwatch.Start();
				RunFile(Path.GetFullPath(quickTestFilePath), true);
				stopwatch.Stop();
				Print($"Run time: {stopwatch.Elapsed.TotalSeconds} sec");
			}
			else
			{
				Print("Quick test not found, skipping...\n");
			}
			return;
		}

		if (args.Length > 0)
		{
			RunFile(Path.GetFullPath(args[0]));
			return;
		}

		Interpreter repl = new();

		repl.Compile();

		Engine engine = new(repl, Utils.WrapPath(Path.GetFullPath("./")), false);

		repl.implicitOutput = repl.standardOutput;

		Console.WriteLine("Kaolin.Flow (MiniScript)");
		engine.REPL("version");

		while (true)
		{
			Console.Write(repl.NeedMoreInput() ? ">>> " : "> ");
			string? inp = Console.ReadLine();
			if (inp == null) break;
			engine.REPL(inp);
		}

		Print("Bye!");
	}
}
