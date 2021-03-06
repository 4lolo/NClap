﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using NClap.ConsoleInput;
using NClap.Metadata;
using NClap.Parser;
using Common.Logging;

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
        private readonly ILog _log;

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
            Resolve = options?.Resolve ?? DefaultResolve;
            Release = options?.Release ?? DefaultRelease;

            _log = options?.Log;
            _context = context;

            _verbMap = verbs.ToDictionary(v => v.Name.ToLowerInvariant(), v => v);
        }

        public bool Exit { get; set; }

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
        public Func<Type, IVerb> Resolve { get; set; }

        /// <summary>
        /// Function that releases verb instance
        /// </summary>
        public Action<IVerb> Release { get; set; }

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
            var instance = attrib.Instance;
            if (implementingType == null)
            {
                return emptyCompletions();
            }

            var options = new CommandLineParserOptions { Context = _context };
            Func<object> resolve = instance == null ? 
                new Func<object>(() => Resolve(implementingType)) : 
                new Func<object>(() => instance);
            Action<object> release = instance == null ? 
                new Action<object>(v => Release((IVerb)v)) : 
                new Action<object>(v => {});

            return CommandLineParser.GetCompletions(
                implementingType,
                tokenList.Skip(1),
                indexOfTokenToComplete - 1,
                options,
                resolve,
                release);
        }

        private bool ExecuteOnce()
        {
            Client.DisplayPrompt();

            var args = ReadInput();

            return ExecuteOnce(args);
        }

        /// <summary>
        /// Executes one command.
        /// </summary>
        public bool ExecuteOnce(string[] args)
        {
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
            var instance = attrib.Instance;
            Func<IVerb> resolve = instance == null ? 
                new Func<IVerb>(() => Resolve(implementingType)) : 
                new Func<IVerb>(() => instance);
            Action<IVerb> release = instance == null ? 
                new Action<IVerb>(v => Release(v)) : 
                new Action<IVerb>(v => {});

            if (implementingType != null)
            {
                IVerb verb = null;
                try
                {
                    verb = resolve();

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
                catch (Exception ex)
                {
                    string message = string.Format(Strings.VerbExecutionError, verbType, ex.Message);
                    Client.OnError(message);
                    this._log?.Error(message, ex);
                }
                finally
                {
                    if (verb != null)
                    {
                        release(verb);
                    }
                }
            }
            else
            {
                Client.OnError(Strings.NoAccessibleParameterlessConstructor);
            }

            return !this.Exit;
        }

        private IVerb DefaultResolve(Type implementingType)
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

        private void DefaultRelease(IVerb verb)
        {
            // do nothing
        }
    }
}
