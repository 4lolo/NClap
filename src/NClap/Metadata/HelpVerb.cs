using System;
using System.Globalization;
using System.Linq;
using NClap.ConsoleInput;
using NClap.Parser;
using NClap.Utilities;
using NClap.Repl;

namespace NClap.Metadata
{
    /// <summary>
    /// Verb for display help about the verbs available.
    /// </summary>
    internal class HelpVerb : IVerb
    {
        /// <summary>
        /// The default options to use for generate usage info.
        /// </summary>
        public static UsageInfoOptions DefaultUsageInfoOptions { get; set; } =
            UsageInfoOptions.Default | UsageInfoOptions.UseColor;

        /// <summary>
        /// The output handler function for this class.
        /// </summary>
        public static Action<ColoredMultistring> OutputHandler { get; set; }

        /// <summary>
        /// Optionally specifies the verb to retrieve detailed help information
        /// for.
        /// </summary>
        [PositionalArgument(ArgumentFlags.AtMostOnce, HelpText = "Verb to get detailed help information for.")]
        public string Verb { get; set; }

        /// <summary>
        /// Options for displaying help.
        /// </summary>
        [NamedArgument(ArgumentFlags.AtMostOnce, HelpText = "Options for displaying help.")]
        public HelpOptions Options { get; set; }

        /// <summary>
        /// Displays help about the available verbs.
        /// </summary>
        /// <param name="context"></param>
        public void Execute(ILoop loop, object context)
        {
            var outputHandler = HelpVerb.OutputHandler ?? BasicConsoleInputAndOutput.Default.Write;

            if (Verb != null)
            {
                DisplayVerbHelp(loop, outputHandler, Verb);
            }
            else
            {
                DisplayGeneralHelp(loop, outputHandler);
            }
        }

        private static void DisplayGeneralHelp(ILoop loop, Action<ColoredMultistring> outputHandler)
        {
            var verbNames = loop.Verbs
                .Select(v => v.Name)
                .OrderBy(v => v, StringComparer.CurrentCultureIgnoreCase);

            var verbNameMaxLen = verbNames.Max(name => name.ToString().Length);

            var verbSummary = string.Concat(verbNames.Select(verbType =>
            {
                var helpText = GetHelpText(loop, verbType);

                var formatString = "  {0,-" + verbNameMaxLen.ToString(CultureInfo.InvariantCulture) + "}{1}\n";
                return string.Format(
                    CultureInfo.CurrentCulture,
                    formatString,
                    verbType,
                    helpText != null ? " - " + helpText : string.Empty);
            }));

            outputHandler(string.Format(CultureInfo.CurrentCulture, Strings.ValidVerbsHeader, verbSummary));
        }

        private static void DisplayVerbHelp(ILoop loop, Action<ColoredMultistring> outputHandler, string verb)
        {
            var attrib = GetVerbDescriptor(loop, verb);
            var implementingType = attrib.ImplementingType;
            if (implementingType == null)
            {
                outputHandler(Strings.NoHelpAvailable + Environment.NewLine);
                return;
            }

            var usageInfo = CommandLineParser.GetUsageInfo(implementingType, null, null, verb.ToString(), HelpVerb.DefaultUsageInfoOptions);

            /*
            if (!string.IsNullOrEmpty(attrib.HelpText))
            {
                usageInfo = attrib.HelpText + "\n\n" + usageInfo;
            }
            */

            outputHandler(usageInfo);
        }

        private static string GetHelpText(ILoop loop, string value)
        {
            var attrib = GetVerbDescriptor(loop, value);
            var helpText = attrib?.HelpText;
            return !string.IsNullOrEmpty(helpText) ? helpText : null;
        }

        private static VerbDescriptor GetVerbDescriptor(ILoop loop, string value)
        {
            return loop.Verbs.SingleOrDefault(v => v.Key == value.ToLowerInvariant());
        }
    }
}
