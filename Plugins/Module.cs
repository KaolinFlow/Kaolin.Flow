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
            ValFunction f = new FunctionBuilder("createImport")
                .AddParam("path")
                .AddParam("auto", new ValNumber(1))
                .SetCallback((context, p) =>
                {
                    string importPath = context.GetLocalString("path");
                    Uri fullPath;
                    bool isAuto = context.GetLocalBool("auto");
                    var matched = MatchPattern(importPath);

                    if (matched != null)
                    {
                        ValMap map = (ValMap)matched;
                        map.TryGetValue("module", out Value value);
                        if (isAuto)
                        {
                            map.TryGetValue("name", out Value n);
                            string name = ((ValString)n).value;

                            TAC.Context callerContext = context.parent;
                            callerContext.SetVar(name, value);
                        }

                        return new Intrinsic.Result(value);
                    }
                    fullPath = Utils.ResolvePath(path, importPath + ".ms");

                    Parser parser = new();
                    string fileContent;

                    if (!Utils.IsHTTP(fullPath.AbsoluteUri))
                    {
                        StreamReader file = new(fullPath.AbsolutePath);
                        fileContent = file.ReadToEnd();
                    }
                    else
                    {
                        fileContent = client.GetStringAsync(fullPath).GetAwaiter().GetResult();
                    }

                    parser.errorContext = fullPath.AbsoluteUri;

                    string dirPath = new Uri(fullPath, "./").AbsoluteUri;
                    parser.Parse("import = createImport(\"" + dirPath + "\")");
                    parser.Parse("path = \"" + fullPath + "\"");
                    parser.Parse("createImport = outer.createImport(path)");
                    parser.Parse(fileContent);

                    var val = engine.InvokeValue(new ValFunction(parser.CreateImport()), []);

                    if (isAuto)
                    {
                        string name = Path.GetFileName(importPath);

                        TAC.Context callerContext = context.parent;
                        callerContext.SetVar(name, val);
                    }
                    return new Intrinsic.Result(val);


                })
                .Function;

            return f;
        }

        public static ValMap NewModule(Value module, string name)
        {
            return new MapBuilder(Engine.New(ModuleClass))
                .AddProp("module", module)
                .AddProp("name", new ValString(name))
                .map;
        }

        public static ValMap NewModule(string name, Value module)
        {
            return new MapBuilder(Engine.New(ModuleClass))
                .AddProp("module", module)
                .AddProp("name", new ValString(name))
                .map;
        }

        public static readonly ValMap ModuleClass = new();
        public static readonly ValFunction NewModuleFunction = new FunctionBuilder()
            .AddParam("module")
            .AddParam("name")
            .SetCallback((context, p) =>
            {
                return new Intrinsic.Result(
                    NewModule(context.GetLocal("module"), context.GetLocalString("name"))
                );
            })
            .Function;
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

            map.TryGetValue(s, out Value v);

            if (v != null)
            {
                return engine.InvokeValue((ValFunction)v, [Utils.Cast(s)]);
            }

            return null;
        }

        public override void Inject()
        {
            ValFunction factory = CreateImportFunctionFactory();
            Uri uri = new(engine.path);

            engine.interpreter.SetGlobalValue("createImport", factory);
            engine.interpreter.SetGlobalValue("imports", new ValMap());
            engine.interpreter.SetGlobalValue("Module", ModuleClass);
            engine.interpreter.SetGlobalValue("newModule", NewModuleFunction);
            engine.Eval("globals.import = createImport(\"" + ToUriString(Path.GetDirectoryName(uri.AbsolutePath)!) + "\")\nglobals.path = \"" + uri.AbsoluteUri + "\"");
            engine.Eval("(version)[\"kaolin.flow\"] = \"" + ThisAssembly.AssemblyInformationalVersion + "\"");
        }
    }
}