import "native"

symbols = {}
symbols["Add"] = {}
symbols["Add"]["Do"] = {}
symbols["Add"]["Do"]["argsLength"] = 2

dll = native.import("./native/bin/Debug/net8.0/native.dll", symbols)

print dll
print dll.symbols.Add.Do(1, 2)