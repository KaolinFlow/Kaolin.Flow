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
                        .AddParam("bytes")
                        .SetCallback((context, p) =>
                        {
                            return new Intrinsic.Result(System.Text.Encoding.UTF8.GetString(Http.UnWrapData((ValList)context.GetLocal("bytes"))));
                        })
                        .Function
                )
                .AddProp("eval",
                    new FunctionBuilder("eval")
                        .AddParam("code")
                        .SetCallback((context, p) =>
                        {
                            if (p != null)
                            {
                                if (!context.variables.map.ContainsKey(new ValString("value"))) return p;
                                Value val = context.GetVar("value", ValVar.LocalOnlyMode.Strict);

                                return new Intrinsic.Result(val!, true);
                            }

                            engine.EvalValue(context.GetLocalString("code"))
                                .ContinueWith((t) =>
                                {
                                    context.SetVar("value", t.Result);
                                });

                            return new Intrinsic.Result(ValNull.instance, false);
                        })
                        .Function
                )
                .map;

            Register("dev", map);
        }
    }
}