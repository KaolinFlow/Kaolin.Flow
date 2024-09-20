import "machine"

file = machine.file

if file.exists("test3") then
    file.delete "test3", true
end if

if file.exists("test2") then
    file.delete "test2", true
end if

if file.exists("test") then
    file.delete "test", true
end if

print "Name: " + file.name(path)
print "Parent: " + file.parent(path)
print "Dir: " + file.curdir()
file.setdir "../"
print "Child: " + file.child(file.curdir(), "examples")
print "Dir: " + file.curdir()
file.setdir "./examples" 
file.makedir "test" 
file.copy "test", "test2" 
file.move "test2", "test3" 
file.writeLines "test3/a.txt", ["hi!", "yo!"]
print "Content of test3/a.txt: " + file.readLines("test3/a.txt")
print "Info test3: " + file.info("test3")
print "List of examples " + file.children("./")

f = file.open("test3/a.txt")

print "Is Open test3/a.txt: " + f.isOpen()

f.write "new"
f.writeLine "new line"
f.writeLine "new line 2"

print "test3/a.txt line 1: " + f.readLine()

print "test3/a.txt line 2-3: " + f.read()

print "test3/a.txt position: " + f.position()

print "test3/a.txt is at end: " + f.atEnd()

f.close

file.delete "test3", true
file.delete "test", true