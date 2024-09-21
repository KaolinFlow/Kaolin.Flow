using Kaolin.Flow.Builders;
using Kaolin.Flow.Core;
using Miniscript;

namespace Kaolin.Flow.Plugins
{
    public class Dev(Engine engine) : Base(engine)
    {
        public override void Inject()
        {
            ValMap map = new MapBuilder()
                .AddProp("bytesToString",
                    new FunctionBuilder("bytesToString")
                        .AddParam("bytes", new ValList())
                        .SetCallback((context, p) =>
                        {
                            return new Intrinsic.Result(System.Text.Encoding.UTF8.GetString(Http.UnWrapData((ValList)context.GetLocal("bytes"))));
                        })
                        .Function
                )
                .AddProp("eval",
                    new FunctionBuilder("eval")
                        .AddParam("code", new ValString(""))
                        .SetCallback((context, p) =>
                        {
                            ValMap mv = (ValMap)context.GetVar("importMeta");
                            mv.TryGetValue("path", out Value v);

                            return new Intrinsic.Result(engine.EvalValue(context.GetLocalString("code"), ((ValString)v).value + ":" + context.parent.GetSourceLoc().ToString()));
                        })
                        .Function
                )
                .map;

            Register("dev", map);
        }
    }
}