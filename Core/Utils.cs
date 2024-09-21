using Kaolin.Flow.Builders;
using Kaolin.Flow.Core;
using Miniscript;


namespace Kaolin.Flow.Core
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
        public static string UnWrapPath(string basePath, string path)
        {
            return ResolvePath(new Uri(new Uri(basePath), "./").AbsolutePath, path).AbsolutePath;
        }

        public static bool IsHTTP(string path)
        {
            return path.StartsWith("https://") || path.StartsWith("http://");
        }
        public static string RemoveProtocol(string path)
        {
            if (HasProtocol(path)) return path[(new Uri(path).Scheme + "://").Length..];

            return path;
        }
        public static string GetProtocol(string path)
        {
            if (HasProtocol(path)) return new Uri(path).Scheme;

            return "file";
        }
        public static string WrapWithProtocol(string path)
        {
            string p = GetProtocol(path);

            return p + "://" + path;
        }
        public static Uri ResolvePath(string basePath, string relativePath)
        {
            if (HasProtocol(relativePath))
            {
                return new Uri(relativePath);
            }
            else if (IsHTTP(basePath))
            {
                return new Uri(new Uri(basePath), relativePath);
            }
            else
            {
                return new Uri(WrapWithProtocol(Path.Combine(RemoveProtocol(basePath), relativePath)));
            }
        }

        public static string WrapPath(string s)
        {
            if (HasProtocol(s)) return s;

            return "file://" + s;
        }
        public static bool HasProtocol(string s)
        {
            return IsHTTP(s) || s.StartsWith("file://");
        }

        public static object UnWrapValue(Value value, Engine engine)
        {
            if (value is ValString v1) return UnWrapValue(v1);
            if (value is ValNumber v2) return UnWrapValue(v2);
            if (value is ValFunction v3) return UnWrapValue(v3, engine);
            if (value is ValNull) return null!;
            if (value is ValList v5) return UnWrapValue(v5, engine);
            if (value is ValPtr v7) return UnWrapValue(v7);
            if (value is ValMap v6) return UnWrapValue(v6, engine);

            throw new Exception("Type: " + value.GetType().ToString() + " is not supported");
        }
        public static string UnWrapValue(ValString s)
        {
            return s.value;
        }
        public static object UnWrapValue(ValPtr s)
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
                return engine.InvokeValue(val, args);
            };
        }
        public static unsafe Value Cast(object v)
        {
            if (v is string v3) return Cast(v3);
            if (v is double v2) return Cast(v2);
            if (v is float v1) return Cast(v1);
            if (v is long v4) return Cast(v4);
            if (v is int v5) return Cast(v5);
            if (v is bool v6) return Cast(v6);
            if (v is Ptr v10) return Cast(v10);
            if (v is FunctionPointer v7) return Cast(v7);
            if (v is MapPointer v8) return Cast(v8);
            if (v is ListPointer v9) return Cast(v9);
            if (v is null) return ValNull.instance;

            throw new Exception("Type: " + v.GetType().ToString() + " is not supported!");
        }

        public static ValPtr Cast(Ptr o)
        {
            return new ValPtr(o);
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
            return new FunctionBuilder(f.Method.Name)
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