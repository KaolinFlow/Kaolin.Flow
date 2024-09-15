using System.IO.Enumeration;
using System.Net;
using System.Text.RegularExpressions;
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

                    string? matched = MatchPattern(importPath);

                    if (matched != null)
                    {
                        string path = ((ValString)engine.interpreter.GetGlobalValue("path")).value;
                        fullPath = ResolvePath(new Uri(new Uri(path), "./").AbsoluteUri, matched + ".ms");
                    }
                    else
                    {
                        fullPath = ResolvePath(path, importPath + ".ms");
                    }

                    bool isAuto = context.GetLocalBool("auto");
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

                    if (!IsHTTP(fullPath.AbsoluteUri))
                    {
                        StreamReader file = new(fullPath.AbsolutePath);
                        fileContent = file.ReadToEnd();
                    }
                    else
                    {
                        HttpClient client = new();
                        client.DefaultRequestHeaders.Add("Cache-Control", "max-age=0");

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

        public static Uri ResolvePath(string basePath, string relativePath)
        {
            if (IsHTTP(relativePath))
            {
                return new Uri(relativePath);
            }
            else if (IsHTTP(basePath))
            {
                return new Uri(new Uri(basePath), relativePath);
            }
            else
            {
                return new Uri(Path.Combine(new Uri(basePath).AbsolutePath, relativePath));
            }
        }

        public string? MatchPattern(string s)
        {
            ValMap map = (ValMap)engine.interpreter.GetGlobalValue("imports");

            foreach (var entry in map.map)
            {
                var key = ((ValString)entry.Key).value;
                var val = ((ValString)entry.Value).value;


                if (key == s) return val;
            }

            return null;
        }

        public override void Inject()
        {
            ValFunction factory = CreateImportFunctionFactory();
            Uri uri = new(engine.path);

            engine.interpreter.SetGlobalValue("createImport", factory);
            engine.interpreter.SetGlobalValue("imports", new ValMap());
            engine.Eval("globals.import = createImport(\"" + ToUriString(Path.GetDirectoryName(uri.LocalPath)!) + "\")\nglobals.path = \"" + uri.AbsolutePath + "\"");

            engine.Eval("(version)[\"kaolin.flow\"] = \"1.0.0\"");
        }
    }
}