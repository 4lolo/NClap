﻿using System;
using System.Collections.Generic;

namespace NClap.Utilities
{
    /// <summary>
    /// Simplified, colored string version of StringBuilder.
    /// </summary>
    public class ColoredMultistringBuilder
    {
        private readonly List<ColoredString> _pieces = new List<ColoredString>();

        /// <summary>
        /// Append a colored string.
        /// </summary>
        /// <param name="value">The colored string to append.</param>
        public void Append(ColoredString value) => _pieces.Add(value);

        /// <summary>
        /// Append the provided colored strings.
        /// </summary>
        /// <param name="values">The colored strings to append.</param>
        public void Append(IEnumerable<ColoredString> values) => _pieces.AddRange(values);

        /// <summary>
        /// Append a newline.
        /// </summary>
        public void AppendLine() => _pieces.Add(Environment.NewLine);

        /// <summary>
        /// Append a colored string followed by a newline.
        /// </summary>
        /// <param name="value">The colored string to append.</param>
        public void AppendLine(ColoredString value)
        {
            _pieces.Add(value);
            _pieces.Add(new ColoredString(Environment.NewLine, value.ForegroundColor, value.BackgroundColor));
        }

        /// <summary>
        /// Append the provided colored strings followed by a newline.
        /// </summary>
        /// <param name="values">The colored multistrings to append.</param>
        public void AppendLine(IEnumerable<ColoredString> values)
        {
            Append(values);
            AppendLine();
        }

        /// <summary>
        /// Converts the current contents of the builder to a bare string.
        /// </summary>
        /// <returns></returns>
        public override string ToString() =>
            string.Concat(_pieces);

        /// <summary>
        /// Converts the current contents of the builder to a colored
        /// multistring.
        /// </summary>
        /// <returns>The multistring.</returns>
        public ColoredMultistring ToMultistring() => new ColoredMultistring(_pieces);
    }
}
