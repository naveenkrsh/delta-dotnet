using Parquet.Schema;

namespace DeltaLake.Operations {
    internal class ParquetToSparkSchemaConverter {
        public static string ConvertToSparkJsonSchema(ParquetSchema parquetSchema) {
            List<string> fields = new List<string>();

            foreach(DataField field in parquetSchema.DataFields) {
                fields.Add(ConvertFieldToSparkJson(field));
            }

            return $"{{ \"type\": \"struct\", \"fields\": [{string.Join(", ", fields)}] }}";
        }

        private static string ConvertFieldToSparkJson(DataField field) {
            string sparkType = MapParquetTypeToSparkType(field.ClrType);
            return $"{{ \"name\": \"{field.Name}\", \"type\": \"{sparkType}\", \"nullable\": true, \"metadata\": {{}} }}";
        }

        private static string MapParquetTypeToSparkType(Type clrType) {
            return clrType switch {
                Type t when t == typeof(int) => "integer",        // Map Int32
                Type t when t == typeof(long) => "long",          // Map Int64
                Type t when t == typeof(bool) => "boolean",       // Map Boolean
                Type t when t == typeof(double) => "double",      // Map Double
                Type t when t == typeof(string) => "string",      // Map String
                Type t when t == typeof(float) => "float",        // Map Float
                Type t when t == typeof(decimal) => "decimal",    // Map Decimal
                Type t when t == typeof(DateTime) => "timestamp", // Map DateTime
                Type t when t == typeof(byte) => "byte",          // Map Byte
                Type t when t == typeof(short) => "short",        // Map Int16
                Type t when t == typeof(char) => "char",          // Map Char
                Type t when t.IsArray && t.GetElementType() != null && t.GetElementType() == typeof(byte) => "binary", // For byte arrays
                Type t when t.IsArray => "array",                 // ListType to ArrayType (can be extended for specific element types)
                Type t when t.IsClass => "struct",                // For complex types (e.g., StructType)
                _ => throw new NotImplementedException($"Mapping for {clrType} is not implemented.")
            };
        }

    }
}

