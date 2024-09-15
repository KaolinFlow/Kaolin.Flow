using Kaolin.Flow.Builders;
using Kaolin.Flow.Core;
using Miniscript;

namespace Kaolin.Flow.Plugins
{

    public class Dev(Engine engine) : Base(engine)
    {
        readonly Dictionary<string, Value> evalMemo = [];
        public override void Inject()
        {
            ValMap map = new MapBuilder()
                .AddProp("bytesToString",
                    new FunctionBuilder()
                        .AddParam("bytes")
                        .SetCallback((context, p) =>
                        {
                            return new Intrinsic.Result(System.Text.Encoding.UTF8.GetString(Http.UnWrapData((ValList)context.GetLocal("bytes"))));
                        })
                        .Function
                )
                .AddProp("eval",
                    new FunctionBuilder()
                        .AddParam("code")
                        .SetCallback((context, p) =>
                        {
                            string s = Guid.NewGuid().ToString();

                            if (p != null)
                            {
                                string key = ((ValString)p.result).value;
                                bool ok = evalMemo.TryGetValue(key, out Value val);

                                if (!ok) return new Intrinsic.Result(p.result, false);

                                evalMemo.Remove(key);

                                return new Intrinsic.Result(val!, true);
                            }

                            engine.EvalValue(context.GetLocalString("code"))
                                .ContinueWith((t) =>
                                {
                                    evalMemo.Add(s, t.Result);
                                });

                            return new Intrinsic.Result(new ValString(s), false);
                        })
                        .Function
                )
                .map;

            Register("dev", map);
        }
    }
}