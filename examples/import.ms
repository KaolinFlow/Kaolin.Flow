imports["./e"] = function()
    return newModule(import("./import/import/d"), "e")
end function

imported = import("./import/a")

print "Executed from: " + path + " received " + imported
print imported