using Kaolin.Flow.Builders;
using Kaolin.Flow.Core;
using Miniscript;

namespace Kaolin.Flow.Plugins
{
    public class Error(Engine engine) : Base(engine)
    {
        public override void Inject()
        {
            ValMap ErrorClass = new();
            ValMap map = new MapBuilder()
                .AddProp("Error", ErrorClass)
                .AddProp("throw",
                    new FunctionBuilder("throw")
                        .AddParam("error", new ValString(""))
                        .SetCallback((context, p) =>
                        {
                            string err = context.GetLocalString("error");
                            RuntimeException exc = new(err);
                            exc.location = context.GetSourceLoc() ?? context.parent.GetSourceLoc();
                            engine.interpreter.errorOutput.Invoke(exc.Description(), true);

                            return Intrinsic.Result.Null;
                        })
                        .Function
                )
                .AddProp("try",
                    new FunctionBuilder("try")
                        .AddParam("callback",
                            new FunctionBuilder()
                                .SetCallback((_, _) =>
                                {
                                    return Intrinsic.Result.Null;
                                })
                                .Function
                        )
                        .AddParam("args", new ValList())
                        .SetCallback((context, p) =>
                        {
                            var cb = (ValFunction)context.GetLocal("callback");
                            ErrorHandler.Callback errCb = null!;
                            string? err = null;

                            errCb = (error) =>
                            {
                                engine.errorHandler.Off(errCb);
                                err = error;

                                return true;
                            };

                            engine.errorHandler.On(errCb);
                            Value? val = null;
                            ValList args = (ValList)context.GetLocal("args");
                            bool isDone = false;

                            engine.interpreter.vm.ManuallyPushCall(new FunctionBuilder().SetCallback((context, partialResult) =>
                            {
                                if (err != null) return Intrinsic.Result.Null;
                                if (partialResult != null)
                                {
                                    Value value = context.GetTemp(0);
                                    val = value;
                                    isDone = true;

                                    return new Intrinsic.Result(value);
                                }

                                engine.interpreter.vm.ManuallyPushCall(cb, new ValTemp(0), args.values);

                                return new Intrinsic.Result(ValNull.instance, false);
                            }).Function, null!);
                            while (engine.interpreter.Running() && val == null && err == null && !isDone)
                            {
                                try
                                {
                                    engine.interpreter.vm.Step();
                                }
                                catch (Exception e)
                                {
                                    err = "Internal Error: " + e.ToString();

                                    engine.errorHandler.Trigger(err);
                                }
                            }

                            return new Intrinsic.Result(
                                new MapBuilder(Engine.New(ErrorClass))
                                    .AddProp("isError", Utils.Cast(err != null))
                                    .AddProp("isValue", Utils.Cast(val != null))
                                    .AddProp("value", val ?? ValNull.instance)
                                    .AddProp("error", err != null ? new ValString(err) : ValNull.instance)
                                    .map
                            );
                        })
                        .Function
                )
                .map;

            Register("error", map);
        }
    }
}