using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using NClap.ConsoleInput;
using NClap.Metadata;
using NClap.Parser;

namespace NClap.Repl
{
    /// <summary>
    /// An interactive REPL loop.
    /// </summary>
    /// <typeparam name="TContext">Type of the context object passed to
    /// all verbs.</typeparam>
    public class Loop<TContext> : ILoop
    {
        private readonly TContext _context;
        private readonly IReadOnlyDictionary<string, VerbDescriptor> _verbMap;

        /// <summary>
        /// Constructor that requires an explicit implementation of
        /// <see cref="ILoopClient"/>.
        /// </summary>
        /// <param name="verbs">Verbs</param>
        /// <param name="loopClient">The client to use.</param>
        /// <param name="options">Options for loop.</param>
        /// <param name="context">Caller-provided context object.</param>
        public Loop(IEnumerable<VerbDescriptor> verbs, ILoopClient loopClient, LoopOptions options, TContext context) : this(verbs, options, context)
        {
            if (loopClient == null)
            {
                throw new ArgumentNullException(nameof(loopClient));
            }

            Client = loopClient;
        }

        /// <summary>
        /// Constructor that creates a loop with a default client.
        /// </summary>
        /// <param name="verbs">Verbs</param>
        /// <param name="parameters">Optionally provides parameters controlling
        /// the loop's input and output behaviors; if not provided, default
        /// parameters are used.</param>
        /// <param name="options">Options for loop.</param>
        /// <param name="context">Caller-provided context object.</param>
        public Loop(IEnumerable<VerbDescriptor> verbs, LoopInputOutputParameters parameters, LoopOptions options, TContext context) : this(verbs, options, context)
        {
            var consoleInput = parameters?.ConsoleInput ?? BasicConsoleInputAndOutput.Default;
            var consoleOutput = parameters?.ConsoleOutput ?? BasicConsoleInputAndOutput.Default;
            var keyBindingSet = parameters?.KeyBindingSet ?? ConsoleKeyBindingSet.Default;

            var lineInput = parameters?.LineInput ?? new ConsoleLineInput(
                consoleOutput,
                new ConsoleInputBuffer(),
                new ConsoleHistory(),
                GenerateCompletions);

            lineInput.Prompt = parameters?.Prompt ?? Strings.DefaultPrompt;

            ConsoleReader = new ConsoleReader(lineInput, consoleInput, consoleOutput, keyBindingSet);

            var consoleClient = new ConsoleLoopClient(
                ConsoleReader,
                parameters?.ErrorWriter ?? Console.Error);

            consoleClient.Reader.CommentCharacter = options?.EndOfLineCommentCharacter;

            Client = consoleClient;
        }

        /// <summary>
        /// Shared private constructor used internally by all public
        /// constructors.
        /// </summary>
        /// <param name="verbs">Verbs</param>
        /// <param name="options">Options for loop.</param>
        /// <param name="context">Caller-provided context object.</param>
        private Loop(IEnumerable<VerbDescriptor> verbs, LoopOptions options, TContext context)
        {
            EndOfLineCommentCharacter = options?.EndOfLineCommentCharacter;
            Resolver = options?.Resolver ?? DefaultResolver;

            _context = context;

            _verbMap = verbs.ToDictionary(v => v.Name.ToLowerInvariant(), v => v);
        }

        /// <summary>
        /// The client associated with this loop.
        /// </summary>
        public ILoopClient Client { get; }

        /// <summary>
        /// The console reader used by this loop, or null if none is present.
        /// </summary>
        public IConsoleReader ConsoleReader { get; }

        /// <summary>
        /// The character that starts a comment.
        /// </summary>
        public char? EndOfLineCommentCharacter { get; set; }

        /// <summary>
        /// Function that resolves provided type instance
        /// </summary>
        public Func<Type, IVerb> Resolver { get; set; }

        /// <summary>
        /// Verbs list for help
        /// </summary>
        public IEnumerable<VerbDescriptor> Verbs => this._verbMap.Values.AsEnumerable();

        /// <summary>
        /// Executes the loop.
        /// </summary>
        public void Execute()
        {
            while (ExecuteOnce())
            {
            }
        }

        /// <summary>
        /// Generates possible string completions for an input line to the
        /// loop.
        /// </summary>
        /// <param name="tokens">The tokens presently in the input line.</param>
        /// <param name="indexOfTokenToComplete">The 0-based index of the token
        /// from the input line to be completed.</param>
        /// <returns>An enumeration of the possible completions for the
        /// indicated token.</returns>
        public IEnumerable<string> GenerateCompletions(IEnumerable<string> tokens, int indexOfTokenToComplete)
        {
            Func<IEnumerable<string>> emptyCompletions = Enumerable.Empty<string>;

            var tokenList = tokens.ToList();

            var tokenToComplete = string.Empty;
            if (indexOfTokenToComplete < tokenList.Count)
            {
                tokenToComplete = tokenList[indexOfTokenToComplete];
            }

            if (indexOfTokenToComplete == 0)
            {
                return _verbMap
                    .Where(command => command.Key.StartsWith(tokenToComplete, StringComparison.CurrentCultureIgnoreCase))
                    .OrderBy(command => command.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(command => command.Value.Name);
            }

            if (tokenList.Count < 1)
            {
                return emptyCompletions();
            }

            var verbToken = tokenList[0].ToLowerInvariant();
            
            VerbDescriptor attrib;
            if (!_verbMap.TryGetValue(verbToken, out attrib))
            {
                return emptyCompletions();
            }

            var implementingType = attrib.ImplementingType;
            if (implementingType == null)
            {
                return emptyCompletions();
            }

            Func<object> parsedObjectFactory = () => Resolver?.Invoke(implementingType);

            var options = new CommandLineParserOptions { Context = _context };
            return CommandLineParser.GetCompletions(
                implementingType,
                tokenList.Skip(1),
                indexOfTokenToComplete - 1,
                options,
                parsedObjectFactory);
        }

        private bool ExecuteOnce()
        {
            Client.DisplayPrompt();

            var args = ReadInput();

            if (args == null)
            {
                return false;
            }

            if (args.Length == 0)
            {
                return true;
            }

            string verbType = args[0].ToLowerInvariant();
            if (!_verbMap.ContainsKey(verbType))
            {
                Client.OnError(string.Format(CultureInfo.CurrentCulture, Strings.UnrecognizedVerb, args[0]));
                return true;
            }

            return Execute(verbType, args.Skip(1));
        }

        private string[] ReadInput()
        {
            var line = Client.ReadLine();

            // Return null if we're at the end of the input stream.
            if (line == null)
            {
                return null;
            }

            // Preprocess the line.
            line = Preprocess(line);

            try
            {
                // Parse the string into tokens.
                return CommandLineParser.Tokenize(line).Select(token => token.ToString()).ToArray();
            }
            catch (ArgumentException ex)
            {
                Client.OnError(string.Format(CultureInfo.CurrentCulture, Strings.ExceptionWasThrownParsingInputLine, ex));
                return new string[] { };
            }
        }

        /// <summary>
        /// Preprocesses a line of input, primarily to remove comments.
        /// </summary>
        /// <param name="input">The line of input to preprocess.</param>
        /// <returns>The preprocessed result.</returns>
        private string Preprocess(string input)
        {
            if (!EndOfLineCommentCharacter.HasValue)
            {
                return input;
            }

            var commentStartIndex = input.IndexOf(EndOfLineCommentCharacter.Value);
            if (commentStartIndex >= 0)
            {
                input = input.Substring(0, commentStartIndex);
            }

            return input;
        }

        private bool Execute(string verbType, IEnumerable<string> args)
        {
            VerbDescriptor attrib;
            if (!_verbMap.TryGetValue(verbType.ToLowerInvariant(), out attrib))
            {
                throw new NotSupportedException();
            }

            var implementingType = attrib.ImplementingType;
            if (implementingType != null)
            {
                var verb = Resolver?.Invoke(implementingType) as IVerb;
                if (verb == null)
                {
                    Client.OnError(string.Format(CultureInfo.CurrentCulture, Strings.ImplementingTypeNotIVerb, implementingType.FullName, typeof(IVerb).FullName));
                    return true;
                }

                var options = new CommandLineParserOptions
                {
                    Context = _context,
                    Reporter = error => Client.OnError(error.ToString().TrimEnd())
                };

                if (!CommandLineParser.Parse(args.ToList(), verb, options))
                {
                    Client.OnError(Strings.InvalidUsage);
                    return true;
                }

                verb.Execute(this, _context);
            }

            return !attrib.Exits;
        }

        private IVerb DefaultResolver(Type implementingType)
        {
            var constructor = implementingType.GetConstructor(Type.EmptyTypes);
            if (constructor == null)
            {
                Client.OnError(string.Format(CultureInfo.CurrentCulture, Strings.NoAccessibleParameterlessConstructor, implementingType.FullName));
                return null;
            }

            var verb = constructor.Invoke(null) as IVerb<TContext>;
            if (verb == null)
            {
                Client.OnError(string.Format(CultureInfo.CurrentCulture, Strings.ImplementingTypeNotIVerb, implementingType.FullName, typeof(IVerb).FullName));
                return null;
            }

            return (IVerb)verb;
        }
    }
}
