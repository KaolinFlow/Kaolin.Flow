using System.Collections;
using System.Diagnostics;
using Kaolin.Flow.Builders;
using Kaolin.Flow.Core;
using Miniscript;

namespace Kaolin.Flow.Plugins
{
    public class Machine(Engine engine) : Base(engine)
    {
        readonly Dictionary<string, Value> evalMemo = [];
        public override void Inject()
        {
            ValMap env = new()
            {
                evalOverride = (Value key, out Value value) =>
                {
                    value = Utils.Cast(Environment.GetEnvironmentVariable(((ValString)key).value)!);

                    return true;
                },
                assignOverride = (Value key, Value value) =>
                {
                    Environment.SetEnvironmentVariable(((ValString)key).value, ((ValString)value).value);

                    return false;
                },
            };

            foreach (DictionaryEntry e in Environment.GetEnvironmentVariables())
            {
                env.map.Add(Utils.Cast((string)e.Key), Utils.Cast((string)e.Value!));
            }

            ValList shellArgs = new();

            foreach (string s in Environment.GetCommandLineArgs())
            {
                shellArgs.values.Add(Utils.Cast(s));
            }

            engine.interpreter.SetGlobalValue("env", env);
            engine.interpreter.SetGlobalValue("shellArgs", shellArgs);
            engine.interpreter.SetGlobalValue("eval", new FunctionBuilder().AddParam("code").SetCallback((context, p) =>
            {
                string s = Guid.NewGuid().ToString();

                if (p != null)
                {
                    string key = ((ValString)p.result).value;
                    bool ok = evalMemo.TryGetValue(key, out Value val);

                    if (!ok) return new Intrinsic.Result(p.result, false);

                    evalMemo.Remove(key);

                    return new Intrinsic.Result(val!, true);
                }

                engine.EvalValue(context.GetLocalString("code"))
                    .ContinueWith((t) =>
                    {
                        evalMemo.Add(s, t.Result);
                    });

                return new Intrinsic.Result(new ValString(s), false);
            }).Function);
            engine.interpreter.SetGlobalValue("exit", new FunctionBuilder().AddParam("resultCode", new ValNumber(0)).SetCallback((context, p) =>
            {
                Environment.Exit(context.GetLocalInt("resultCode"));

                return Intrinsic.Result.Null;
            }).Function);
            engine.interpreter.SetGlobalValue("input", new FunctionBuilder().AddParam("prompt", new ValString("")).SetCallback((context, p) =>
            {
                string s = context.GetLocalString("prompt");
                if (s.Length != 0) Engine.Print(s, false);
                string v = Console.ReadLine()!;

                return new Intrinsic.Result(v);
            }).Function);
            engine.interpreter.SetGlobalValue("exec", new FunctionBuilder()
                .AddParam("cmd")
                .AddParam("timeout", new ValNumber(30))
                .SetCallback((context, p) =>
                {
                    string s = context.GetLocalString("cmd");
                    int t = context.GetLocalInt("timeout");

                    List<string> args = [.. s.Split(" ")];
                    string name = args[0];

                    args.RemoveAt(0);

                    Process process = Process.Start(name, string.Join(" ", args));

                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.UseShellExecute = false;
                    process.Start();

                    Task.Delay(t * 1000).ContinueWith((t) =>
                    {
                        process.Close();
                    });

                    process.WaitForExit();

                    return new Intrinsic.Result(new MapBuilder().AddProp("errors", Utils.Cast(process.StandardError.ReadToEnd())).AddProp("status", Utils.Cast(process.ExitCode)).AddProp("output", Utils.Cast(process.StandardOutput.ReadToEnd())).map);
                }).Function
            );
        }
    }
}