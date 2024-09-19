
using Kaolin.Flow.Core;

class Add
{
    public static int Do(int a, int b)
    {
        return a + b;
    }
    public int Does(int a, int b)
    {
        return a + b;
    }

    public static void ReadPtr(Ptr ptr)
    {
        Console.WriteLine(ptr.Value);
    }
    public static unsafe Ptr WritePtr()
    {
        return new AnyPtr(new Add());
    }
    public static Add Instance()
    {
        return new Add();
    }
}