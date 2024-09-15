using Miniscript;

namespace Kaolin.Flow.Builders
{
    public class MapBuilder(ValMap? map = null)
    {
        public readonly ValMap map = map ?? new ValMap();

        public MapBuilder AddProp(string key, Value value)
        {
            map.SetElem(new ValString(key), value);

            return this;
        }
        public MapBuilder AddProp(int key, Value value)
        {
            map.SetElem(new ValNumber(key), value);

            return this;
        }
        public MapBuilder AddProp(float key, Value value)
        {
            map.SetElem(new ValNumber(key), value);

            return this;
        }
        public MapBuilder AddProp(Value key, Value value)
        {
            map.SetElem(key, value);

            return this;
        }
    }
}