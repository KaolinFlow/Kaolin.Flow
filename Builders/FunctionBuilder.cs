using Miniscript;

namespace Kaolin.Flow.Builders
{
    public class FunctionBuilder
    {
        public readonly Intrinsic intrinsic;

        public FunctionBuilder(Intrinsic? intrinsic = null)
        {
            if (intrinsic == null)
            {
                this.intrinsic = Intrinsic.Create("anonymous");
            }
            else this.intrinsic = intrinsic;
        }
        public FunctionBuilder(string name)
        {
            intrinsic = Intrinsic.Create(name);
        }

        public FunctionBuilder AddParam(string name, Value? defaultValue = null)
        {
            intrinsic.AddParam(name, defaultValue!);

            return this;
        }

        public FunctionBuilder AddParam(string name, string defaultValue)
        {
            intrinsic.AddParam(name, Utils.Cast(defaultValue));

            return this;
        }

        public FunctionBuilder AddParam(string name, int defaultValue)
        {
            intrinsic.AddParam(name, Utils.Cast(defaultValue));

            return this;
        }

        public FunctionBuilder AddParam(string name, float defaultValue)
        {
            intrinsic.AddParam(name, Utils.Cast(defaultValue));

            return this;
        }

        public FunctionBuilder AddParam(string name, bool defaultValue)
        {
            intrinsic.AddParam(name, Utils.Cast(defaultValue));

            return this;
        }

        public FunctionBuilder SetCallback(IntrinsicCode code)
        {
            intrinsic.code = code;

            return this;
        }

        public ValFunction Function
        {
            get
            {
                return intrinsic.GetFunc();
            }
        }
    }
}