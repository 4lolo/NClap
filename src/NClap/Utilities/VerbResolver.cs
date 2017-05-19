using NClap.Metadata;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace NClap.Utilities
{
    public static class VerbResolver
    {
        public static VerbDescriptor ExitVerbDescriptor => new VerbDescriptor
        {
            Name = "Exit",
            HelpText = "Exits application",
            ImplementingType = typeof(ExitVerb),
            Instance = new ExitVerb()
        };

        public static VerbDescriptor HelpVerbDescriptor => new VerbDescriptor
        {
            Name = "Help",
            HelpText = "Displays help",
            ImplementingType = typeof(HelpVerb),
            Instance = new HelpVerb()
        };

        public static IEnumerable<VerbDescriptor> ResolveFromAssemblies(params Assembly[] assemblies)
        {
            foreach (Assembly assembly in assemblies)
            {
                Type[] types = assembly.GetTypes();
                foreach (Type type in types)
                {
                    VerbAttribute attribute = type.GetCustomAttribute<VerbAttribute>();
                    if (attribute != null)
                    {
                        yield return new VerbDescriptor
                        {
                            Name = attribute.Name ?? type.Name,
                            HelpText = attribute.HelpText,
                            ImplementingType = type
                        };
                    }
                }
            }

            yield return HelpVerbDescriptor;

            yield return ExitVerbDescriptor;
        }

        public static IEnumerable<VerbDescriptor> ResolveAll()
        {
            return ResolveFromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
        }
    }
}
