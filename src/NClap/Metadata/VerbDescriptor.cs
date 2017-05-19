using System;

namespace NClap.Metadata
{
    /// <summary>
    /// Attribute class used to denote verbs.
    /// </summary>
    public class VerbDescriptor
    {
        /// <summary>
        /// Default, parameterless constructor.
        /// </summary>
        public VerbDescriptor()
        {
        }

        /// <summary>
        /// Verb name
        /// </summary>
        public string Name { get;set; }

        /// <summary>
        /// Verb key
        /// </summary>
        public string Key => Name.ToLowerInvariant();

        /// <summary>
        /// Type that "implements" this verb
        /// </summary>
        public virtual Type ImplementingType { get; set; }

        /// <summary>
        /// The help text associated with this verb.
        /// </summary>
        public string HelpText { get; set; }

        public IVerb Instance { get; set; }
    }
}
