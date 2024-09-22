import "machine"

a = 0

b = function
    return function 
        print outer
    end function
end function

print locals

f = machine.bindOuter(@b, locals) //wont be able to be outbinded, use setOuter to make it bindable. use getOuter to get the outer of a function

fa = f()

fa()