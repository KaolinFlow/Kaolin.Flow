using Kaolin.Flow.Builders;
using Kaolin.Flow.Core;
using Miniscript;

namespace Kaolin.Flow.Plugins
{

    public class Module(Engine engine) : Base(engine)
    {
        public static void Print(string s, bool lineBreak = true)
        {
            if (lineBreak) Console.WriteLine(s);
            else Console.Write(s);
        }
        public ValFunction CreateImportFunction(string path)
        {
            Value val = null!;
            ValFunction f = new FunctionBuilder()
                .AddParam("path")
                .AddParam("auto", new ValNumber(1))
                .SetCallback((context, p) =>
                {


                    string importPath = context.GetLocalString("path");
                    string fullPath = Path.GetFullPath(Path.Combine(path, importPath));
                    bool isAuto = context.GetLocalBool("auto");
                    if (p != null)
                    {
                        if (val == null) return new Intrinsic.Result(ValNull.instance, false);

                        if (isAuto)
                        {
                            string name = Path.GetFileName(fullPath);

                            TAC.Context callerContext = context.parent;
                            callerContext.SetVar(name, val);
                        }

                        return new Intrinsic.Result(val);
                    }

                    StreamReader file = new(fullPath + ".ms");
                    Parser parser = new();
                    string dirPath = Path.GetDirectoryName(Path.GetFullPath(Path.Combine(path, importPath)))!;
                    parser.Parse("import = createImport(\"" + dirPath + "\")");
                    parser.Parse("path = \"" + fullPath + "\"");
                    parser.Parse("createImport = outer.createImport(path)");
                    parser.Parse(file.ReadToEnd());
                    _ = engine.InvokeValue(new ValFunction(parser.CreateImport()), [])
                        .ContinueWith((task) =>
                        {
                            val = task.Result;


                        });
                    return new Intrinsic.Result(ValNull.instance, false);
                })
                .Function;

            return f;
        }
        public ValFunction CreateImportFunctionFactory()
        {
            return new FunctionBuilder()
                .AddParam("path")
                .SetCallback((context, p) =>
                {
                    string path = context.GetLocalString("path");

                    return new Intrinsic.Result(
                        CreateImportFunction(path)
                    );
                })
                .Function;
        }

        public override void Inject()
        {
            ValFunction factory = CreateImportFunctionFactory();
            string s = Path.GetFullPath(engine.path);

            engine.interpreter.SetGlobalValue("createImport", factory);
            engine.Eval("globals.import = createImport(\"" + Path.GetDirectoryName(s) + "\")\nglobals.path = \"" + s + "\"");

            engine.Eval("(version)[\"kaolin.flow\"] = \"1.0.0\"");
        }
    }
}