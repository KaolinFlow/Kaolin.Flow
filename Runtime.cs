using Kaolin.Flow.Builders;
using Kaolin.Flow.Core;
using Miniscript;

namespace Kaolin.Flow
{
    public class Runtime : Engine
    {
        public Runtime(Interpreter interpreter, string path, bool isDebugging) : base(interpreter, path, isDebugging)
        {
            Inject();
        }
        public void Inject()
        {
            interpreter.SetGlobalValue("KF", new MapBuilder().map);

            _ = new Plugins.Module(this);
            _ = new Plugins.Machine(this);
            _ = new Plugins.Dev(this);
            _ = new Plugins.Http(this);
            _ = new Plugins.Native(this);
            _ = new Plugins.Loader(this);
            _ = new Plugins.Error(this);
        }

    }
}
