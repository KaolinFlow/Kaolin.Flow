import "native"

symbols = { "Add": { "Do": { "args": [native.Type.Int, native.Type.Int], "return": native.Type.Int }, "Does": { "args": [native.Type.Int, native.Type.Int], "return": native.Type.Int }, "Add": { "args": [] } } }

dll = native.import("./native/bin/Debug/net8.0/native.dll", symbols)

print dll
print dll.symbols
print dll.symbols.Add.Do(1, 2)

instance = dll.symbols.Add.Add()

print instance
print instance.Does(1, 2)