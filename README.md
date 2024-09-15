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
You may import `machine` and `dev`! Please refer to [this doc](https://miniscript.org/cmdline/) as an extra guideline.<br />
Work on Progress: `file`

## New Module
```
import "test"

print test

imported = import("test2")

print imported
print test2

imported_but_no_auto_variable = import("test3", false)

print imported_but_no_auto_variable

print e

return "Exported"
```

## HTTP Import
```
import "http://localhost/from_http"

print from_http
```

## Import Map
```
imports["./e"] = "./f" //will only match the exact string! (please contribute to make it better)

import "e"

imports["./f"] = "Hello!"

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

## Native Code Import (Work on Progress)

## HTTP (Work on Progress)

## WebSocket (Work on Progress)

## Native GUI (Work on Progress)

# Sponsor
[PayPalmeplsIndonesianGovermentSucksALot](https://paypal.me/nekomaru76)