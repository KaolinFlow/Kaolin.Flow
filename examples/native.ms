import "native"

symbols = { "Add": { "WritePtr": { "args": [], "return": native.retDef(native.Type.Pointer) }, "ReadPtr": { "args": [native.Type.Pointer], "return": native.retDef(native.Type.Void) }, "Do": { "args": [native.Type.Int, native.Type.Int], "return": native.retDef(native.Type.Int) }, "Does": { "args": [native.Type.Int, native.Type.Int], "return": native.retDef(native.Type.Int) }, "Add": { "args": [] } } }

symbols["Add"]["Instance"] = { "args": [], "return": native.retDef(native.Type.Instance, symbols["Add"]) }

dll = native.import("./native/bin/Debug/net8.0/native.dll", symbols)

print dll
print dll.symbols
print dll.symbols.Add.Do(1, 2)

instance = dll.symbols.Add.Add()

print instance
print instance.Does(1, 2)

ptr = dll.symbols.Add.WritePtr()

print ptr
dll.symbols.Add.ReadPtr(ptr)

add = dll.symbols.Add.Instance()

print add