using Kaolin.Flow;
using Miniscript;

class Program(Interpreter interpreter, string path, bool isDebugging) : Runtime(interpreter, path, isDebugging)
{
	public static void Main(string[] args)
	{
		if (args.Length > 0 && args[0] == "--test")
		{
			HostInfo.name = "Test harness";

			if (args.Length > 1 && args[1] == "--integration")
			{
				var file = args.Length < 3 || string.IsNullOrEmpty(args[2]) ? "../../../TestSuite.txt" : args[2];
				Print("Running test suite.\n");
				RunTestSuite(file);
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
				RunFile(quickTestFilePath, true);
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
			RunFile(args[0]);
			return;
		}

		Interpreter repl = new();

		repl.Compile();

		Runtime runtime = new(repl, "./", false);

		repl.implicitOutput = repl.standardOutput;

		Console.WriteLine("Kaolin.Flow (MiniScript)");
		runtime.REPL("version");

		while (true)
		{
			Console.Write(repl.NeedMoreInput() ? ">>> " : "> ");
			string? inp = Console.ReadLine();
			if (inp == null) break;
			runtime.REPL(inp);
		}

		Print("Bye!");
	}
}