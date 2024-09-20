using Kaolin.Flow.Builders;
using Kaolin.Flow.Core;
using Miniscript;

namespace Kaolin.Flow.Plugins
{
    public abstract class Base
    {

        public readonly Engine engine;

        public Base(Engine engine)
        {
            this.engine = engine;

            Inject();
        }
        public void Register(string key, Value value)
        {
            engine.interpreter.GetGlobalValue("imports").SetElem(new ValString(key), new FunctionBuilder().SetCallback((context, p) =>
            {
                return new Intrinsic.Result(Module.NewModule(value, key));
            }).Function);
        }
        public void Register(string key, IntrinsicCode code)
        {
            engine.interpreter.GetGlobalValue("imports").SetElem(new ValString(key), new FunctionBuilder().SetCallback((context, p) =>
            {
                return code(context, p);
            }).Function);
        }

        public abstract void Inject();
    }
}