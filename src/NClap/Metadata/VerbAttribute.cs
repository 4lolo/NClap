using System;

namespace NClap.Metadata
{
    [AttributeUsage(AttributeTargets.Class)]
    public class VerbAttribute : Attribute
    {
        public string Name { get;set; }

        public string HelpText { get; set; }
    }
}
