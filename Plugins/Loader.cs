using System.Reflection;
using Kaolin.Flow.Builders;
using Kaolin.Flow.Core;
using Miniscript;

namespace Kaolin.Flow.Plugins
{

    public class Loader(Engine engine) : Base(engine)
    {

        public override void Inject()
        {
            ValMap map = new MapBuilder()
                .AddProp("load",
                    new FunctionBuilder("load")
                        .AddParam("path")
                        .AddParam("types")
                        .SetCallback((context, p) =>
                        {
                            Assembly assembly = Assembly.LoadFrom(Utils.UnWrapPath(((ValString)context.parent.GetVar("path")).value, context.GetLocalString("path")));
                            var types = assembly.GetTypes();

                            foreach (var val in ((ValList)context.GetLocal("types")).values)
                            {
                                Type? type = null;

                                foreach (var t in types)
                                {
                                    if (t.Name == ((ValString)val).value)
                                    {
                                        type = t;

                                        break;
                                    }
                                }

                                if (type == null) throw new Exception("Cannot found type " + val);

                                _ = (Base)Activator.CreateInstance(type, [engine])!;
                            }

                            return new Intrinsic.Result(ValNull.instance);
                        })
                        .Function
                )
                .map;

            Register("plugin", map);
        }
    }
}