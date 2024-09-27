using Kaolin.Flow.Builders;
using Miniscript;
using System.Collections.Generic;
using System.IO;
using System;

namespace Kaolin.Flow.Core
{
    public class Engine
    {
        public readonly ErrorHandler errorHandler;
        public readonly Interpreter interpreter;
        public readonly bool isDebugging;
        public string path;

        public Engine(Interpreter interpreter, string path, bool isDebugging = false)
        {
            this.path = path;
            this.isDebugging = isDebugging;
            this.interpreter = interpreter;
            errorHandler = new(this);

            interpreter.errorOutput = (output, _) =>
            {
                errorHandler.Trigger(output);
            };

            Inject();
        }

        public void REPL(string sourceLine, double timeLimit = 60)
        {
            interpreter.REPL(sourceLine, timeLimit);
        }

        public void Debug(string s)
        {
            if (isDebugging) Console.WriteLine(s);
        }

        public virtual void Print(string s, bool lineBreak = true)
        {
            if (lineBreak) Console.WriteLine(s);
            else Console.Write(s);
        }
        public static void ListErrors(Script script)
        {
            if (script.errors == null)
            {
                Console.WriteLine("No errors.");
                return;
            }
            foreach (Error err in script.errors)
            {
                Console.WriteLine(string.Format("{0} on line {1}: {2}",
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
                    Console.WriteLine(string.Format("TEST FAILED AT LINE {0}\n  EXPECTED: {1}\n    ACTUAL: {2}",
                        outputLineNum + i, expectedOutput[i], actualOutput[i]));
                }
            }
            if (expectedOutput.Count > actualOutput.Count)
            {
                Console.WriteLine(string.Format("TEST FAILED: MISSING OUTPUT AT LINE {0}", outputLineNum + actualOutput.Count));
                for (int i = actualOutput.Count; i < expectedOutput.Count; i++)
                {
                    Console.WriteLine("  MISSING: " + expectedOutput[i]);
                }
            }
            else if (actualOutput.Count > expectedOutput.Count)
            {
                Console.WriteLine(string.Format("TEST FAILED: EXTRA OUTPUT AT LINE {0}", outputLineNum + expectedOutput.Count));
                for (int i = expectedOutput.Count; i < actualOutput.Count; i++)
                {
                    Console.WriteLine("  EXTRA: " + actualOutput[i]);
                }
            }

        }

        public static void RunTestSuite(string path)
        {
            StreamReader file = new(path);
            if (file == null)
            {
                Console.WriteLine("Unable to read: " + path);
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
            Console.WriteLine("\nIntegration tests complete.\n");
        }
        public static Engine? RunFile(string path, bool isDebugging = false, bool dumpTAC = false)
        {
            StreamReader file = new(path);
            if (file == null)
            {
                Console.WriteLine("Unable to read: " + path);
                return null;
            }
            Interpreter miniscript = new()
            {
                standardOutput = (content, lineBreak) =>
                {
                    if (lineBreak) Console.WriteLine(content);
                    else Console.Write(content);
                }
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

            Engine core = new(miniscript, Utils.WrapPath(path), isDebugging);

            core.Run();

            return core;
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
            bool isDone = false;

            interpreter.vm.ManuallyPushCall(new FunctionBuilder().SetCallback((context, partialResult) =>
            {
                if (partialResult != null)
                {
                    isDone = true;

                    return Intrinsic.Result.Null;
                }

                interpreter.vm.ManuallyPushCall(function, null!, [.. arguments]);

                return new Intrinsic.Result(ValNull.instance, false);
            }).Function, null!);
            while (interpreter.Running() && !isDone)
            {
                interpreter.vm.Step();
            }
        }
        public Value InvokeValue(ValFunction function, Value[] arguments)
        {
            Value val = null!;
            bool isDone = false;

            interpreter.vm.ManuallyPushCall(new FunctionBuilder().SetCallback((context, partialResult) =>
            {
                if (partialResult != null)
                {
                    Value value = context.GetTemp(0);
                    val = value;
                    isDone = true;

                    if (val == null) val = ValNull.instance;

                    return new Intrinsic.Result(value);
                }

                interpreter.vm.ManuallyPushCall(function, new ValTemp(0), [.. arguments]);

                return new Intrinsic.Result(ValNull.instance, false);
            }).Function, null!);
            while (interpreter.Running() && !isDone)
            {
                interpreter.vm.Step();
            }

            return val;
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
        public virtual void Inject()
        {
            interpreter.SetGlobalValue("KF", new MapBuilder().map);

            _ = new Plugins.Module(this);
            _ = new Plugins.Machine(this);
            _ = new Plugins.Dev(this);
            _ = new Plugins.Http(this);
            _ = new Plugins.Native(this);
            _ = new Plugins.Loader(this);
            _ = new Plugins.Error(this);
        }
    }
}
