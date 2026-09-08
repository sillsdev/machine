using System.Collections.Generic;

namespace SIL.Machine.FiniteState
{
    internal class RegistersEqualityComparer<TOffset> : IEqualityComparer<Register<TOffset>[]>
    {
        private readonly IEqualityComparer<TOffset> _offsetEqualityComparer;

        public RegistersEqualityComparer(IEqualityComparer<TOffset> offsetEqualityComparer)
        {
            _offsetEqualityComparer = offsetEqualityComparer;
        }

        public bool Equals(Register<TOffset>[] x, Register<TOffset>[] y)
        {
            for (int i = 0; i < x.Length; i++)
            {
                if (!x[i].ValueEquals(y[i], _offsetEqualityComparer))
                    return false;
            }
            return true;
        }

        public int GetHashCode(Register<TOffset>[] obj)
        {
            int code = 23;
            for (int i = 0; i < obj.Length; i++)
            {
                if (obj[i].HasOffset)
                {
                    code = code * 31 + _offsetEqualityComparer.GetHashCode(obj[i].Offset);
                    code = code * 31 + obj[i].IsStart.GetHashCode();
                }
                else
                {
                    code = code * 31 + 0;
                }
            }
            return code;
        }
    }
}
