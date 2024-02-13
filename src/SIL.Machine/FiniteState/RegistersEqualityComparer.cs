using System.Collections.Generic;

namespace SIL.Machine.FiniteState
{
    internal class RegistersEqualityComparer<TOffset> : IEqualityComparer<Register<TOffset>[,]>
    {
        private readonly IEqualityComparer<TOffset> _offsetEqualityComparer;

        public RegistersEqualityComparer(IEqualityComparer<TOffset> offsetEqualityComparer)
        {
            _offsetEqualityComparer = offsetEqualityComparer;
        }

        public bool Equals(Register<TOffset>[,] x, Register<TOffset>[,] y)
        {
            for (int i = 0; i < x.GetLength(0); i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    if (!x[i, j].ValueEquals(y[i, j], _offsetEqualityComparer))
                        return false;
                }
            }
            return true;
        }

        public int GetHashCode(Register<TOffset>[,] obj)
        {
            int code = 23;
            for (int i = 0; i < obj.GetLength(0); i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    if (obj[i, j].HasOffset)
                    {
                        code = code * 31 + _offsetEqualityComparer.GetHashCode(obj[i, j].Offset);
                        code = code * 31 + obj[i, j].IsStart.GetHashCode();
                    }
                    else
                        code = code * 31 + 0;
                }
            }
            return code;
        }
    }
}
