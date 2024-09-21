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
    public unsafe class AnyPtr(object val) : Ptr()
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
    public unsafe class ValPtr(Ptr userData) : ValMap()
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
}