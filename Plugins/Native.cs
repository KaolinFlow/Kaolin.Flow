using System.Reflection;
using Kaolin.Flow.Builders;
using Kaolin.Flow.Core;
using Miniscript;

namespace Kaolin.Flow.Plugins
{
    public class Native(Engine engine) : Base(engine)
    {
        readonly public static string TypeString = "string";
        readonly public static string TypeInt = "int";
        readonly public static string TypeLong = "long";
        readonly public static string TypeDouble = "double";
        readonly public static string TypeFloat = "float";
        readonly public static string TypeList = "list";
        readonly public static string TypeMap = "map";
        readonly public static string TypeFunction = "function";
        readonly public static string TypeBool = "bool";
        readonly public static string TypeAuto = "auto";
        readonly public static string TypePtr = "ptr";
        readonly public static string TypeVoid = "void";
        readonly public static string TypeType = "type";
        readonly public static ValMap types = new MapBuilder()
           .AddProp("String", Utils.Cast(TypeString))
           .AddProp("Int", Utils.Cast(TypeInt))
           .AddProp("Long", Utils.Cast(TypeLong))
           .AddProp("Float", Utils.Cast(TypeFloat))
           .AddProp("Double", Utils.Cast(TypeDouble))
           .AddProp("List", Utils.Cast(TypeList))
           .AddProp("Map", Utils.Cast(TypeMap))
           .AddProp("Function", Utils.Cast(TypeFunction))
           .AddProp("Bool", Utils.Cast(TypeBool))
           .AddProp("Auto", Utils.Cast(TypeAuto))
           .AddProp("Pointer", Utils.Cast(TypePtr))
           .AddProp("Void", Utils.Cast(TypeVoid))
           .AddProp("Type", Utils.Cast(TypeType))
           .map;

        public object UnWrapValue(Value value, string type)
        {
            if (type == TypeString)
            {
                return Utils.UnWrapValue((ValString)value);
            }
            else if (type == TypeInt)
            {
                return (int)Utils.UnWrapValue((ValNumber)value);
            }
            else if (type == TypeLong)
            {
                return (long)Utils.UnWrapValue((ValNumber)value);
            }
            else if (type == TypeFloat)
            {
                return (float)Utils.UnWrapValue((ValNumber)value);
            }
            else if (type == TypeDouble)
            {
                return Utils.UnWrapValue((ValNumber)value);
            }
            else if (type == TypeList)
            {
                return Utils.UnWrapValue((ValList)value, engine);
            }
            else if (type == TypeMap)
            {
                return Utils.UnWrapValue((ValMap)value, engine);
            }
            else if (type == TypeFunction)
            {
                return Utils.UnWrapValue((ValFunction)value, engine);
            }
            else if (type == TypeBool)
            {
                return Utils.UnWrapValue((ValNumber)value) != 0;
            }
            else if (type == TypeAuto)
            {
                return Utils.UnWrapValue(value, engine);
            }
            else if (type == TypePtr)
            {
                return Utils.UnWrapValue((ValPtr)value);
            }
            else if (type == TypeVoid)
            {
                return ValNull.instance;
            }
            else if (type == TypeVoid)
            {
                return ((ValMap)value).userData;
            }
            else
            {
                throw new Exception("Unknown type " + type);
            }
        }

        public Value Cast(object value, ValMap typeMap)
        {
            typeMap.TryGetValue("type", out Value t);

            string type = ((ValString)t!).value;

            if (type == TypeString)
            {
                return Utils.Cast((string)value);
            }
            else if (type == TypeInt)
            {
                return Utils.Cast((int)value);
            }
            else if (type == TypeLong)
            {
                return Utils.Cast((long)value);
            }
            else if (type == TypeFloat)
            {
                return Utils.Cast((float)value);
            }
            else if (type == TypeDouble)
            {
                return Utils.Cast((double)value);
            }
            else if (type == TypeList)
            {
                return Utils.Cast((ListPointer)value);
            }
            else if (type == TypeMap)
            {
                return Utils.Cast((MapPointer)value);
            }
            else if (type == TypeFunction)
            {
                return Utils.Cast((FunctionPointer)value);
            }
            else if (type == TypeBool)
            {
                return Utils.Cast((bool)value);
            }
            else if (type == TypeAuto)
            {
                return Utils.Cast(value);
            }
            else if (type == TypePtr)
            {
                return Utils.Cast((Ptr)value);
            }
            else if (type == TypeVoid)
            {
                return ValNull.instance;
            }
            else if (type == TypeType)
            {
                typeMap.TryGetValue("definition", out Value m);

                return new MapBuilder(WrapType(value.GetType(), (ValMap)m!, null!)).SetUserData(value).map;
            }
            else
            {
                throw new Exception("Unknown type " + type);
            }
        }

        public static string ResolveType(Type type)
        {
            return "unimplemented";
        }

        public IntrinsicCode CreateCallbackInstance(Type type, ValMap symbolsDefinition, ValMap parentDefinition)
        {
            symbolsDefinition.TryGetValue("args", out Value _def);
            symbolsDefinition.TryGetValue("return", out Value _ret);

            var definitions = (ValList)_def;
            var returnType = (ValString)_ret;

            return (context, p) =>
            {
                List<object> args = [];

                for (int i = 0; i < definitions.values.Count; i++)
                {
                    args.Add(UnWrapValue(context.GetLocal("arg" + i), ((ValString)definitions.values[i]).value));
                }

                object o = Activator.CreateInstance(type, [.. args])!;

                return new Intrinsic.Result(new MapBuilder(WrapType(o.GetType(), parentDefinition, o)).SetUserData(o).map);
            };
        }

        public IntrinsicCode CreateCallback(MethodInfo method, ValMap symbolsDefinition, object instance)
        {
            symbolsDefinition.TryGetValue("args", out Value _def);
            symbolsDefinition.TryGetValue("return", out Value _ret);

            var definitions = (ValList)_def;
            var returnType = (ValMap)_ret;

            return (context, p) =>
            {
                List<object> args = [];

                for (int i = 0; i < definitions.values.Count; i++)
                {
                    args.Add(UnWrapValue(context.GetLocal("arg" + i), ((ValString)definitions.values[i]).value));
                }

                return new Intrinsic.Result(Cast(method.Invoke(instance, [.. args])!, returnType));
            };
        }

        public ValMap WrapType(Type type, ValMap symbolsDefinition, object instance)
        {
            MapBuilder symbolsBuilder = new();

            foreach (var entrymap in symbolsDefinition.map)
            {
                var key = ((ValString)entrymap.Key).value;
                var value = (ValMap)entrymap.Value;
                value.TryGetValue("args", out Value _args);
                var args = (ValList)_args;
                IntrinsicCode callback;
                FunctionBuilder functionBuilder = new();

                if (key != type.Name)
                {
                    MethodInfo? method = null;
                    foreach (var _method in type.GetMethods())
                    {
                        if (_method.Name == key)
                        {
                            method = _method;

                            break;
                        }
                    }

                    if (method == null) throw new Exception("Cannot found method " + type.Name + "." + key);
                    if (method.IsStatic && instance != null) continue;
                    if (!method.IsStatic && instance == null) continue;

                    callback = CreateCallback(method, value, instance!);
                }
                else
                {
                    callback = CreateCallbackInstance(type, value, symbolsDefinition);
                }


                for (int i = 0; i < args.values.Count; i++)
                {
                    functionBuilder.AddParam("arg" + i);
                }

                functionBuilder.SetCallback(callback);
                symbolsBuilder.AddProp(key, functionBuilder.Function);
            }

            return symbolsBuilder.map;
        }

        public override void Inject()
        {
            ValMap NativeDLL = new();
            ValMap map = new MapBuilder()
                .AddProp("Type", types)
                .AddProp("NativeDLL", NativeDLL)
                .AddProp("retDef",
                    new FunctionBuilder()
                        .AddParam("type")
                        .AddParam("definition", ValNull.instance)
                        .SetCallback((context, p) =>
                            new Intrinsic.Result(
                                new MapBuilder()
                                    .AddProp("type", context.GetLocal("type"))
                                    .AddProp("definition", context.GetLocal("definition"))
                                .map
                            )
                        )
                    .Function
                )
                .AddProp("import",
                    new FunctionBuilder()
                        .AddParam("path")
                        .AddParam("symbols")
                        .SetCallback((context, p) =>
                        {
                            Assembly assembly = Assembly.LoadFrom(engine.UnWrapFilePath(context.GetLocalString("path")));
                            ValMap symbolsDefinition = (ValMap)context.GetLocal("symbols");
                            MapBuilder resultBuilder = new();

                            Type[] types = assembly.GetTypes();

                            foreach (var entry in symbolsDefinition.map)
                            {
                                Type type = null!;
                                string name = ((ValString)entry.Key).value;

                                foreach (var t in types)
                                {
                                    if (t.Name == name) type = t;
                                }

                                if (type == null)
                                {
                                    throw new Exception("Cannot found type " + name + " in assembly");
                                }

                                symbolsDefinition.TryGetValue(name, out Value _m);

                                ValMap map = (ValMap)_m;

                                resultBuilder.AddProp(entry.Key, WrapType(type, map, null!));
                            }

                            return new Intrinsic.Result(
                                new MapBuilder(Engine.New(NativeDLL))
                                    .AddProp("symbols", resultBuilder.map)
                                    .map
                            );
                        })
                    .Function
                )
                .map;

            Register("native", map);
        }
    }
}
