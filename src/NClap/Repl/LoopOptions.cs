using NClap.Metadata;
using System;

namespace NClap.Repl
{
    /// <summary>
    /// Options for an interactive loop.
    /// </summary>
    public class LoopOptions
    {
        /// <summary>
        /// The character that starts a comment.
        /// </summary>
        public char? EndOfLineCommentCharacter { get; set; }

        /// <summary>
        /// Function that resolves provided type instance
        /// </summary>
        public Func<Type, IVerb> Resolver { get;set; }
    }
}
