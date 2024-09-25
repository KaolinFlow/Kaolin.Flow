using System.Collections;
using System.Diagnostics;
using System.Text;
using Kaolin.Flow.Builders;
using Kaolin.Flow.Core;
using Microsoft.VisualBasic;
using Miniscript;
using System.IO;

namespace Kaolin.Flow.Plugins
{
    public class Machine(Engine engine) : Base(engine)
    {
        public static (FileMode, FileAccess) ConvertMode(string mode)
        {
            FileMode fileMode;
            FileAccess fileAccess;

            switch (mode)
            {
                case "r":
                    fileMode = FileMode.Open;
                    fileAccess = FileAccess.Read;
                    break;

                case "w":
                    fileMode = FileMode.Create;
                    fileAccess = FileAccess.Write;
                    break;

                case "rw":
                    fileMode = FileMode.OpenOrCreate;
                    fileAccess = FileAccess.ReadWrite;
                    break;

                case "r+":
                    fileMode = FileMode.Open;
                    fileAccess = FileAccess.ReadWrite;
                    break;

                case "w+":
                    fileMode = FileMode.Create;
                    fileAccess = FileAccess.ReadWrite;
                    break;

                case "rw+":
                    fileMode = FileMode.OpenOrCreate;
                    fileAccess = FileAccess.ReadWrite;
                    break;

                default:
                    throw new ArgumentException($"Unsupported mode: {mode}");
            }

            return (fileMode, fileAccess);
        }

        public static bool IsDirectory(string path)
        {
            FileAttributes attr = File.GetAttributes(path);
            bool isDirectory = (attr & FileAttributes.Directory) == FileAttributes.Directory;

            return isDirectory;
        }

        readonly ValMap FileHandle = new MapBuilder()

            .map;

        public static void CopyDir(string sourceDirectory, string targetDirectory)
        {
            DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
            DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);

            CopyDirAll(diSource, diTarget);
        }

        public static void CopyDirAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            foreach (FileInfo fi in source.GetFiles())
            {
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyDirAll(diSourceSubDir, nextTargetSubDir);
            }
        }

        public static string UnWrapPath(string s)
        {
            if (Utils.HasProtocol(s)) return s[(new Uri(s).Scheme + "://").Length..];

            return Utils.ResolvePath(new Uri(Directory.GetCurrentDirectory()).AbsolutePath, s).AbsolutePath;
        }


        public static string WrapPath(string s)
        {
            return Utils.ResolvePath(new Uri(Directory.GetCurrentDirectory()).AbsolutePath, s).AbsoluteUri;
        }

        public override void Inject()
        {
            if (!IsDirectory(UnWrapPath(engine.path))) Directory.SetCurrentDirectory(Directory.GetParent(UnWrapPath(engine.path))!.Name);

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

            ValMap map = new MapBuilder()
                .AddProp("env", env)
                .AddProp("shellArgs", shellArgs)
                .AddProp("FileHandle", FileHandle)
                .AddProp("file",
                    new MapBuilder()
                        .AddProp("curdir",
                            new FunctionBuilder("curdir")
                                .SetCallback((context, p) =>
                                {
                                    return new Intrinsic.Result(WrapPath(Directory.GetCurrentDirectory()));
                                })
                                .Function
                        )
                        .AddProp("setdir",
                            new FunctionBuilder("setdir")
                                .AddParam("path", new ValString(""))
                                .SetCallback((context, p) =>
                                {
                                    string path = UnWrapPath(context.GetLocalString("path"));

                                    if (!Directory.Exists(path)) return Intrinsic.Result.False;

                                    Directory.SetCurrentDirectory(path);

                                    return Intrinsic.Result.True;
                                })
                                .Function
                        )
                        .AddProp("makedir",
                            new FunctionBuilder("makedir")
                                .AddParam("path", new ValString(""))
                                .SetCallback((context, p) =>
                                {
                                    Directory.CreateDirectory(UnWrapPath(context.GetLocalString("path")));

                                    return Intrinsic.Result.Null;
                                })
                                .Function
                        )
                        .AddProp("children",
                            new FunctionBuilder("children")
                                .AddParam("path", ValNull.instance)
                                .SetCallback((context, p) =>
                                {
                                    string path;

                                    if (context.GetLocal("path") == ValNull.instance)
                                    {
                                        path = Directory.GetCurrentDirectory();
                                    }
                                    else
                                    {
                                        path = context.GetLocalString("path");
                                    }

                                    path = UnWrapPath(path);

                                    ValList list = new();

                                    foreach (var entry in Directory.EnumerateFileSystemEntries(path))
                                    {
                                        list.values.Add(Utils.Cast(WrapPath(entry!)));
                                    }

                                    return new Intrinsic.Result(list);
                                })
                                .Function
                        )
                        .AddProp("name",
                            new FunctionBuilder("name")
                                .AddParam("path", new ValString(""))
                                .SetCallback((context, p) =>
                                {
                                    return new Intrinsic.Result(Path.GetFileName(UnWrapPath(context.GetLocalString("path"))));
                                })
                                .Function
                        )
                        .AddProp("parent",
                            new FunctionBuilder("parent")
                                .AddParam("path", new ValString(""))
                                .SetCallback((context, p) =>
                                {
                                    return new Intrinsic.Result(Directory.GetParent(UnWrapPath(context.GetLocalString("path")))!.Name);
                                })
                                .Function
                        )
                        .AddProp("exists",
                            new FunctionBuilder("exists")
                                .AddParam("path", new ValString(""))
                                .SetCallback((context, p) =>
                                {
                                    var path = UnWrapPath(context.GetLocalString("path"));
                                    try
                                    {

                                        var isDir = IsDirectory(path);

                                        return new Intrinsic.Result(Utils.Cast(isDir ? Directory.Exists(path) : File.Exists(path)));
                                    }
                                    catch
                                    {
                                        return new Intrinsic.Result(Utils.Cast(false));
                                    }
                                })
                                .Function
                        )
                        .AddProp("readLines",
                            new FunctionBuilder("readLines")
                                .AddParam("path", new ValString(""))
                                .SetCallback((context, p) =>
                                {
                                    string path = UnWrapPath(context.GetLocalString("path"));
                                    ValList lines = new();

                                    foreach (var line in File.ReadLines(path))
                                    {
                                        lines.values.Add(Utils.Cast(line));
                                    }

                                    return new Intrinsic.Result(lines);
                                })
                                .Function
                        )
                        .AddProp("writeLines",
                            new FunctionBuilder("writeLines")
                                .AddParam("path", new ValString(""))
                                .AddParam("lines", new ValList())
                                .SetCallback((context, p) =>
                                {
                                    string path = UnWrapPath(context.GetLocalString("path"));
                                    ValList lines = (ValList)context.GetLocal("lines");
                                    List<string> contents = [];

                                    foreach (var line in lines.values)
                                    {
                                        contents.Add(((ValString)line).value);
                                    }

                                    File.WriteAllLines(path, [.. contents]);

                                    return new Intrinsic.Result(lines);
                                })
                                .Function
                        )
                        .AddProp("delete",
                            new FunctionBuilder("delete")
                                .AddParam("path", new ValString(""))
                                .AddParam("isRecursive", Utils.Cast(false))
                                .SetCallback((context, p) =>
                                {
                                    string path = UnWrapPath(context.GetLocalString("path"));
                                    bool isDirectory = IsDirectory(path);

                                    if (isDirectory) Directory.Delete(path, context.GetLocalBool("isRecursive"));
                                    else File.Delete(path);
                                    return Intrinsic.Result.True;
                                })
                                .Function
                        )
                        .AddProp("child",
                            new FunctionBuilder("child")
                                .AddParam("basePath", new ValString(""))
                                .AddParam("subPath", new ValString(""))
                                .SetCallback((context, p) =>
                                {
                                    return new Intrinsic.Result(Utils.ResolvePath(context.GetLocalString("basePath"), context.GetLocalString("subPath")).AbsoluteUri);
                                })
                                .Function
                        )
                        .AddProp("move",
                            new FunctionBuilder("move")
                                .AddParam("oldPath", new ValString(""))
                                .AddParam("newPath", new ValString(""))
                                .SetCallback((context, p) =>
                                {
                                    string oldPath = UnWrapPath(context.GetLocalString("oldPath"));
                                    string newPath = UnWrapPath(context.GetLocalString("newPath"));

                                    bool isDirectory = IsDirectory(oldPath);

                                    if (isDirectory) Directory.Move(oldPath, newPath);
                                    else File.Move(oldPath, newPath);


                                    return Intrinsic.Result.Null;
                                })
                                .Function
                        )
                        .AddProp("copy",
                            new FunctionBuilder("copy")
                                .AddParam("sourceFilePath", new ValString(""))
                                .AddParam("targetFilePath", new ValString(""))
                                .SetCallback((context, p) =>
                                {
                                    string sourceFilePath = UnWrapPath(context.GetLocalString("sourceFilePath"));
                                    string targetFilePath = UnWrapPath(context.GetLocalString("targetFilePath"));

                                    bool isDirectory = IsDirectory(sourceFilePath);

                                    if (isDirectory) CopyDir(sourceFilePath, targetFilePath);
                                    else File.Copy(sourceFilePath, targetFilePath);


                                    return Intrinsic.Result.Null;
                                })
                                .Function
                        )
                        .AddProp("info",
                            new FunctionBuilder("info")
                                .AddParam("path", new ValString(""))
                                .SetCallback((context, p) =>
                                {
                                    string path = UnWrapPath(context.GetLocalString("path"));
                                    string date;
                                    bool isDirectory = IsDirectory(path);
                                    long size;

                                    if (isDirectory)
                                    {
                                        size = 0;
                                        date = new DirectoryInfo(path).LastWriteTime.ToString("dd/MM/yy HH:mm:ss");
                                    }
                                    else
                                    {
                                        size = new FileInfo(path).Length;
                                        date = File.GetLastWriteTime(path).ToString("dd/MM/yy HH:mm:ss");
                                    }

                                    return new Intrinsic.Result(
                                        new MapBuilder()
                                            .AddProp("path", Utils.Cast(WrapPath(path)))
                                            .AddProp("isDirectory", Utils.Cast(isDirectory))
                                            .AddProp("size", Utils.Cast(size))
                                            .AddProp("date", Utils.Cast(date))
                                            .AddProp("comment", Utils.Cast(""))
                                            .map
                                    );
                                })
                                .Function
                        )
                        .AddProp("open",
                            new FunctionBuilder("open")
                                .AddParam("path", new ValString(""))
                                .AddParam("mode", "rw+")
                                .SetCallback((context, p) =>
                                {
                                    string path = UnWrapPath(context.GetLocalString("path"));
                                    var r = ConvertMode(context.GetLocalString("mode"));
                                    FileStream fileStream = File.Open(path, r.Item1);
                                    var streamReader = new StreamReader(fileStream, Encoding.UTF8, true);

                                    bool isOpen = true;

                                    return new Intrinsic.Result(
                                        new MapBuilder(Engine.New(FileHandle))
                                            .SetUserData(fileStream)
                                            .AddProp("isOpen",
                                                new FunctionBuilder("isOpen")
                                                    .SetCallback((context, p) =>
                                                    {
                                                        return new Intrinsic.Result(Utils.Cast(isOpen));
                                                    })
                                                    .Function
                                            )
                                            .AddProp("close",
                                                new FunctionBuilder("close")
                                                    .SetCallback((context, p) =>
                                                    {
                                                        isOpen = false;

                                                        fileStream.Close();

                                                        return Intrinsic.Result.Null;
                                                    })
                                                    .Function
                                            )
                                            .AddProp("position",
                                                new FunctionBuilder("position")
                                                    .SetCallback((context, p) =>
                                                    {
                                                        return new Intrinsic.Result(Utils.Cast(fileStream.Position));
                                                    })
                                                    .Function
                                            )
                                            .AddProp("atEnd",
                                                new FunctionBuilder("atEnd")
                                                    .SetCallback((context, p) =>
                                                    {
                                                        return new Intrinsic.Result(Utils.Cast(fileStream.Position == fileStream.Length));
                                                    })
                                                    .Function
                                            )
                                            .AddProp("writeLine",
                                                new FunctionBuilder("writeLine")
                                                    .AddParam("content", new ValString(""))
                                                    .SetCallback((context, p) =>
                                                    {
                                                        fileStream.Write(Encoding.ASCII.GetBytes(context.GetLocalString("content") + "\n"));

                                                        return Intrinsic.Result.Null;
                                                    })
                                                    .Function
                                            )
                                            .AddProp("readLine",
                                                new FunctionBuilder("read")
                                                    .SetCallback((context, p) =>
                                                    {

                                                        return new Intrinsic.Result(Utils.Cast(streamReader.ReadLine()!));
                                                    })
                                                    .Function
                                            )
                                            .AddProp("write",
                                                new FunctionBuilder("write")
                                                    .AddParam("content", new ValString(""))
                                                    .SetCallback((context, p) =>
                                                    {
                                                        fileStream.Write(Encoding.ASCII.GetBytes(context.GetLocalString("content")));

                                                        return Intrinsic.Result.Null;
                                                    })
                                                    .Function
                                            )
                                            .AddProp("read",
                                                new FunctionBuilder("read")
                                                    .SetCallback((context, p) =>
                                                    {

                                                        return new Intrinsic.Result(Utils.Cast(streamReader.ReadToEnd()));
                                                    })
                                                    .Function
                                            )
                                            .map
                                    );
                                })
                                .Function
                        )
                        .map
                )
                .AddProp("exit",
                    new FunctionBuilder("exit")
                        .AddParam("resultCode", new ValNumber(0))
                        .SetCallback((context, p) =>
                        {
                            Environment.Exit(context.GetLocalInt("resultCode"));

                            return Intrinsic.Result.Null;
                        })
                        .Function
                )
                .AddProp("getOuter",
                    new FunctionBuilder("getOuter")
                        .AddParam("callback",
                            new FunctionBuilder()
                                .SetCallback((_, _) =>
                                {
                                    return Intrinsic.Result.Null;
                                })
                                .Function
                        )
                        .SetCallback((context, p) =>
                        {
                            ValFunction f = (ValFunction)context.GetLocal("callback");

                            return new Intrinsic.Result(f.outerVars);
                        })
                        .Function
                )
                .AddProp("bindOuter",
                    new FunctionBuilder("bindOuter")
                        .AddParam("callback",
                            new FunctionBuilder()
                                .SetCallback((_, _) =>
                                {
                                    return Intrinsic.Result.Null;
                                })
                                .Function
                        )
                        .AddParam("outer", new ValMap())
                        .SetCallback((context, p) =>
                        {
                            ValFunction f = (ValFunction)context.GetLocal("callback");

                            return new Intrinsic.Result(new ValBFunction(f.function, (ValMap)context.GetLocal("outer")));
                        })
                        .Function
                )
                .AddProp("setOuter",
                    new FunctionBuilder("setOuter")
                        .AddParam("callback",
                            new FunctionBuilder()
                                .SetCallback((_, _) =>
                                {
                                    return Intrinsic.Result.Null;
                                })
                                .Function
                        )
                        .AddParam("outer", new ValMap())
                        .SetCallback((context, p) =>
                        {
                            ValFunction f = (ValFunction)context.GetLocal("callback");

                            return new Intrinsic.Result(f.BindAndCopy((ValMap)context.GetLocal("outer")));
                        })
                        .Function
                )
                .AddProp("input",
                    new FunctionBuilder("input")
                        .AddParam("prompt", new ValString(""))
                        .SetCallback((context, p) =>
                        {
                            string s = context.GetLocalString("prompt");
                            if (s.Length != 0) Engine.Print(s, false);
                            string v = Console.ReadLine()!;

                            return new Intrinsic.Result(v);
                        })
                        .Function
                )
                .AddProp("exec",
                    new FunctionBuilder("exec")
                        .AddParam("file", new ValString(""))
                        .AddParam("timeout", new ValNumber(30))
                        .SetCallback((context, p) =>
                        {
                            string s = context.GetLocalString("file");
                            int t = context.GetLocalInt("timeout");

                            if (s.Length == 0) return new Intrinsic.Result(
                                new MapBuilder()
                                    .AddProp("errors", Utils.Cast(""))
                                    .AddProp("status", Utils.Cast(0))
                                    .AddProp("output", Utils.Cast(""))
                                    .map
                            );

                            List<string> args = [.. s.Split(" ")];
                            string name = args[0];

                            args.RemoveAt(0);
                            Process process = new()
                            {
                                EnableRaisingEvents = false
                            };
                            process.StartInfo.FileName = name;
                            process.StartInfo.Arguments = Strings.Join([.. args]);

                            process.StartInfo.RedirectStandardError = true;
                            process.StartInfo.RedirectStandardOutput = true;
                            process.StartInfo.UseShellExecute = false;
                            process.Start();

                            Task.Delay(t * 1000).ContinueWith((t) =>
                            {
                                process.Close();
                            });

                            process.WaitForExit();

                            return new Intrinsic.Result(
                                new MapBuilder()
                                    .AddProp("errors", Utils.Cast(process.StandardError.ReadToEnd()))
                                    .AddProp("status", Utils.Cast(process.ExitCode))
                                    .AddProp("output", Utils.Cast(process.StandardOutput.ReadToEnd()))
                                    .map
                            );
                        })
                        .Function
                )
                .map;

            Register("machine", map);
        }
    }
}