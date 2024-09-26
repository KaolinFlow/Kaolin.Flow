using Kaolin.Flow;
using Kaolin.Flow.Core;
using Miniscript;
using System.IO;
using System;

class Program(Interpreter interpreter, string path, bool isDebugging) : Engine(interpreter, path, isDebugging)
{
	public static void Main(string[] args)
	{
		if (args.Length > 0 && args[0] == "--test")
		{
			HostInfo.name = "Test harness";

			if (args.Length > 1 && args[1] == "--integration")
			{
				var file = args.Length < 3 || string.IsNullOrEmpty(args[2]) ? "../../../TestSuite.txt" : args[2];
				Console.WriteLine("Running test suite.\n");
				RunTestSuite(Path.GetFullPath(file));
				return;
			}

			Console.WriteLine("Miniscript test harness.\n");

			Console.WriteLine("Running unit tests.\n");
			UnitTest.Run();

			Console.WriteLine("\n");

			const string quickTestFilePath = "../../../QuickTest.ms";

			if (File.Exists(quickTestFilePath))
			{
				Console.WriteLine("Running quick test.\n");
				var stopwatch = new System.Diagnostics.Stopwatch();
				stopwatch.Start();
				RunFile(Path.GetFullPath(quickTestFilePath), true);
				stopwatch.Stop();
				Console.WriteLine($"Run time: {stopwatch.Elapsed.TotalSeconds} sec");
			}
			else
			{
				Console.WriteLine("Quick test not found, skipping...\n");
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

		Console.WriteLine("Bye!");
	}
}
