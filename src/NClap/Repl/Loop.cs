using NClap.Metadata;
using System.Collections.Generic;

namespace NClap.Repl
{
    /// <summary>
    /// Wrapper class for Loop&lt;TVerbType, TContext&gt;.
    /// </summary>
    /// <typeparam name="TVerbType">Enum type that defines possible verbs.
    /// </typeparam>
    public static class Loop
    {
        /// <summary>
        /// Executes the loop.
        /// </summary>
        public static void Execute(IEnumerable<VerbDescriptor> verbs) => 
            Execute(verbs, (LoopInputOutputParameters)null, null);

        /// <summary>
        /// Executes the loop.
        /// </summary>
        /// <param name="loopClient">Options for loop.</param>
        public static void Execute(IEnumerable<VerbDescriptor> verbs, ILoopClient loopClient) => 
            Execute(verbs, loopClient, null);

        /// <summary>
        /// Executes the loop.
        /// </summary>
        /// <param name="options">Options for loop.</param>
        public static void Execute(IEnumerable<VerbDescriptor> verbs, LoopOptions options) => 
            Execute(verbs, (LoopInputOutputParameters)null, options);

        /// <summary>
        /// Executes the loop.
        /// </summary>
        /// <param name="loopClient">The client to use.</param>
        /// <param name="options">Options for loop.</param>
        public static void Execute(IEnumerable<VerbDescriptor> verbs, ILoopClient loopClient, LoopOptions options) =>
            Execute<object>(verbs, loopClient, options, null);

        /// <summary>
        /// Executes the loop.
        /// </summary>
        /// <param name="parameters">Optionally provides parameters controlling
        /// the loop's input and output behaviors; if not provided, default
        /// parameters are used.</param>
        /// <param name="options">Options for loop.</param>
        public static void Execute(IEnumerable<VerbDescriptor> verbs, LoopInputOutputParameters parameters, LoopOptions options) => 
            Execute<object>(verbs, parameters, options, null);

        /// <summary>
        /// Executes the loop.
        /// </summary>
        /// <param name="loopClient">The client to use.</param>
        /// <param name="options">Options for loop.</param>
        /// <param name="context">Context object for the loop.</param>
        public static void Execute<TContext>(IEnumerable<VerbDescriptor> verbs, ILoopClient loopClient, LoopOptions options, TContext context)
        {
            var loop = new Loop<TContext>(verbs, loopClient, options, context);
            loop.Execute();
        }

        /// <summary>
        /// Executes the loop.
        /// </summary>
        /// <param name="parameters">Optionally provides parameters controlling
        /// the loop's input and output behaviors; if not provided, default
        /// parameters are used.</param>
        /// <param name="options">Options for loop.</param>
        /// <param name="context">Context object for the loop.</param>
        public static void Execute<TContext>(IEnumerable<VerbDescriptor> verbs, LoopInputOutputParameters parameters, LoopOptions options, TContext context)
        {
            var loop = new Loop<TContext>(verbs, parameters, options, context);
            loop.Execute();
        }

        /// <summary>
        /// Executes one command.
        /// </summary>
        /// <param name="options">Options for loop.</param>
        public static void ExecuteOnce(IEnumerable<VerbDescriptor> verbs, LoopOptions options, string[] args) => 
            ExecuteOnce(verbs, (LoopInputOutputParameters)null, options, args);

        /// <summary>
        /// Executes one command.
        /// </summary>
        /// <param name="parameters">Optionally provides parameters controlling
        /// the loop's input and output behaviors; if not provided, default
        /// parameters are used.</param>
        /// <param name="options">Options for loop.</param>
        public static void ExecuteOnce(IEnumerable<VerbDescriptor> verbs, LoopInputOutputParameters parameters, LoopOptions options, string[] args) => 
            ExecuteOnce<object>(verbs, parameters, options, null, args);

        /// <summary>
        /// Executes the loop.
        /// </summary>
        /// <param name="loopClient">The client to use.</param>
        /// <param name="options">Options for loop.</param>
        /// <param name="context">Context object for the loop.</param>
        public static void ExecuteOnce<TContext>(IEnumerable<VerbDescriptor> verbs, ILoopClient loopClient, LoopOptions options, TContext context, string[] args)
        {
            var loop = new Loop<TContext>(verbs, loopClient, options, context);
            loop.ExecuteOnce(args);
        }

        /// <summary>
        /// Executes the loop.
        /// </summary>
        /// <param name="parameters">Optionally provides parameters controlling
        /// the loop's input and output behaviors; if not provided, default
        /// parameters are used.</param>
        /// <param name="options">Options for loop.</param>
        /// <param name="context">Context object for the loop.</param>
        public static void ExecuteOnce<TContext>(IEnumerable<VerbDescriptor> verbs, LoopInputOutputParameters parameters, LoopOptions options, TContext context, string[] args)
        {
            var loop = new Loop<TContext>(verbs, parameters, options, context);
            loop.ExecuteOnce(args);
        }
    }
}