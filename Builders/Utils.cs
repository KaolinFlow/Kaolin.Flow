using Miniscript;

namespace Kaolin.Flow.Builders
{
    public class Utils
    {

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

        public static ValNumber Cast(bool b)
        {
            return new ValNumber(b ? 1 : 0);
        }
    }
}