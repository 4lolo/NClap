using System;
using System.Collections.Generic;
using NClap.ConsoleInput;
using NClap.Metadata;

namespace NClap.Repl
{
    public interface ILoop
    {
        bool Exit { get; set; }
        ILoopClient Client { get; }
        IConsoleReader ConsoleReader { get; }
        char? EndOfLineCommentCharacter { get; }
        Func<Type, IVerb> Resolve { get; }
        IEnumerable<VerbDescriptor> Verbs { get; }
    }
}