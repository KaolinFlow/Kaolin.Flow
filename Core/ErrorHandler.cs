using System.Collections.Generic;
using System.IO;
using System;

namespace Kaolin.Flow.Core
{
    public class ErrorHandler(Engine engine)
    {
        public delegate bool Callback(string error);

        public List<Callback> callbacks = [];

        public Engine engine = engine;

        public void On(Callback callback)
        {
            if (!callbacks.Exists((c) => c == callback)) callbacks.Insert(0, callback);
        }
        public void Off(Callback callback)
        {
            callbacks.Remove(callback);
        }

        public void Trigger(string e)
        {
            foreach (var callback in callbacks)
            {
                if (callback(e)) return;
            }

            engine.Print("Unhandled Error:\n\t" + e);
            Environment.Exit(1);
        }
    }
}
