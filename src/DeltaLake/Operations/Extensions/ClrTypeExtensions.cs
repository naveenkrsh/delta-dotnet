namespace DeltaLake.Operations.Extensions {
    internal static class ClrTypeExtensions {
        public static object Max(object a, object b, Type clrType) {
            if(clrType == typeof(int))
                return Math.Max((int)a, (int)b);
            else if(clrType == typeof(double))
                return Math.Max((double)a, (double)b);
            else if(clrType == typeof(float))
                return Math.Max((float)a, (float)b);
            else if(clrType == typeof(long))
                return Math.Max((long)a, (long)b);
            else if(clrType == typeof(decimal))
                return Math.Max((decimal)a, (decimal)b);
            else if(clrType == typeof(string))
                return string.Compare((string)a, (string)b) > 0 ? a : b;
            else if(clrType == typeof(bool))
                return ((bool)a || (bool)b) ? a : b;
            else if(clrType == typeof(DateTime))
                return ((DateTime)a > (DateTime)b) ? a : b;
            else if(clrType == typeof(byte))
                return Math.Max((byte)a, (byte)b);
            else
                throw new ArgumentException("Unsupported clrType");
        }


        public static object Min(object a, object b, Type clrType) {
            if(clrType == typeof(int))
                return Math.Min((int)a, (int)b);
            else if(clrType == typeof(double))
                return Math.Min((double)a, (double)b);
            else if(clrType == typeof(float))
                return Math.Min((float)a, (float)b);
            else if(clrType == typeof(long))
                return Math.Min((long)a, (long)b);
            else if(clrType == typeof(decimal))
                return Math.Min((decimal)a, (decimal)b);
            else if(clrType == typeof(string))
                return string.Compare((string)a, (string)b) < 0 ? a : b;
            else if(clrType == typeof(bool))
                return ((bool)a && (bool)b) ? a : b;
            else if(clrType == typeof(DateTime))
                return ((DateTime)a < (DateTime)b) ? a : b;
            else if(clrType == typeof(byte))
                return Math.Min((byte)a, (byte)b);
            else
                throw new ArgumentException("Unsupported clrType");
        }
    }
}