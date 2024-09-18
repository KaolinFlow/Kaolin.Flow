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
            Value matchedObject = null!;
            ValFunction f = new FunctionBuilder("createImport")
                .AddParam("path")
                .AddParam("auto", new ValNumber(1))
                .SetCallback((context, p) =>
                {
                    string importPath = context.GetLocalString("path");
                    Uri fullPath;
                    bool isAuto = context.GetLocalBool("auto");

                    if (p != null)
                    {
                        ValMap o = (ValMap)p.result;
                        o.TryGetValue("isProcessing", out Value t1);
                        bool isProcessing = ((ValNumber)t1).value == 1;

                        if (!isProcessing)
                        {
                            o.TryGetValue("isMatch", out Value t2);
                            bool isMatch = ((ValNumber)t2).value == 1;
                            if (isMatch)
                            {
                                if (matchedObject == null) return new Intrinsic.Result(p.result, false);
                                if (isAuto)
                                {
                                    string name = Path.GetFileName(importPath);

                                    TAC.Context callerContext = context.parent;
                                    callerContext.SetVar(name, matchedObject);
                                }

                                return new Intrinsic.Result(matchedObject);

                            }
                            else
                            {
                                fullPath = Engine.ResolvePath(path, importPath + ".ms");
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

                            parser.errorContext = fullPath.AbsoluteUri;

                            string dirPath = new Uri(fullPath, "./").AbsoluteUri;
                            parser.Parse("import = createImport(\"" + dirPath + "\")");
                            parser.Parse("path = \"" + fullPath + "\"");
                            parser.Parse("createImport = outer.createImport(path)");
                            parser.Parse(fileContent);
                            engine.InvokeValue(new ValFunction(parser.CreateImport()), [])
                                .ContinueWith((task) =>
                                {
                                    val = task.Result;
                                });
                            return new Intrinsic.Result(new MapBuilder().AddProp("isProcessing", new ValNumber(1)).map, false);
                        }
                        else if (val != null)
                        {
                            if (isAuto)
                            {
                                string name = Path.GetFileName(importPath);

                                TAC.Context callerContext = context.parent;
                                callerContext.SetVar(name, val);
                            }
                            return new Intrinsic.Result(val);
                        }
                        else
                        {
                            return new Intrinsic.Result(p.result, false);
                        }
                    }


                    var matched = MatchPattern(importPath);

                    if (matched != null)
                    {
                        matched.ContinueWith((task) =>
                        {
                            matchedObject = task.Result;
                        });

                        return new Intrinsic.Result(new MapBuilder().AddProp("isMatch", new ValNumber(1)).AddProp("isProcessing", new ValNumber(0)).map, false);
                    }
                    else
                    {
                        return new Intrinsic.Result(new MapBuilder().AddProp("isMatch", new ValNumber(0)).AddProp("isProcessing", new ValNumber(0)).map, false);
                    }

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

        public Task<Value>? MatchPattern(string s)
        {
            ValMap map = (ValMap)engine.interpreter.GetGlobalValue("imports");

            foreach (var entry in map.map)
            {
                var key = ((ValString)entry.Key).value;

                if (key == s) return engine.InvokeValue((ValFunction)entry.Value, [Utils.Cast(s)]);

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