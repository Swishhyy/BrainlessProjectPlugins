using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace Brainless.Internal.Updates
{
    // A small strict JSON codec for updater DTOs and the stats database. It never
    // initializes DataContractSerializer's machine/app configuration machinery.
    internal static class JsonContract
    {
        internal static T Read<T>(byte[] data)
        {
            var reader = new Reader(new UTF8Encoding(false, true).GetString(data));
            object value = reader.Value(0);
            reader.End();
            return (T)Bind(value, typeof(T));
        }

        internal static byte[] Write(object value)
        {
            var text = new StringBuilder();
            int nodes = 0;
            Emit(text, value, 0, ref nodes);
            return Encoding.UTF8.GetBytes(text.ToString());
        }

        private static PropertyInfo[] Properties(Type type) => type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0 && (!type.IsDefined(typeof(DataContractAttribute), true) || p.IsDefined(typeof(DataMemberAttribute), true))).ToArray();
        private static string Name(PropertyInfo property) => property.GetCustomAttribute<DataMemberAttribute>()?.Name ?? property.Name;
        private static object Bind(object value, Type type)
        {
            if (value == null)
            {
                if (type.IsValueType && Nullable.GetUnderlyingType(type) == null)
                    throw new FormatException("JSON null is not valid for this field.");
                return null;
            }

            if (type == typeof(string))
                return value is string ? value : throw new FormatException("Expected JSON string.");
            if (type == typeof(bool))
                return value is bool ? value : throw new FormatException("Expected JSON boolean.");
            if (value is Number number)
            {
                try
                {
                    return Convert.ChangeType(number.Text, type, CultureInfo.InvariantCulture);
                }
                catch (Exception ex)when (ex is FormatException || ex is OverflowException || ex is InvalidCastException)
                {
                    throw new FormatException("Invalid JSON number for its field.");
                }
            }

            if (type.IsArray && value is List<object> list)
            {
                var result = Array.CreateInstance(type.GetElementType(), list.Count);
                for (int i = 0; i < list.Count; i++)
                    result.SetValue(Bind(list[i], type.GetElementType()), i);
                return result;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>) && value is List<object> items)
            {
                var result = (IList)Activator.CreateInstance(type);
                foreach (var item in items)
                    result.Add(Bind(item, type.GetGenericArguments()[0]));
                return result;
            }

            if (!(value is Dictionary<string, object> fields))
                throw new FormatException("Expected JSON object.");
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<, >) && type.GetGenericArguments()[0] == typeof(string))
            {
                var dictionary = (IDictionary)Activator.CreateInstance(type);
                Type item = type.GetGenericArguments()[1];
                foreach (var field in fields)
                    dictionary.Add(field.Key, Bind(field.Value, item));
                return dictionary;
            }

            object instance = Activator.CreateInstance(type, true);
            foreach (var property in Properties(type))
            {
                if (fields.TryGetValue(Name(property), out var raw))
                    property.SetValue(instance, Bind(raw, property.PropertyType));
                else
                {
                    if (property.GetCustomAttribute<DataMemberAttribute>()?.IsRequired == true)
                        throw new FormatException("Missing JSON field.");
                    property.SetValue(instance, property.PropertyType.IsValueType ? Activator.CreateInstance(property.PropertyType) : null);
                }
            }

            return instance;
        }

        private static void Emit(StringBuilder output, object value, int depth, ref int nodes)
        {
            if (depth > 64 || ++nodes > 1000000)
                throw new FormatException("JSON complexity exceeds the limit.");
            if (value == null)
            {
                output.Append("null");
                return;
            }

            if (value is string text)
            {
                Quote(output, text);
                return;
            }

            if (value is bool boolean)
            {
                output.Append(boolean ? "true" : "false");
                return;
            }

            if (value is byte || value is sbyte || value is short || value is ushort || value is int || value is uint || value is long || value is ulong || value is float || value is double || value is decimal)
            {
                string number = Convert.ToString(value, CultureInfo.InvariantCulture);
                if (number == "NaN" || number == "Infinity" || number == "-Infinity")
                    throw new FormatException("JSON numbers must be finite.");
                output.Append(number);
                return;
            }

            bool first = true;
            if (value is IDictionary dictionary)
            {
                output.Append('{');
                foreach (DictionaryEntry field in dictionary)
                {
                    if (!(field.Key is string key))
                        throw new FormatException("JSON dictionary keys must be strings.");
                    if (!first)
                        output.Append(',');
                    first = false;
                    Quote(output, key);
                    output.Append(':');
                    Emit(output, field.Value, depth + 1, ref nodes);
                }

                output.Append('}');
                return;
            }

            if (value is IEnumerable sequence)
            {
                output.Append('[');
                foreach (object item in sequence)
                {
                    if (!first)
                        output.Append(',');
                    first = false;
                    Emit(output, item, depth + 1, ref nodes);
                }

                output.Append(']');
                return;
            }

            output.Append('{');
            foreach (var property in Properties(value.GetType()))
            {
                if (!first)
                    output.Append(',');
                first = false;
                Quote(output, Name(property));
                output.Append(':');
                Emit(output, property.GetValue(value), depth + 1, ref nodes);
            }

            output.Append('}');
        }

        private static void Quote(StringBuilder output, string value)
        {
            output.Append('"');
            foreach (char c in value)
            {
                if (c == '"')
                    output.Append("\\\"");
                else if (c == '\\')
                    output.Append("\\\\");
                else if (c < 32 || char.IsSurrogate(c))
                    output.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                else
                    output.Append(c);
            }

            output.Append('"');
        }

        private sealed class Number
        {
            internal readonly string Text;
            internal Number(string text)
            {
                Text = text;
            }
        }

        private sealed class Reader
        {
            private readonly string text;
            private int position, nodes;
            internal Reader(string text)
            {
                this.text = text;
            }

            private void Space()
            {
                while (position < text.Length && (text[position] == ' ' || text[position] == '\t' || text[position] == '\r' || text[position] == '\n'))
                    position++;
            }

            internal void End()
            {
                Space();
                if (position != text.Length)
                    throw new FormatException("Trailing JSON content.");
            }

            internal object Value(int depth)
            {
                if (depth > 64 || ++nodes > 1000000)
                    throw new FormatException("JSON complexity exceeds the limit.");
                Space();
                if (position >= text.Length)
                    throw new FormatException("Unexpected end of JSON.");
                char c = text[position];
                if (c == '"')
                    return String();
                if (c == '{')
                {
                    position++;
                    Space();
                    var result = new Dictionary<string, object>(StringComparer.Ordinal);
                    if (Consume('}'))
                        return result;
                    do
                    {
                        Space();
                        if (position >= text.Length || text[position] != '"')
                            throw new FormatException("Expected JSON property name.");
                        string key = String();
                        Space();
                        Require(':');
                        if (result.ContainsKey(key))
                            throw new FormatException("Duplicate JSON property.");
                        result.Add(key, Value(depth + 1));
                        Space();
                        if (Consume('}'))
                            return result;
                        Require(',');
                    }
                    while (true);
                }

                if (c == '[')
                {
                    position++;
                    Space();
                    var result = new List<object>();
                    if (Consume(']'))
                        return result;
                    do
                    {
                        result.Add(Value(depth + 1));
                        Space();
                        if (Consume(']'))
                            return result;
                        Require(',');
                    }
                    while (true);
                }

                if (Literal("true"))
                    return true;
                if (Literal("false"))
                    return false;
                if (Literal("null"))
                    return null;
                return Numeric();
            }

            private bool Consume(char c)
            {
                if (position < text.Length && text[position] == c)
                {
                    position++;
                    return true;
                }

                return false;
            }

            private void Require(char c)
            {
                if (!Consume(c))
                    throw new FormatException("Invalid JSON punctuation.");
            }

            private bool Literal(string value)
            {
                if (position + value.Length > text.Length || string.CompareOrdinal(text, position, value, 0, value.Length) != 0)
                    return false;
                position += value.Length;
                return true;
            }

            private object Numeric()
            {
                int start = position;
                Consume('-');
                if (!Consume('0'))
                    Digits();
                if (Consume('.'))
                    Digits();
                if (Consume('e') || Consume('E'))
                {
                    if (!Consume('+'))
                        Consume('-');
                    Digits();
                }

                if (start == position)
                    throw new FormatException("Invalid JSON value.");
                return new Number(text.Substring(start, position - start));
            }

            private void Digits()
            {
                int start = position;
                while (position < text.Length && text[position] >= '0' && text[position] <= '9')
                    position++;
                if (start == position)
                    throw new FormatException("Invalid JSON number.");
            }

            private string String()
            {
                Require('"');
                var result = new StringBuilder();
                while (position < text.Length)
                {
                    char c = text[position++];
                    if (c == '"')
                        return result.ToString();
                    if (c < 32)
                        throw new FormatException("Unescaped JSON control character.");
                    if (c != '\\')
                    {
                        result.Append(c);
                        continue;
                    }

                    if (position >= text.Length)
                        throw new FormatException("Incomplete JSON escape.");
                    c = text[position++];
                    switch (c)
                    {
                        case '"':
                        case '\\':
                        case '/':
                            result.Append(c);
                            break;
                        case 'b':
                            result.Append('\b');
                            break;
                        case 'f':
                            result.Append('\f');
                            break;
                        case 'n':
                            result.Append('\n');
                            break;
                        case 'r':
                            result.Append('\r');
                            break;
                        case 't':
                            result.Append('\t');
                            break;
                        case 'u':
                            if (position + 4 > text.Length || !ushort.TryParse(text.Substring(position, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out ushort code))
                                throw new FormatException("Invalid JSON Unicode escape.");
                            result.Append((char)code);
                            position += 4;
                            break;
                        default:
                            throw new FormatException("Invalid JSON escape.");
                    }
                }

                throw new FormatException("Unterminated JSON string.");
            }
        }
    }
}
