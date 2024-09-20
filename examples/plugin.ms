import "plugin"

plugin.load "./pluginHelloWorld/bin/Debug/net8.0/pluginHelloWorld.dll", ["Plugin"]
import "helloWorld"
print helloWorld