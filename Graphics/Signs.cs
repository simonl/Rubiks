using System;

namespace Graphics
{
    public enum Sign
    {
        Negative = -1,
        Zero = 0,
        Positive = +1,
    }
    
    /*
     *  + | 0 | 1 | 2
     *  -------------
     *  0 | 0 | 1 | 2
     *  -------------
     *  1 | 1 | 2 | 0
     *  -------------
     *  2 | 2 | 0 | 1
     *  
     *  + | - | 0 | +
     *  -------------
     *  - | + | - | 0
     *  -------------
     *  0 | - | 0 | +
     *  -------------
     *  + | 0 | + | -
     *  
     *  x | - | 0 | +
     *  -------------
     *  - | + | 0 | -
     *  -------------
     *  0 | 0 | 0 | 0
     *  -------------
     *  + | - | 0 | +
     *  
     *  x | - | +
     *  ---------
     *  - | + | -
     *  ---------
     *  + | - | +
     *  
     * 
     */
    public static class Signs
    {
        public static Sign Negate(this Sign sign)
        {
            return sign.Add(sign);
        }

        public static Sign Absolute(this Sign sign)
        {
            return sign.Multiply(sign);
        }

        public static Sign Multiply(this Sign left, Sign right)
        {
            return (Sign)((int)left * (int)right);
        }

        public static Sign Add(this Sign left, Sign right)
        {
            var modular = (left.Modular() + right.Modular()) % 3;

            return modular.Significant();
        }
        
        private static int Modular(this Sign sign)
        {
            return ((int)sign + 3) % 3;
        }
        
        private static Sign Significant(this int modular)
        {
            switch (modular)
            {
                case 0:
                    return Sign.Zero;
                case 1:
                    return Sign.Positive;
                case 2:
                    return Sign.Negative;
                default:
                    throw new ArgumentException("modular");
            }
        }
        
        public static string Show(this Sign sign)
        {
            switch (sign)
            {
                case Sign.Negative:
                    return "-";
                case Sign.Zero:
                    return "0";
                case Sign.Positive:
                    return "+";
                default:
                    throw new ArgumentException("sign");
            }
        }
    }
}
