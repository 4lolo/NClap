﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;

using NClap.ConsoleInput;
using NClap.Metadata;
using NClap.Utilities;

namespace NClap.Parser
{
    /// <summary>
    /// Command-line parser.
    /// </summary>
    public static class CommandLineParser
    {
        /// <summary>
        /// Retrieves the current console's width in characters.
        /// </summary>
        internal static Func<int> GetConsoleWidth { get; set; } =
            () => Console.WindowWidth;

        /// <summary>
        /// Default console width in characters.
        /// </summary>
        private const int DefaultConsoleWidth = 80;

        /// <summary>
        /// Default <see cref="ColoredErrorReporter" /> used by this class.
        /// </summary>
        public static ColoredErrorReporter DefaultReporter { get; } = BasicConsoleInputAndOutput.Default.Write;

        /// <summary>
        /// Parses command-line arguments into a reference-type object. Displays
        /// usage message to Console.Out if invalid arguments are encountered.
        /// Errors are output on Console.Error. Use ArgumentAttributes to
        /// control parsing behavior.
        /// </summary>
        /// <typeparam name="T">Type of the parsed arguments object.</typeparam>
        /// <param name="arguments">The actual arguments.</param>
        /// <param name="destination">The resulting parsed arguments.</param>
        /// <returns>True if no errors were detected.</returns>
        public static bool ParseWithUsage<T>(IList<string> arguments, T destination) where T : class =>
            ParseWithUsage(arguments, destination, (CommandLineParserOptions)null);

        /// <summary>
        /// Parses command-line arguments into a reference-type object. Displays
        /// usage message to Console.Out if invalid arguments are encountered.
        /// Errors are output on Console.Error. Use ArgumentAttributes to
        /// control parsing behavior.
        /// </summary>
        /// <typeparam name="T">Type of the parsed arguments object.</typeparam>
        /// <param name="arguments">The actual arguments.</param>
        /// <param name="destination">The resulting parsed arguments.</param>
        /// <param name="reporter">The destination for parse errors.</param>
        /// <returns>True if no errors were detected.</returns>
        public static bool ParseWithUsage<T>(IList<string> arguments, T destination, ErrorReporter reporter) where T : class =>
            ParseWithUsage(arguments, destination, new CommandLineParserOptions { Reporter = s => reporter(s) });

        /// <summary>
        /// Parses command-line arguments into a reference-type object.
        /// Displays usage message if invalid arguments are encountered.
        /// </summary>
        /// <typeparam name="T">Type of the parsed arguments object.</typeparam>
        /// <param name="arguments">The actual arguments.</param>
        /// <param name="destination">The resulting parsed arguments.</param>
        /// <param name="options">Optionally provides additional options
        /// controlling how parsing proceeds.</param>
        /// <returns>True if no errors were detected.</returns>
        public static bool ParseWithUsage<T>(IList<string> arguments, T destination, CommandLineParserOptions options) where T : class =>
            ParseWithUsage(arguments, destination, options, UsageInfoOptions.Default);

        /// <summary>
        /// Parses command-line arguments into a reference-type object.
        /// Displays usage message if invalid arguments are encountered.
        /// </summary>
        /// <typeparam name="T">Type of the parsed arguments object.</typeparam>
        /// <param name="arguments">The actual arguments.</param>
        /// <param name="destination">The resulting parsed arguments.</param>
        /// <param name="options">Optionally provides additional options
        /// controlling how parsing proceeds.</param>
        /// <param name="usageInfoOptions">Options for how to display usage
        /// information, in case it's presented.</param>
        /// <returns>True if no errors were detected.</returns>
        public static bool ParseWithUsage<T>(IList<string> arguments, T destination, CommandLineParserOptions options, UsageInfoOptions usageInfoOptions) where T : class
        {
            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (options == null)
            {
                options = new CommandLineParserOptions { Reporter = DefaultReporter };
            }

            Contract.Requires(Contract.ForAll(arguments, arg => arg != null));

            // Check if the object inherits from HelpArgumentsBase.
            var helpDestination = destination as HelpArgumentsBase;

            var abridgedOptions = usageInfoOptions;
            if (helpDestination != null)
            {
                abridgedOptions &= ~(UsageInfoOptions.IncludeOptionalParameterDescriptions | UsageInfoOptions.IncludeDescription);
                abridgedOptions |= UsageInfoOptions.IncludeRemarks;
            }

            // Parse!
            if (!Parse(arguments, destination, options))
            {
                var optionsForParseError = usageInfoOptions;
                if (!helpDestination?.Help ?? false)
                {
                    optionsForParseError = abridgedOptions;
                }

                // An error was encountered in arguments. Display the usage
                // message.
                options.Reporter?.Invoke(Environment.NewLine);
                options.Reporter?.Invoke(GetUsageInfo(destination.GetType(), destination, optionsForParseError));

                return false;
            }

            // We parsed the arguments, but check if we were requested to
            // display the usage help message anyway.
            if ((helpDestination != null) && helpDestination.Help)
            {
                options.Reporter?.Invoke(GetUsageInfo(destination.GetType(), destination, usageInfoOptions));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Parses command-line arguments into a value-type object. Displays
        /// usage message to Console.Out if invalid arguments are encountered.
        /// Errors are output on Console.Error. Use ArgumentAttributes to
        /// control parsing behavior.
        /// </summary>
        /// <typeparam name="T">Type of the parsed arguments object.</typeparam>
        /// <param name="arguments">The actual arguments.</param>
        /// <param name="destination">The resulting parsed arguments.</param>
        /// <returns>True if no errors were detected.</returns>
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "1#", Justification = "Required for structs")]
        public static bool ParseWithUsage<T>(IList<string> arguments, ref T destination) where T : struct =>
            ParseWithUsage(arguments, ref destination, new CommandLineParserOptions { Reporter = DefaultReporter });

        /// <summary>
        /// Parses command-line arguments into a value-type object.
        /// Displays usage message if invalid arguments are encountered.
        /// </summary>
        /// <typeparam name="T">Type of the parsed arguments object.</typeparam>
        /// <param name="arguments">The actual arguments.</param>
        /// <param name="destination">The resulting parsed arguments.</param>
        /// <param name="options">Optionally provides additional options
        /// controlling how parsing proceeds.</param>
        /// <returns>True if no errors were detected.</returns>
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "1#", Justification = "Required for structs")]
        public static bool ParseWithUsage<T>(IList<string> arguments, ref T destination, CommandLineParserOptions options) where T : struct =>
            ParseWithUsage(arguments, ref destination, options, UsageInfoOptions.Default);

        /// <summary>
        /// Parses command-line arguments into a value-type object.
        /// Displays usage message if invalid arguments are encountered.
        /// </summary>
        /// <typeparam name="T">Type of the parsed arguments object.</typeparam>
        /// <param name="arguments">The actual arguments.</param>
        /// <param name="destination">The resulting parsed arguments.</param>
        /// <param name="options">Optionally provides additional options
        /// controlling how parsing proceeds.</param>
        /// <param name="usageInfoOptions">Options for how to display usage
        /// information, in case it's presented.</param>
        /// <returns>True if no errors were detected.</returns>
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "1#", Justification = "Required for structs")]
        public static bool ParseWithUsage<T>(IList<string> arguments, ref T destination, CommandLineParserOptions options, UsageInfoOptions usageInfoOptions) where T : struct
        {
            var boxedDestination = (object)destination;
            if (!ParseWithUsage(arguments, boxedDestination, options, usageInfoOptions))
            {
                return false;
            }

            destination = (T)boxedDestination;
            return true;
        }

        /// <summary>
        /// Parses command-line arguments into a reference-type object. Errors
        /// are output on Console.Error. Use ArgumentAttributes to control
        /// parsing behavior.
        /// </summary>
        /// <typeparam name="T">Type of the parsed arguments object.</typeparam>
        /// <param name="arguments">The actual arguments.</param>
        /// <param name="destination">The resulting parsed arguments.</param>
        /// <returns>True if no errors were detected.</returns>
        public static bool Parse<T>(IList<string> arguments, T destination) where T : class =>
            Parse(arguments, destination, new CommandLineParserOptions { Reporter = s => Console.Error.WriteLine(s) });

        /// <summary>
        /// Parses command-line arguments into a reference-type object. Use
        /// ArgumentAttributes to control parsing behavior.
        /// </summary>
        /// <typeparam name="T">Type of the parsed arguments object.</typeparam>
        /// <param name="arguments">The actual arguments.</param>
        /// <param name="destination">The resulting parsed arguments.</param>
        /// <param name="options">Optionally provides additional options
        /// controlling how parsing proceeds.</param>
        /// <returns>True if no errors were detected.</returns>
        public static bool Parse<T>(IList<string> arguments, T destination, CommandLineParserOptions options) where T : class
        {
            Contract.Requires(arguments != null);
            Contract.Requires(Contract.ForAll(arguments, arg => arg != null));
            Contract.Requires(destination != null, "destination cannot be null");

            var parser = new CommandLineParserEngine(destination.GetType(), destination, options);
            return parser.Parse(arguments, destination);
        }

        /// <summary>
        /// Parses command-line arguments into a value-type object. Errors are
        /// output on Console.Error. Use ArgumentAttributes to control parsing
        /// behavior.
        /// </summary>
        /// <typeparam name="T">Type of the parsed arguments object.</typeparam>
        /// <param name="arguments">The actual arguments.</param>
        /// <param name="destination">The resulting parsed arguments.</param>
        /// <returns>True if no errors were detected.</returns>
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "1#", Justification = "Required for structs")]
        public static bool Parse<T>(IList<string> arguments, ref T destination) where T : struct =>
            Parse(arguments, ref destination, null);

        /// <summary>
        /// Parses command-line arguments into a value-type object. Use
        /// ArgumentAttributes to control parsing behavior.
        /// </summary>
        /// <typeparam name="T">Type of the parsed arguments object.</typeparam>
        /// <param name="arguments">The actual arguments.</param>
        /// <param name="destination">The resulting parsed arguments.</param>
        /// <param name="options">Optionally provides additional options
        /// controlling how parsing proceeds.</param>
        /// <returns>True if no errors were detected.</returns>
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "1#", Justification = "Required for structs")]
        public static bool Parse<T>(IList<string> arguments, ref T destination, CommandLineParserOptions options) where T : struct
        {
            var boxedDestination = (object)destination;
            if (!Parse(arguments, boxedDestination, options))
            {
                return false;
            }

            destination = (T)boxedDestination;
            return true;
        }

        /// <summary>
        /// Formats a parsed set of arguments back into tokenized string form.
        /// </summary>
        /// <typeparam name="T">Type of the parsed arguments object.</typeparam>
        /// <param name="value">The parsed argument set.</param>
        /// <returns>The tokenized string.</returns>
        public static IEnumerable<string> Format<T>(T value) =>
            new CommandLineParserEngine(value.GetType()).Format(value);

        /// <summary>
        /// Returns a usage string for command line argument parsing. Use
        /// ArgumentAttributes to control parsing behavior. Formats the output
        /// to the width of the current console window.
        /// </summary>
        /// <param name="type">Type of the parsed arguments object to get usage
        /// info for.</param>
        /// <returns>Printable string containing a user friendly description of
        /// command line arguments.</returns>
        public static ColoredMultistring GetUsageInfo(Type type) =>
            GetUsageInfo(type, type.GetDefaultValue());

        /// <summary>
        /// Returns a usage string for command line argument parsing. Use
        /// ArgumentAttributes to control parsing behavior. Formats the output
        /// to the width of the current console window.
        /// </summary>
        /// <param name="type">Type of the parsed arguments object to get usage
        /// info for.</param>
        /// <param name="options">Options for generating usage info.</param>
        /// <returns>Printable string containing a user friendly description of
        /// command line arguments.</returns>
        public static ColoredMultistring GetUsageInfo(Type type, UsageInfoOptions options) =>
            GetUsageInfo(type, type.GetDefaultValue(), null, null, options);

        /// <summary>
        /// Returns a usage string for command line argument parsing. Use
        /// ArgumentAttributes to control parsing behavior. Formats the output
        /// to the width of the current console window.
        /// </summary>
        /// <param name="type">Type of the parsed arguments object.</param>
        /// <param name="defaultValues">Optionally provides an object with
        /// default values.</param>
        /// <returns>Printable string containing a user friendly description of
        /// command line arguments.</returns>
        public static ColoredMultistring GetUsageInfo(Type type, object defaultValues) =>
            GetUsageInfo(type, defaultValues, null);

        /// <summary>
        /// Returns a usage string for command line argument parsing. Use
        /// ArgumentAttributes to control parsing behavior. Formats the output
        /// to the width of the current console window.
        /// </summary>
        /// <param name="type">Type of the parsed arguments object.</param>
        /// <param name="defaultValues">Optionally provides an object with
        /// default values.</param>
        /// <param name="options">Options for generating usage info.</param>
        /// <returns>Printable string containing a user friendly description of
        /// command line arguments.</returns>
        public static ColoredMultistring GetUsageInfo(Type type, object defaultValues, UsageInfoOptions options) =>
            GetUsageInfo(type, defaultValues, null, null, options);

        /// <summary>
        /// Returns a Usage string for command line argument parsing. Use
        /// ArgumentAttributes to control parsing behavior.
        /// </summary>
        /// <param name="type">Type of the parsed arguments object.</param>
        /// <param name="defaultValues">Optionally provides an object with
        /// default values.</param>
        /// <param name="columns">The number of columns to format the output to.
        /// </param>
        /// <returns>Printable string containing a user friendly description of
        /// command-line arguments.</returns>
        public static ColoredMultistring GetUsageInfo(Type type, object defaultValues, int? columns) =>
            GetUsageInfo(type, defaultValues, columns, null, UsageInfoOptions.Default);

        /// <summary>
        /// Returns a Usage string for command line argument parsing. Use
        /// ArgumentAttributes to control parsing behavior.
        /// </summary>
        /// <param name="type">Type of the parsed arguments object.</param>
        /// <param name="defaultValues">Optionally provides an object with
        /// default values.</param>
        /// <param name="columns">The number of columns to format the output to.
        /// </param>
        /// <param name="commandName">Command name to display in the usage
        /// information.</param>
        /// <param name="options">Options for generating usage info.</param>
        /// <returns>Printable string containing a user friendly description of
        /// command line arguments.</returns>
        public static ColoredMultistring GetUsageInfo(
            Type type,
            object defaultValues,
            int? columns,
            string commandName,
            UsageInfoOptions options)
        {
            if (!columns.HasValue)
            {
                try
                {
                    columns = GetConsoleWidth();
                }
                catch (IOException)
                {
                    // If can't determine the console's width, then default it.
                    columns = DefaultConsoleWidth;
                }
            }

            var parser = new CommandLineParserEngine(type, defaultValues, null);
            return parser.GetUsageInfo(columns.Value, commandName, options);
        }

        /// <summary>
        /// Generates a logo string for the application's entry assembly, or
        /// the assembly containing this method if no entry assembly could
        /// be found.
        /// </summary>
        /// <returns>The logo string.</returns>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "It's not appropriate")]
        public static string GetLogo() => AssemblyUtilities.GetLogo();

        /// <summary>
        /// Generate possible completions for the specified set of command-line
        /// tokens.
        /// </summary>
        /// <param name="type">Type of the parsed arguments object.</param>
        /// <param name="tokens">The tokens.</param>
        /// <param name="indexOfTokenToComplete">Index of the token to complete.
        /// </param>
        /// <returns>The candidate completions for the specified token.
        /// </returns>
        public static IEnumerable<string> GetCompletions(Type type, IEnumerable<string> tokens, int indexOfTokenToComplete) =>
            GetCompletions(type, tokens, indexOfTokenToComplete, null /* options */);

        /// <summary>
        /// Generate possible completions for the specified set of command-line
        /// tokens.
        /// </summary>
        /// <param name="type">Type of the parsed arguments object.</param>
        /// <param name="tokens">The tokens.</param>
        /// <param name="indexOfTokenToComplete">Index of the token to complete.
        /// </param>
        /// <param name="options">Parsing options.</param>
        /// <returns>The candidate completions for the specified token.
        /// </returns>
        public static IEnumerable<string> GetCompletions(Type type, IEnumerable<string> tokens, int indexOfTokenToComplete, CommandLineParserOptions options) =>
            GetCompletions(type, tokens, indexOfTokenToComplete, options, null /* object factory */, null);

        /// <summary>
        /// Generate possible completions for the specified set of command-line
        /// tokens.
        /// </summary>
        /// <param name="type">Type of the parsed arguments object.</param>
        /// <param name="tokens">The tokens.</param>
        /// <param name="indexOfTokenToComplete">Index of the token to complete.
        /// </param>
        /// <param name="options">Parsing options.</param>
        /// <param name="destObjectResolve">If non-null, provides a factory
        /// function that can be used to create an object suitable to being
        /// filled out by this parser instance.</param>
        /// <returns>The candidate completions for the specified token.
        /// </returns>
        public static IEnumerable<string> GetCompletions(Type type, IEnumerable<string> tokens, int indexOfTokenToComplete, CommandLineParserOptions options, Func<object> destObjectResolve, Action<object> destObjectRelease)
        {
            var engine = new CommandLineParserEngine(
                type,
                null /* default values */,
                options);

            return engine.GetCompletions(tokens, indexOfTokenToComplete, destObjectResolve, destObjectRelease);
        }

        /// <summary>
        /// Tokenizes the provided input text line, observing quotes.
        /// </summary>
        /// <param name="line">Input line to parse.</param>
        /// <returns>Enumeration of tokens.</returns>
        internal static IEnumerable<Token> Tokenize(string line) =>
            Tokenize(line, CommandLineTokenizerOptions.None);

        /// <summary>
        /// Tokenizes the provided input text line, observing quotes.
        /// </summary>
        /// <param name="line">Input line to parse.</param>
        /// <param name="options">Options for tokenizing.</param>
        /// <returns>Enumeration of tokens.</returns>
        internal static IEnumerable<Token> Tokenize(string line, CommandLineTokenizerOptions options) =>
            CommandLineParserEngine.Tokenize(line, options);
    }
}
