import "native"

symbols = {}
symbols["Add"] = {}
symbols["Add"]["Do"] = {}
symbols["Add"]["Do"]["args"] = [native.Type.Int, native.Type.Int]
symbols["Add"]["Do"]["return"] = native.Type.Int
symbols["Add"]["Do"]["isStatic"] = true
symbols["Add"]["Does"] = {}
symbols["Add"]["Does"]["args"] = [native.Type.Int, native.Type.Int]
symbols["Add"]["Does"]["return"] = native.Type.Int
symbols["Add"]["Does"]["isStatic"] = false
symbols["Add"]["Add"] = {}
symbols["Add"]["Add"]["args"] = []


dll = native.import("./native/bin/Debug/net8.0/native.dll", symbols)

print dll
print dll.symbols
print dll.symbols.Add.Do(1, 2)

instance = dll.symbols.Add.Add()

print instance
print instance.Does(1, 2)