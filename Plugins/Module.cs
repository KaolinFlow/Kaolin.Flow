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
                    parser.Parse("import = outer.importMeta.createImport(\"" + dirPath + "\")");
                    parser.Parse("importMeta = new outer.importMeta");
                    parser.Parse("importMeta[\"path\"] = \"" + fullPath + "\"");
                    parser.Parse("importMeta[\"createImport\"] = globals.importMeta.createImport(importMeta.path)");
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
            ValMap mv = (ValMap)engine.interpreter.GetGlobalValue("importMeta");
            mv.TryGetValue("imports", out Value im);

            ((ValMap)im).TryGetValue(s, out Value v);

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

            engine.interpreter.SetGlobalValue("importMeta",
                new MapBuilder()
                    .AddProp("path", Utils.Cast(uri.AbsoluteUri))
                    .AddProp("createImport", factory)
                    .AddProp("newModule", NewModuleFunction)
                    .AddProp("Module", ModuleClass)
                    .AddProp("imports", new ValMap())
                    .map
            );
            engine.Eval("globals.import = importMeta.createImport(\"" + ToUriString(Path.GetDirectoryName(uri.AbsolutePath)!) + "\")");
            engine.interpreter.GetGlobalValue("version").SetElem(Utils.Cast("kaolin.flow"), Utils.Cast(ThisAssembly.AssemblyInformationalVersion));
        }
    }
}