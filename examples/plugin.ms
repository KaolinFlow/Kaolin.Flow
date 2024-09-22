import "plugin"

plugin.load "./pluginHelloWorld/bin/Release/net8.0/pluginHelloWorld.dll", ["Plugin"]
import "helloWorld"
print helloWorld