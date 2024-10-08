﻿
using Kaolin.Flow.Core;
using System;

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
    public static Ptr WritePtr()
    {
        return new AnyPtr(new Add());
    }
    public static Add Instance()
    {
        return new Add();
    }
}