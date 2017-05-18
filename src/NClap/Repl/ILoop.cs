using System;
using System.Collections.Generic;
using NClap.ConsoleInput;
using NClap.Metadata;

namespace NClap.Repl
{
    public interface ILoop
    {
        ILoopClient Client { get; }
        IConsoleReader ConsoleReader { get; }
        char? EndOfLineCommentCharacter { get; }
        Func<Type, IVerb> Resolver { get; }
        IEnumerable<VerbDescriptor> Verbs { get; }
    }
}