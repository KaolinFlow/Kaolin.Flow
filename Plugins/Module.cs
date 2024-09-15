using System.Net;
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
        public static bool IsHTTP(string s)
        {
            return s.StartsWith("https://") || s.StartsWith("http://");
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
                    Uri fullPath;
                    bool isLocal;

                    if (IsHTTP(importPath))
                    {
                        fullPath = new Uri(importPath);
                        isLocal = false;
                    }
                    else if (IsHTTP(path))
                    {
                        fullPath = new Uri(new Uri(path), importPath);
                        isLocal = false;
                    }
                    else
                    {
                        fullPath = new Uri(Path.Combine(new Uri(path).LocalPath, importPath));
                        isLocal = true;
                    }

                    bool isAuto = context.GetLocalBool("auto");
                    if (p != null)
                    {
                        if (val == null) return new Intrinsic.Result(ValNull.instance, false);

                        if (isAuto)
                        {
                            string name = Path.GetFileName(fullPath.AbsolutePath);

                            TAC.Context callerContext = context.parent;
                            callerContext.SetVar(name, val);
                        }

                        return new Intrinsic.Result(val);
                    }

                    Parser parser = new();
                    string fileContent;

                    if (isLocal)
                    {
                        StreamReader file = new(fullPath.AbsolutePath + ".ms");
                        fileContent = file.ReadToEnd();
                    }
                    else
                    {
                        HttpClient client = new();
                        Uri uri = new(fullPath + ".ms");
                        client.DefaultRequestHeaders.Add("Cache-Control", "max-age=0");

                        fileContent = client.GetStringAsync(uri).GetAwaiter().GetResult();
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

        public static string ToUriString(string s)
        {
            return new Uri(s).AbsoluteUri;
        }

        public override void Inject()
        {
            ValFunction factory = CreateImportFunctionFactory();
            Uri uri = new(engine.path);

            engine.interpreter.SetGlobalValue("createImport", factory);
            engine.Eval("globals.import = createImport(\"" + ToUriString(Path.GetDirectoryName(uri.LocalPath)!) + "\")\nglobals.path = \"" + uri.AbsolutePath + "\"");

            engine.Eval("(version)[\"kaolin.flow\"] = \"1.0.0\"");
        }
    }
}