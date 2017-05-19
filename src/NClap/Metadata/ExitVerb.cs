using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NClap.Repl;

namespace NClap.Metadata
{
    internal class ExitVerb : IVerb
    {
        public void Execute(ILoop loop, object context)
        {
            loop.Exit = true;
        }
    }
}
