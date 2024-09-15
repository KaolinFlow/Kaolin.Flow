using System.Runtime.Remoting;
using Miniscript;

namespace Kaolin.Flow.Builders
{
    public class Utils
    {
        public static string UnWrapValue(ValString s)
        {
            return s.value;
        }

        public static double UnWrapValue(ValNumber s)
        {
            return s.value;
        }
        public static List<object> UnWrapValue(ValList val)
        {
            List<object> list = [];

            foreach (var v in val.values)
            {
                list.Add(UnWrapValue(v));
            }

            return list;
        }
        public static Dictionary<object, object> UnWrapValue(ValMap val)
        {
            Dictionary<object, object> map = [];

            foreach (var entry in val.map)
            {
                map.Add(UnWrapValue(entry.Key), UnWrapValue(entry.Value));
            }

            return map;
        }
        public static object UnWrapValue(Value value)
        {
            if (value.GetType() == typeof(ValString)) return UnWrapValue((ValString)value);
            if (value.GetType() == typeof(ValNumber)) return UnWrapValue((ValNumber)value);
            //if (value.GetType() == typeof(ValFunction)) return ((ValString)value).value;
            if (value.GetType() == typeof(ValNull)) return null!;
            if (value.GetType() == typeof(ValList)) return UnWrapValue((ValList)value);
            if (value.GetType() == typeof(ValMap)) return UnWrapValue((ValMap)value);

            throw new Exception("Type: " + value.GetType().ToString() + " is not supported");
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
            if (type == null) return ValNull.instance;

            throw new Exception("Type: " + type.ToString() + " is not supported!");
        }

        public static ValString Cast(string s)
        {
            return new ValString(s);
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