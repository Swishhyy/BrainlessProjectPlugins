using System;
using System.Globalization;
using System.Reflection;

namespace Brainless.Internal
{
    [AttributeUsage(AttributeTargets.Property)]
    internal sealed class TuneAttribute : Attribute
    {
        internal readonly double Min, Max;
        internal TuneAttribute(double min, double max)
        {
            Min = min;
            Max = max;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class TuningSectionAttribute : Attribute
    {
    }

    internal static class Tuning
    {
        internal static void Validate(object value, string path)
        {
            if (value == null)
                throw new InvalidOperationException(path + " cannot be null.");
            foreach (var property in value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                object current = property.GetValue(value);
                var bound = property.GetCustomAttribute<TuneAttribute>();
                if (bound != null)
                {
                    double number = Convert.ToDouble(current, CultureInfo.InvariantCulture);
                    if (double.IsNaN(number) || double.IsInfinity(number) || number < bound.Min || number > bound.Max)
                        throw new InvalidOperationException(path + "." + property.Name + " must be between " + bound.Min + " and " + bound.Max + ".");
                }
                else if ((property.PropertyType.Namespace == value.GetType().Namespace || property.PropertyType.IsDefined(typeof(TuningSectionAttribute), false)) && property.PropertyType.IsClass && property.PropertyType != typeof(string))
                    Validate(current, path + "." + property.Name);
            }
        }
    }
}
