# Kaolin.Flow (WIP) (0.0.4-alpha1)
MiniScript's Runtime, just like Node to JavaScript :).

# Why?
Focusing solely on adding new features to make the language production-ready without having to dirt the main repository + making everything be built on MiniScript!

# How to Use
You may run the program like how you run your script with MiniScript commandline version.
1. Make sure to clone all the submodules.
2. Run using `dotnet run <file.ms>`!
3. ???<br />

# Note
1. Don't use the `import` and `importMeta` names to create new variables 'cause they are essentials for the functions of pre-existing plugins.
2. `KF` map in global indicates that the code is being executed within Kaolin.Flow.

# Features
## MiniScript's Commandline Intrinsics
You may import `machine` and `dev`! Please refer to [this doc](https://miniscript.org/cmdline/) as an extra guideline.

## New Module
```
import "test"

print test

imported = import("test2")

print imported
print test2

imported_but_no_auto_variable = import("test3", false)

print imported_but_no_auto_variable

return "Exported"
```

## HTTP Import
```
import "http://localhost/from_http"

print from_http
```

## Import Map
```
//will only match the exact string! (please contribute to make it better)
importMeta.imports["./e"] = function(path)
    return importMeta.newModule(import("./f"), "e")
end function 

import "e"

importMeta.imports["./f"] = function()
    return importMeta.newModule("Hello!", "f")
end function

import "f"

print e
print f
```

## Eval (WIP)
Compile the code argument into a function then execute it.
```
eval "return 0"
```

## REPL
Just run the program without any argument.

## HTTP
`data` is always a list of bytes (array of integer) or a string and `headers` is always a map with key and value of strings. Import `http` for using these:
| http    | arg1 | arg2    | arg3    |   |
|---------|------|---------|---------|---|
| get(    | url  | headers |         | ) |
| delete( | url  | headers |         | ) |
| post(   | url  | data    | headers | ) |
| put(    | url  | data    | headers | ) |

## Plugin
Inject your own C# Plugin within MiniScript. Please view `examples/plugin.ms` and `examples/pluginHelloWorld`.

## Native Code Import (WIP)
```
import "native"

symbols = { "Add": { "WritePtr": { "args": [], "return": native.retDef(native.Type.Pointer) }, "ReadPtr": { "args": [native.Type.Pointer] }, "Do": { "args": [native.Type.Int, native.Type.Int], "return": native.retDef(native.Type.Int) }, "Does": { "args": [native.Type.Int, native.Type.Int], "return": native.retDef(native.Type.Int) }, "Add": { "args": [] } } }

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
```

## Error Handler (WIP)
Notice how I put a second import into the `willError` function. Because passing a callback to a plugin and let the plugin execute it will 'cause the function to lose its context of `locals` thus causing `error` to be undefined. Sometimes this error handler will break due to variable issues and such.
```
import "error"

willError = function()
    import "error"
    error.throw "Hello!"
end function

wontError = function(a, b)
    return a + b
end function

print "Trying..."
print error.try(@willError)
print error.try(@wontError, [1, 3])
```

## Bind Outer Variables
`setOuter(callback, outer)` will set the outer variables of a callback but still let the function's outer to be changeable while the `bindOuter` make it unchangeable. Get the outer of a function with `getOuter(callback)`.

## Native GUI (Work on Progress)

## WebSocket (Work on Progress)


# Sponsor
[PayPalmeplsIndonesianGovermentSucksALot](https://paypal.me/nekomaru76)