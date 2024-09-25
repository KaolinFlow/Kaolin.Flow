import "error"
import "machine"

willError = function()
    print "Yes!"

    error.throw "Hello!"

    print "No!"
end function

wontError = function(a, b)
    return a + b
end function

breakingError = function()
    print "Yes!"

    unknownVariable

    print "No!"
end function

print "Trying..."
print error.try(@wontError, [1, 3])
print error.try(@willError)
print error.try(@breakingError)
print "Done!"