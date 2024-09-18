imports["./e"] = function()
    return import("./import/import/d")
end function

imported = import("./import/a")

print "Executed from: " + path + " received " + imported
print imported