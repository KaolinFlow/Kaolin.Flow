using Kaolin.Flow.Builders;
using Miniscript;

namespace Kaolin.Flow.Core
{
    public class Engine
    {
        public readonly ErrorHandler errorHandler = new();
        public readonly Interpreter interpreter;
        public readonly bool isDebugging;
        public string path;

        public Engine(Interpreter interpreter, string path, bool isDebugging = false)
        {
            this.path = path;
            this.isDebugging = isDebugging;
            this.interpreter = interpreter;

            interpreter.errorOutput = (output, _) =>
            {
                errorHandler.Trigger(output);
            };
        }

        public void REPL(string sourceLine, double timeLimit = 60)
        {
            interpreter.REPL(sourceLine, timeLimit);
        }

        public void Debug(string s)
        {
            if (isDebugging) Console.WriteLine(s);
        }

        public static void Print(string s, bool lineBreak = true)
        {
            if (lineBreak) Console.WriteLine(s);
            else Console.Write(s);
        }

        public static void ListErrors(Script script)
        {
            if (script.errors == null)
            {
                Print("No errors.");
                return;
            }
            foreach (Error err in script.errors)
            {
                Print(string.Format("{0} on line {1}: {2}",
                    err.type, err.lineNum, err.description));
            }

        }

        public static void Test(List<string> sourceLines, int sourceLineNum,
                         List<string> expectedOutput, int outputLineNum)
        {
            expectedOutput ??= [];
            //		Console.WriteLine("TEST (LINE {0}):", sourceLineNum);
            //		Console.WriteLine(string.Join("\n", sourceLines));
            //		Console.WriteLine("EXPECTING (LINE {0}):", outputLineNum);
            //		Console.WriteLine(string.Join("\n", expectedOutput));

            Interpreter miniscript = new(sourceLines);
            List<string> actualOutput = [];
            miniscript.standardOutput = (string s, bool eol) => actualOutput.Add(s);
            miniscript.errorOutput = miniscript.standardOutput;
            miniscript.implicitOutput = (s, eol) => { };
            miniscript.RunUntilDone(60, false);

            //		Console.WriteLine("ACTUAL OUTPUT:");
            //		Console.WriteLine(string.Join("\n", actualOutput));

            int minLen = expectedOutput.Count < actualOutput.Count ? expectedOutput.Count : actualOutput.Count;
            for (int i = 0; i < minLen; i++)
            {
                if (actualOutput[i] != expectedOutput[i])
                {
                    Print(string.Format("TEST FAILED AT LINE {0}\n  EXPECTED: {1}\n    ACTUAL: {2}",
                        outputLineNum + i, expectedOutput[i], actualOutput[i]));
                }
            }
            if (expectedOutput.Count > actualOutput.Count)
            {
                Print(string.Format("TEST FAILED: MISSING OUTPUT AT LINE {0}", outputLineNum + actualOutput.Count));
                for (int i = actualOutput.Count; i < expectedOutput.Count; i++)
                {
                    Print("  MISSING: " + expectedOutput[i]);
                }
            }
            else if (actualOutput.Count > expectedOutput.Count)
            {
                Print(string.Format("TEST FAILED: EXTRA OUTPUT AT LINE {0}", outputLineNum + expectedOutput.Count));
                for (int i = expectedOutput.Count; i < actualOutput.Count; i++)
                {
                    Print("  EXTRA: " + actualOutput[i]);
                }
            }

        }

        public static void RunTestSuite(string path)
        {
            StreamReader file = new(path);
            if (file == null)
            {
                Print("Unable to read: " + path);
                return;
            }

            List<string>? sourceLines = null;
            List<string>? expectedOutput = null;
            int testLineNum = 0;
            int outputLineNum = 0;

            string? line = file.ReadLine();
            int lineNum = 1;
            while (line != null)
            {
                if (line.StartsWith("===="))
                {
                    if (sourceLines != null) Test(sourceLines, testLineNum, expectedOutput!, outputLineNum);
                    sourceLines = null;
                    expectedOutput = null;
                }
                else if (line.StartsWith("----"))
                {
                    expectedOutput = [];
                    outputLineNum = lineNum + 1;
                }
                else if (expectedOutput != null)
                {
                    expectedOutput.Add(line);
                }
                else
                {
                    if (sourceLines == null)
                    {
                        sourceLines = [];
                        testLineNum = lineNum;
                    }
                    sourceLines.Add(line);
                }

                line = file.ReadLine();
                lineNum++;
            }
            if (sourceLines != null) Test(sourceLines, testLineNum, expectedOutput!, outputLineNum);
            Print("\nIntegration tests complete.\n");
        }
        public static void RunFile(string path, bool isDebugging = false, bool dumpTAC = false)
        {
            StreamReader file = new(path);
            if (file == null)
            {
                Print("Unable to read: " + path);
                return;
            }
            Interpreter miniscript = new()
            {
                standardOutput = Print
            };
            Parser parser = new()
            {
                errorContext = Utils.WrapPath(path)
            };
            parser.Parse(file.ReadToEnd());
            miniscript.Compile();
            miniscript.vm.ManuallyPushCall(new ValFunction(parser.CreateImport()));

            if (dumpTAC && miniscript.vm != null)
            {
                miniscript.vm.DumpTopContext();
            }

            Runtime core = new(miniscript, Utils.WrapPath(path), isDebugging);

            core.Run();
        }

        public void Run()
        {
            while (interpreter.Running())
            {
                interpreter.RunUntilDone(int.MaxValue, false);
            }
        }


        public void Invoke(ValFunction function, Value[] arguments)
        {
            interpreter.vm.ManuallyPushCall(function, null!, [.. arguments]);
        }
        public Value InvokeValue(ValFunction function, Value[] arguments)
        {
            Value? val = null;

            interpreter.vm.ManuallyPushCall(new FunctionBuilder().SetCallback((context, partialResult) =>
            {

                if (partialResult != null)
                {
                    Value value = context.GetTemp(0);
                    val = value;

                    return new Intrinsic.Result(value);
                }

                interpreter.vm.ManuallyPushCall(function, new ValTemp(0), [.. arguments]);

                return new Intrinsic.Result(ValNull.instance, false);
            }).Function, null!);
            while (interpreter.Running() && val == null)
            {
                interpreter.vm.Step();
            }

            return val!;
        }

        public static ValMap New(ValMap Class)
        {
            ValMap newMap = new();
            newMap.SetElem(ValString.magicIsA, Class);

            return newMap;
        }

        public void Eval(string s, string path = "")
        {
            Parser parser = new()
            {
                errorContext = (path.Length > 0 ? path + " " : "") + "eval"
            };
            parser.Parse(s);

            Invoke(new ValFunction(parser.CreateImport()), []);
        }
        public Value EvalValue(string s, string path = "")
        {
            Parser parser = new()
            {
                errorContext = (path.Length > 0 ? path + " " : "") + "eval"
            };
            parser.Parse(s);

            return InvokeValue(new ValFunction(parser.CreateImport()), []);
        }
    }
}