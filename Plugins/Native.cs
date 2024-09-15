using System.Reflection;
using System.Runtime.InteropServices;
using Kaolin.Flow.Builders;
using Kaolin.Flow.Core;
using Miniscript;

namespace Kaolin.Flow.Plugins
{

    public class Native(Engine engine) : Base(engine)
    {

        public override void Inject()
        {
            ValMap NativeDLL = new();
            ValMap map = new MapBuilder()
                .AddProp("NativeDLL", NativeDLL)
                .AddProp("import",
                    new FunctionBuilder()
                        .AddParam("path")
                        .AddParam("symbols")
                        .SetCallback((context, p) =>
                        {
                            Assembly assembly = Assembly.LoadFrom(engine.UnWrapFilePath(context.GetLocalString("path")));
                            ValMap symbolsDefinition = (ValMap)context.GetLocal("symbols");
                            MapBuilder symbolsBuilder = new();

                            foreach (var entry in symbolsDefinition.map)
                            {
                                MapBuilder mapBuilder = new();
                                var typeName = ((ValString)entry.Key).value;
                                var typeValue = assembly.GetType(typeName)!;
                                var methods = typeValue.GetMethods();

                                foreach (var entrymap in ((ValMap)entry.Value).map)
                                {
                                    MethodInfo? method = null;
                                    var key = ((ValString)entrymap.Key).value;
                                    var value = (ValMap)entrymap.Value;
                                    value.TryGetValue("argsLength", out Value _argsLength);
                                    var length = ((ValNumber)_argsLength).value;
                                    FunctionBuilder functionBuilder = new();

                                    foreach (var _method in methods)
                                    {
                                        if (_method.Name == key)
                                        {
                                            method = _method;

                                            break;
                                        }
                                    }

                                    if (method == null) throw new Exception("Cannot found method " + typeName + "." + key);

                                    for (int i = 0; i < length; i++)
                                    {
                                        functionBuilder.AddParam("arg" + i);
                                    }

                                    functionBuilder.SetCallback((context, p) =>
                                    {
                                        List<object> args = [];

                                        for (int i = 0; i < length; i++)
                                        {
                                            args.Add(Utils.UnWrapValue(context.GetLocal("arg" + i)));
                                        }

                                        return new Intrinsic.Result(Utils.Cast(method.Invoke(null, [.. args])!));
                                    });

                                    mapBuilder.AddProp(key, functionBuilder.Function);
                                }

                                symbolsBuilder.AddProp(typeName, mapBuilder.map);
                            }

                            return new Intrinsic.Result(
                                new MapBuilder(Engine.New(NativeDLL))
                                    .AddProp("symbols", symbolsBuilder.map)
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
