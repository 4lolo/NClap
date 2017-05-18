using System;

namespace NClap.Metadata
{
    /// <summary>
    /// Attribute for annotating help verbs.
    /// </summary>
    public sealed class HelpVerbDescriptor : VerbDescriptor
    {
        /// <summary>
        /// Gets the type that "implements" this verb.
        /// </summary>
        /// attribute.</param>
        /// <returns>The type.</returns>
        public override Type ImplementingType
        {
            get { return typeof(HelpVerb); }
            set {}
        }
    }
}
