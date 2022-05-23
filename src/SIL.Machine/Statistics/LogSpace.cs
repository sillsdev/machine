using System;

namespace SIL.Machine.Statistics
{
    public static class LogSpace
    {
        public const double One = 0;
        public const double Zero = -999999999;

        public static double Add(double logx, double logy)
        {
            if (logx > logy)
                return logx + Math.Log(1 + Math.Exp(logy - logx));
            return logy + Math.Log(1 + Math.Exp(logx - logy));
        }

        public static double Multiply(double logx, double logy)
        {
            double result = logx + logy;
            if (result < Zero)
                result = Zero;
            return result;
        }

        public static double Divide(double logx, double logy)
        {
            double result = logx - logy;
            if (result < Zero)
                result = Zero;
            return result;
        }

        public static double ToLogSpace(double value)
        {
            return Math.Log(value);
        }

        public static double ToStandardSpace(double logvalue)
        {
            return Math.Exp(logvalue);
        }
    }
}
