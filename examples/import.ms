importMeta.imports["./e"] = function()
    return importMeta.newModule(import("./import/import/d"), "e")
end function

imported = import("./import/a")

print "Executed from: " + importMeta.path + " received " + imported
print imported