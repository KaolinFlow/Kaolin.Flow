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