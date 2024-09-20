using Kaolin.Flow.Core;
using Kaolin.Flow.Plugins;
using Miniscript;

class Plugin(Engine engine) : Base(engine)
{
    public override void Inject()
    {
        Register("helloWorld", new ValString("Hello World!"));
    }
}