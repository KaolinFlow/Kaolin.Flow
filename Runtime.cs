using Kaolin.Flow.Builders;
using Kaolin.Flow.Core;
using Miniscript;

namespace Kaolin.Flow
{
    class Runtime : Engine
    {
        public Runtime(Interpreter interpreter, string path, bool isDebugging) : base(interpreter, path, isDebugging)
        {
            Inject();
        }
        public void Inject()
        {
            interpreter.SetGlobalValue("KF", new MapBuilder().map);

            new Plugins.Module(this).Inject();
            new Plugins.Machine(this).Inject();
            new Plugins.Dev(this).Inject();
            new Plugins.Http(this).Inject();
            new Plugins.Native(this).Inject();
        }

    }
}