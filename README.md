# Kaolin.Flow (WIP)
MiniScript's Runtime, just like Node to JavaScript :).

# Why?
Focusing solely on adding new features to make the language production-ready without having to dirt the main repository + making everything be built on MiniScript!

# How to Use
1. Make sure to clone all the submodules.
2. Run using `dotnet run --property WarningLevel=0 <file.ms>`!
3. ???
You may run the program like how you run your script with MiniScript commandline version.

# Features
## MiniScript's Commandline Intrinsics
Please refer to [this doc](https://miniscript.org/cmdline/)
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

return "Exported"
```

## REPL
Just run the program without any argument.

## HTTP Import (Work on Progress)

## Import Map (Work on Progress)

## Native Code Import (Work on Progress)

## HTTP (Work on Progress)

## WebSocket (Work on Progress)

## Native GUI (Work on Progress)

# Sponsor
[PayPalmeplsIndonesianGovermentSucksALot](https://paypal.me/nekomaru76)