using System.Collections;
using System.Diagnostics;
using System.Text;
using Kaolin.Flow.Builders;
using Kaolin.Flow.Core;
using Miniscript;

namespace Kaolin.Flow.Plugins
{
    public class Machine(Engine engine) : Base(engine)
    {

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
                                    return new Intrinsic.Result(engine.path);
                                })
                                .Function
                        )
                        .AddProp("setdir",
                            new FunctionBuilder("setdir")
                                .AddParam("path")
                                .SetCallback((context, p) =>
                                {
                                    string path = engine.UnWrapFilePath(context.GetLocalString("path"));

                                    if (!Directory.Exists(path)) return Intrinsic.Result.False;

                                    engine.path = path;

                                    return Intrinsic.Result.True;
                                })
                                .Function
                        )
                        .AddProp("makedir",
                            new FunctionBuilder("makedir")
                                .AddParam("path")
                                .SetCallback((context, p) =>
                                {
                                    Directory.CreateDirectory(engine.UnWrapFilePath(context.GetLocalString("path")));

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
                                        path = engine.path;
                                    }
                                    else
                                    {
                                        path = context.GetLocalString("path");
                                    }

                                    path = engine.UnWrapFilePath(path);

                                    ValList list = new();

                                    foreach (var entry in Directory.EnumerateFileSystemEntries(path))
                                    {
                                        list.values.Add(Utils.Cast(entry!));
                                    }

                                    return new Intrinsic.Result(list);
                                })
                                .Function
                        )
                        .AddProp("name",
                            new FunctionBuilder("name")
                                .AddParam("path")
                                .SetCallback((context, p) =>
                                {
                                    return new Intrinsic.Result(Path.GetFileName(engine.UnWrapFilePath(context.GetLocalString("path"))));
                                })
                                .Function
                        )
                        .AddProp("parent",
                            new FunctionBuilder("parent")
                                .AddParam("path")
                                .SetCallback((context, p) =>
                                {
                                    return new Intrinsic.Result(Directory.GetParent(engine.UnWrapFilePath(context.GetLocalString("path")))!.Name);
                                })
                                .Function
                        )
                        .AddProp("exists",
                            new FunctionBuilder("exists")
                                .AddParam("path")
                                .SetCallback((context, p) =>
                                {
                                    return new Intrinsic.Result(Utils.Cast(File.Exists(engine.UnWrapFilePath(context.GetLocalString("path")))));
                                })
                                .Function
                        )
                        .AddProp("readLines",
                            new FunctionBuilder("readLines")
                                .AddParam("path")
                                .SetCallback((context, p) =>
                                {
                                    string path = engine.UnWrapFilePath(context.GetLocalString("path"));
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
                                .AddParam("path")
                                .AddParam("lines")
                                .SetCallback((context, p) =>
                                {
                                    string path = engine.UnWrapFilePath(context.GetLocalString("path"));
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
                                .AddParam("path")
                                .AddParam("isRecursive", Utils.Cast(false))
                                .SetCallback((context, p) =>
                                {
                                    string path = engine.UnWrapFilePath(context.GetLocalString("path"));
                                    bool isDirectory = IsDirectory(path);

                                    if (isDirectory) Directory.Delete(path, context.GetLocalBool("isRecursive"));
                                    else File.Delete(path);
                                    return Intrinsic.Result.True;
                                })
                                .Function
                        )
                        .AddProp("child",
                            new FunctionBuilder("child")
                                .AddParam("basePath")
                                .AddParam("subPath")
                                .SetCallback((context, p) =>
                                {
                                    return new Intrinsic.Result(Engine.ResolvePath(context.GetLocalString("basePath"), context.GetLocalString("subPath")).AbsoluteUri);
                                })
                                .Function
                        )
                        .AddProp("move",
                            new FunctionBuilder("move")
                                .AddParam("oldPath")
                                .AddParam("newPath")
                                .SetCallback((context, p) =>
                                {
                                    string oldPath = engine.UnWrapFilePath(context.GetLocalString("oldPath"));
                                    string newPath = engine.UnWrapFilePath(context.GetLocalString("newPath"));

                                    bool isDirectory = IsDirectory(oldPath);

                                    if (isDirectory) Directory.Move(oldPath, newPath);
                                    else File.Move(oldPath, newPath);


                                    return Intrinsic.Result.Null;
                                })
                                .Function
                        )
                        .AddProp("copy",
                            new FunctionBuilder("copy")
                                .AddParam("sourceFilePath")
                                .AddParam("targetFilePath")
                                .SetCallback((context, p) =>
                                {
                                    string sourceFilePath = engine.UnWrapFilePath(context.GetLocalString("sourceFilePath"));
                                    string targetFilePath = engine.UnWrapFilePath(context.GetLocalString("targetFilePath"));

                                    bool isDirectory = IsDirectory(sourceFilePath);

                                    if (isDirectory) CopyDir(sourceFilePath, targetFilePath);
                                    else File.Copy(sourceFilePath, targetFilePath);


                                    return Intrinsic.Result.Null;
                                })
                                .Function
                        )
                        .AddProp("info",
                            new FunctionBuilder("info")
                                .AddParam("path")
                                .SetCallback((context, p) =>
                                {
                                    string path = engine.UnWrapFilePath(context.GetLocalString("path"));
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
                                            .AddProp("path", Utils.Cast(Engine.WrapFilePath(path)))
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
                                .AddParam("path")
                                .AddParam("mode", "rw+")
                                .SetCallback((context, p) =>
                                {
                                    string path = engine.UnWrapFilePath(context.GetLocalString("path"));
                                    _ = Enum.TryParse(context.GetLocalString("mode"), out FileMode mode);
                                    FileStream fileStream = File.Open(path, mode);

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
                                                        return new Intrinsic.Result(Utils.Cast(fileStream.Position == fileStream.Length - 1));
                                                    })
                                                    .Function
                                            )
                                            .AddProp("write",
                                                new FunctionBuilder("write")
                                                    .AddParam("content")
                                                    .SetCallback((context, p) =>
                                                    {
                                                        Value val = context.GetLocal("content");

                                                        if (val.GetType() == typeof(ValString)) fileStream.Write(Encoding.ASCII.GetBytes(((ValString)val).value));
                                                        else fileStream.Write(Http.UnWrapData((ValList)val));

                                                        return Intrinsic.Result.Null;
                                                    })
                                                    .Function
                                            )
                                            .AddProp("writeLine",
                                                new FunctionBuilder("writeLine")
                                                    .AddParam("content")
                                                    .SetCallback((context, p) =>
                                                    {
                                                        fileStream.Write(Encoding.ASCII.GetBytes(context.GetLocalString("content") + "\n"));

                                                        return Intrinsic.Result.Null;
                                                    })
                                                    .Function
                                            )
                                            .AddProp("read",
                                                new FunctionBuilder("read")
                                                    .AddParam("content")
                                                    .AddParam("isUTF8", new ValNumber(1))
                                                    .SetCallback((context, p) =>
                                                    {
                                                        byte[] bytes = Enumerable.Repeat((byte)0x0, (int)(fileStream.Length - fileStream.Position)).ToArray();
                                                        fileStream.Read(bytes, (int)fileStream.Position, (int)(fileStream.Length - fileStream.Position));

                                                        if (context.GetLocalBool("isUTF8"))
                                                        {
                                                            return new Intrinsic.Result(Encoding.ASCII.GetString(bytes));
                                                        }

                                                        return new Intrinsic.Result(Http.WrapData(bytes));
                                                    })
                                                    .Function
                                            )
                                            .AddProp("readLine",
                                                new FunctionBuilder("read")
                                                    .AddParam("content")
                                                    .AddParam("isUTF8", new ValNumber(1))
                                                    .SetCallback((context, p) =>
                                                    {
                                                        List<byte> list = [];
                                                        byte[] newLine = Encoding.UTF8.GetBytes(Environment.NewLine.ToArray());
                                                        byte newLineMarker = newLine[^1];

                                                        while (fileStream.Position != fileStream.Length - 1)
                                                        {
                                                            int one = fileStream.ReadByte();

                                                            list.Add((byte)one);

                                                            if (one == newLineMarker && Utils.EndsWith([.. list], newLine))
                                                            {
                                                                list = list[..(list.Count - newLine.Length)];

                                                                break;
                                                            }
                                                        }

                                                        if (context.GetLocalBool("isUTF8"))
                                                        {
                                                            return new Intrinsic.Result(Encoding.ASCII.GetString(list.ToArray()));
                                                        }

                                                        return new Intrinsic.Result(Http.WrapData([.. list]));
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
                        })
                        .Function
                )
                .map;

            Register("machine", map);
        }
    }
}