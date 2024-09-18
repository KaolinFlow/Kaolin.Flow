using Kaolin.Flow.Core;
using Miniscript;


namespace Kaolin.Flow.Builders
{
    public class MapPointer : Dictionary<object, object>
    {

    }
    public class ListPointer : List<object>
    {

    }
    public delegate Value FunctionPointer(params Value[] args);
    public class Utils
    {

        public static object UnWrapValue(Value value, Engine engine)
        {
            if (value.GetType() == typeof(ValString)) return UnWrapValue((ValString)value);
            if (value.GetType() == typeof(ValNumber)) return UnWrapValue((ValNumber)value);
            if (value.GetType() == typeof(ValFunction)) return UnWrapValue((ValFunction)value, engine);
            if (value.GetType() == typeof(ValNull)) return null!;
            if (value.GetType() == typeof(ValList)) return UnWrapValue((ValList)value, engine);
            if (value.GetType() == typeof(ValMap)) return UnWrapValue((ValMap)value, engine);

            throw new Exception("Type: " + value.GetType().ToString() + " is not supported");
        }
        public static string UnWrapValue(ValString s)
        {
            return s.value;
        }

        public static double UnWrapValue(ValNumber s)
        {
            return s.value;
        }
        public static ListPointer UnWrapValue(ValList val, Engine engine)
        {
            ListPointer list = [];

            foreach (var v in val.values)
            {
                list.Add(UnWrapValue(v!, engine));
            }

            return list;
        }
        public static MapPointer UnWrapValue(ValMap val, Engine engine)
        {
            MapPointer map = [];

            foreach (var entry in val.map)
            {
                map.Add(UnWrapValue(entry.Key, engine), UnWrapValue(entry.Value, engine));
            }

            return map;
        }
        public static FunctionPointer UnWrapValue(ValFunction val, Engine engine)
        {
            return (params Value[] args) =>
            {
                return engine.InvokeValue(val, args).GetAwaiter().GetResult();
            };
        }
        public static Value Cast(object v)
        {

            Type type = v.GetType();
            if (type == typeof(string)) return Cast((string)v);
            if (type == typeof(double)) return Cast((double)v);
            if (type == typeof(float)) return Cast((float)v);
            if (type == typeof(long)) return Cast((long)v);
            if (type == typeof(int)) return Cast((int)v);
            if (type == typeof(bool)) return Cast((bool)v);
            if (type == typeof(FunctionPointer)) return Cast((FunctionPointer)v);
            if (type == typeof(MapPointer)) return Cast((MapPointer)v);
            if (type == typeof(ListPointer)) return Cast((ListPointer)v);
            if (type == null) return ValNull.instance;

            throw new Exception("Type: " + type.ToString() + " is not supported!");
        }

        public static ValString Cast(string s)
        {
            return new ValString(s);
        }

        public static ValMap Cast(MapPointer map)
        {
            ValMap r = new();

            foreach (var entry in map)
            {
                r.SetElem(Cast(entry.Key), Cast(entry.Value));
            }

            return r;
        }

        public static ValList Cast(ListPointer list)
        {
            ValList r = new();

            foreach (var v in list)
            {
                r.values.Add(Cast(v));
            }

            return r;
        }


        public static ValFunction Cast(FunctionPointer f)
        {
            return new FunctionBuilder()
                .SetCallback((context, p) =>
                {
                    return new Intrinsic.Result(f([.. context.args]), false);
                })
                .Function;
        }
        public static ValNumber Cast(int n)
        {
            return new ValNumber(n);
        }

        public static ValNumber Cast(float n)
        {
            return new ValNumber(n);
        }
        public static ValNumber Cast(double n)
        {
            return new ValNumber(n);
        }
        public static ValNumber Cast(long n)
        {
            return new ValNumber(n);
        }

        public static ValNumber Cast(bool b)
        {
            return new ValNumber(b ? 1 : 0);
        }

        public static bool EndsWith(byte[] A, byte[] B)
        {
            if (B.Length > A.Length)
                return false;

            for (int i = 0; i < B.Length; i++)
            {
                if (A[A.Length - B.Length + i] != B[i])
                    return false;
            }

            return true;
        }

    }
}