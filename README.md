# Kaolin.Flow (WIP)
MiniScript's Runtime, just like Node to JavaScript :).

# Why?
Focusing solely on adding new features to make the language production-ready without having to dirt the main repository + making everything be built on MiniScript!

# How to Use
You may run the program like how you run your script with MiniScript commandline version.
1. Make sure to clone all the submodules.
2. Run using `dotnet run --property WarningLevel=0 <file.ms>`!
3. ???<br />

# Features
## MiniScript's Commandline Intrinsics
You may import `machine` and `dev`! Please refer to [this doc](https://miniscript.org/cmdline/) as an extra guideline. `file` is untested and may breaks.<br />

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
imports["./e"] = function(path)
    return import("./f")
end function 

import "e"

imports["./f"] = function()
    return "Hello!"
end function

import "f"

print e
print f
```

## Eval
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

## Native Code Import (WIP)
```
import "native"

symbols = {}
symbols["Add"] = {}
symbols["Add"]["Do"] = {}
symbols["Add"]["Do"]["argsLength"] = 2

dll = native.import("./native.dll", symbols)

print dll
print dll.symbols.Add.Do(1, 2)
```

## Native GUI (Work on Progress)

## WebSocket (Work on Progress)


# Sponsor
[PayPalmeplsIndonesianGovermentSucksALot](https://paypal.me/nekomaru76)