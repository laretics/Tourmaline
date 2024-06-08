using System;
using System.Linq;

namespace TOURMALINE.Common
{
    /// <summary>
    /// Localization attribute for decorating enums
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class GetStringAttribute : Attribute
    {
        public string Name { get; protected set; }
        public GetStringAttribute(string name) { Name = name; }

        public static string GetPrettyName(Enum value)
        {
            var type = value.GetType();
            return type.GetField(Enum.GetName(type, value))
                .GetCustomAttributes(false)
                .OfType<GetStringAttribute>()
                .SingleOrDefault()
                .Name;
        }
    }

    /// <summary>
    /// Localization attribute for decorating enums
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class GetParticularStringAttribute : GetStringAttribute
    {
        public string Context { get; protected set; }
        public GetParticularStringAttribute(string context, string name) : base(name) { Context = context; }
    }
}

