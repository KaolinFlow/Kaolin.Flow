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
                            string error = context.GetLocalString("error");
                            RuntimeException exception = new(error);
                            exception.location = context.GetSourceLoc() ?? context.parent.GetSourceLoc();
                            engine.interpreter.errorOutput.Invoke(exception.Description(), true);
                            context.parent.JumpToEnd();

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
                            ValFunction callback = (ValFunction)context.GetLocal("callback");
                            ErrorHandler.Callback errorCallback = null!;
                            List<Value> errors = [];
                            ValList args = (ValList)context.GetLocal("args");
                            ValMap locals = context.parent.variables;
                            Value value = null!;
                            bool isDone = false;

                            engine.interpreter.vm.ManuallyPushCall(new FunctionBuilder().SetCallback((context, partialResult) =>
                            {
                                if (partialResult != null)
                                {
                                    Value returnValue = context.GetTemp(0);
                                    value = returnValue;
                                    isDone = true;

                                    return new Intrinsic.Result(value);
                                }

                                engine.interpreter.vm.ManuallyPushCall(callback, new ValTemp(0), [.. args.values]);
                                return new Intrinsic.Result(ValNull.instance, false);
                            }).Function, null!);

                            TAC.Context callbackContext = engine.interpreter.vm.GetTopContext();
                            errorCallback = (error) =>
                            {
                                errors.Add(new ValString(error));
                                callbackContext.ClearCodeAndTemps();

                                isDone = true;

                                return true;
                            };

                            engine.errorHandler.On(errorCallback);

                            /*try
                            {*/
                            while (engine.interpreter.Running() && !isDone)
                            {
                                engine.interpreter.vm.Step();
                            }
                            /*}
                            catch (Exception exception)
                            {
                                string error = "Internal Error: " + exception.ToString();

                                errors.Add(new ValString(error));
                                engine.errorHandler.Trigger(error);
                            }*/

                            engine.errorHandler.Off(errorCallback);
                            return new Intrinsic.Result(
                                new MapBuilder(Engine.New(ErrorClass))
                                    .AddProp("isError", Utils.Cast(errors.Count != 0))
                                    .AddProp("isValue", Utils.Cast(value != null))
                                    .AddProp("value", value ?? ValNull.instance)
                                    .AddProp("error", new ValList(errors))
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