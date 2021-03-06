﻿using System;
using System.Collections.Generic;
using NClap.ConsoleInput;
using NClap.Metadata;
using NClap.Parser;
using NClap.Types;
using NClap.Utilities;
using NClap.Repl;

namespace NClap.Repl.TestApp
{
    enum VerbType
    {
        Verb1,
        Verb2
    }

    [Verb]
    class SetPrompt
    {
        [PositionalArgument(ArgumentFlags.Required)] public string Prompt { get; set; }
    }

    [Verb(HelpText = "Displays verb help")]
    class Here : IVerb
    {
        [NamedArgument(ArgumentFlags.AtMostOnce)] public int Hello { get; set; }

        [NamedArgument(ArgumentFlags.AtMostOnce)] public bool SomeBool { get; set; }

        [NamedArgument(ArgumentFlags.AtMostOnce)] public VerbType SomeEnum { get; set; }

        [NamedArgument(ArgumentFlags.AtMostOnce)] public Tuple<int, VerbType, string> SomeTuple { get; set; }

        [NamedArgument(ArgumentFlags.AtMostOnce)] public KeyValuePair<int, VerbType> SomePair { get; set; }

        [NamedArgument(ArgumentFlags.AtMostOnce)] public KeyValuePair<int, List<VerbType>> SomeListPair { get; set; }

        [NamedArgument(ArgumentFlags.AtMostOnce)] public Uri SomeUri { get; set; }

        [PositionalArgument(ArgumentFlags.Required)] public FileSystemPath Path { get; set; }

        public void Execute(ILoop loop, object o)
        {
        }
    }

    class SomeArgCompleter : IStringCompleter
    {
        public IEnumerable<string> GetCompletions(ArgumentCompletionContext context, string valueToComplete) =>
            new[] { "xyzzy", "fizzy" };
    }

    [Verb(HelpText = "Simple here command")]
    class HereToo : IVerb
    {
        [PositionalArgument(ArgumentFlags.Required, Completer = typeof(SomeArgCompleter))] public string SomeArg { get; set; }

        public void Execute(ILoop loop, object o)
        {
        }
    }

    class Program
    {
        private static int Main(string[] args)
        {
            var programArgs = new ProgramArguments();

            if (!CommandLineParser.ParseWithUsage(args, programArgs))
            {
                return -1;
            }

            RunInteractively();

            return 0;
        }

        private static void RunInteractively()
        {
            Console.WriteLine("Entering loop.");

            var options = new LoopOptions
            {
                EndOfLineCommentCharacter = '#'
            };

            var keyBindingSet = ConsoleKeyBindingSet.CreateDefaultSet();
            keyBindingSet.Bind('c', ConsoleModifiers.Control, ConsoleInputOperation.Abort);

            var parameters = new LoopInputOutputParameters
            {
                Prompt = new ColoredString("Loop> ", ConsoleColor.Cyan),
                KeyBindingSet = keyBindingSet
            };

            Loop.Execute(VerbResolver.ResolveAll(), parameters, options);

            Console.WriteLine("Exited loop.");
        }
    }
}
