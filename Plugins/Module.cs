using Kaolin.Flow.Builders;
using Kaolin.Flow.Core;
using Miniscript;

namespace Kaolin.Flow.Plugins
{

    public class Module : Base
    {
        public Module(Engine engine) : base(engine)
        {
            client.DefaultRequestHeaders.Add("Cache-Control", "max-age=0");
        }

        readonly HttpClient client = new();
        public static void Print(string s, bool lineBreak = true)
        {
            if (lineBreak) Console.WriteLine(s);
            else Console.Write(s);
        }
        public ValFunction CreateImportFunction(string path)
        {
            Value val = null!;
            ValFunction f = new FunctionBuilder("createImport")
                .AddParam("path")
                .AddParam("auto", new ValNumber(1))
                .SetCallback((context, p) =>
                {
                    string importPath = context.GetLocalString("path");
                    Uri fullPath;

                    Value? matched = MatchPattern(importPath);
                    bool isAuto = context.GetLocalBool("auto");

                    if (matched != null)
                    {
                        if (matched.GetType() != typeof(ValString))
                        {
                            if (isAuto)
                            {
                                string name = Path.GetFileName(importPath);

                                TAC.Context callerContext = context.parent;
                                callerContext.SetVar(name, matched);
                            }

                            return new Intrinsic.Result(matched);
                        }

                        string path = ((ValString)engine.interpreter.GetGlobalValue("path")).value;
                        fullPath = Engine.ResolvePath(new Uri(new Uri(path), "./").AbsoluteUri, matched + ".ms");
                    }
                    else
                    {
                        fullPath = Engine.ResolvePath(path, importPath + ".ms");
                    }

                    if (p != null)
                    {
                        if (val == null) return new Intrinsic.Result(ValNull.instance, false);

                        if (isAuto)
                        {
                            string name = Path.GetFileName(importPath);

                            TAC.Context callerContext = context.parent;
                            callerContext.SetVar(name, val);
                        }

                        return new Intrinsic.Result(val);
                    }

                    Parser parser = new();
                    string fileContent;

                    if (!Engine.IsHTTP(fullPath.AbsoluteUri))
                    {
                        StreamReader file = new(fullPath.AbsolutePath);
                        fileContent = file.ReadToEnd();
                    }
                    else
                    {

                        fileContent = client.GetStringAsync(fullPath).GetAwaiter().GetResult();
                    }

                    string dirPath = new Uri(fullPath, "./").AbsoluteUri;
                    parser.Parse("import = createImport(\"" + dirPath + "\")");
                    parser.Parse("path = \"" + fullPath + "\"");
                    parser.Parse("createImport = outer.createImport(path)");
                    parser.Parse(fileContent);
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
            return new FunctionBuilder("createImportFactory")
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

        public static string ToUriString(string s)
        {
            return new Uri(s).AbsoluteUri;
        }

        public Value? MatchPattern(string s)
        {
            ValMap map = (ValMap)engine.interpreter.GetGlobalValue("imports");

            foreach (var entry in map.map)
            {
                var key = ((ValString)entry.Key).value;

                if (key == s) return entry.Value;

            }

            return null;
        }

        public override void Inject()
        {
            ValFunction factory = CreateImportFunctionFactory();
            Uri uri = new(engine.path);

            engine.interpreter.SetGlobalValue("createImport", factory);
            engine.interpreter.SetGlobalValue("imports", new ValMap());
            engine.Eval("globals.import = createImport(\"" + ToUriString(Path.GetDirectoryName(uri.LocalPath)!) + "\")\nglobals.path = \"" + uri.AbsoluteUri + "\"");

            engine.Eval("(version)[\"kaolin.flow\"] = \"1.0.0\"");
        }
    }
}