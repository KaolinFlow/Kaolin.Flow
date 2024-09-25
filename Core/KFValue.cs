using Miniscript;

namespace Kaolin.Flow.Core
{

    abstract public class Ptr
    {
        public abstract object Value
        {
            get; set;
        }
    }
    public class AnyPtr(object val) : Ptr()
    {
        object v = val;

        public override object Value
        {
            get
            {
                return v;
            }
            set
            {
                v = value;
            }
        }
    }
    /// <summary>
    /// ValPtr represents a pointer value.
    /// </summary>
    /// 
    public class ValPtr(Ptr userData) : ValMap()
    {
        public new Ptr userData = userData;

        public override string ToString(TAC.Machine vm)
        {
            return "Pointer{" + userData.Value.GetType() + "}";
        }

        public override string CodeForm(TAC.Machine vm, int recursionLimit = -1)
        {
            return "[pointer]";
        }

        public override bool BoolValue()
        {
            return true;
        }

        public override bool IsA(Value type, TAC.Machine vm)
        {
            if (type == null) return false;
            return type == vm.stringType;
        }

        public override int Hash()
        {
            return userData.Value.GetHashCode();
        }

        public override double Equality(Value rhs)
        {
            return rhs is ValPtr ptr && ptr.userData.Value == userData.Value ? 1 : 0;
        }
    }
    /// <summary>
    /// ValBFunction represents a function value.
    /// </summary>
    /// 
    public class ValBFunction(Function function, ValMap variables) : ValFunction(function, variables)
    {
        public override ValFunction BindAndCopy(ValMap contextVariables)
        {
            return this;
        }
        public static ValFunction Bind(ValFunction function, ValMap variables)
        {
            return new ValBFunction(function.function, variables);
        }
        public static ValFunction Bind(ValFunction function)
        {
            return new ValBFunction(function.function, function.outerVars);
        }
    }
}