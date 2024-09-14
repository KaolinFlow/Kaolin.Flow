using Miniscript;

namespace Kaolin.Flow.Builders
{
    public class ListBuilder(ValList? list = null)
    {
        public readonly ValList list = list ?? new ValList();

        public ListBuilder AddProp(Value value)
        {
            list.values.Add(value);

            return this;
        }
    }
}