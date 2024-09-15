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
            engine.interpreter.GetGlobalValue("imports").SetElem(new ValString(key), value);
        }

        public abstract void Inject();
    }
}