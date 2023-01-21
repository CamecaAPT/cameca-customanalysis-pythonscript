using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Cameca.CustomAnalysis.PythonScript.Python;

/// <summary>
/// Tools for working with Python syntax
/// </summary>
internal static class PythonSyntax
{
    private static readonly ISet<string> ReservedIdentifiers = new HashSet<string>(StringComparer.Ordinal)
    {
        "False", "None", "True", "and", "as", "assert", "async", "await",
        "break", "class", "continue", "def", "del", "elif", "else", "except",
        "finally", "for", "from", "global", "if", "import", "in", "is",
        "lambda", "nonlocal", "not", "or", "pass", "raise", "return", "try",
        "while", "with", "yield",
    };

    public static bool IsValidIdentifier(string identifier)
    {
        /* No reserved keywords
         * Contain letters, digits, and underscore
         * Can’t begin with a digit
         */
        return !ReservedIdentifiers.Contains(identifier)
               && Regex.Match(identifier, "^[_a-z][_a-z0-9]*$", RegexOptions.IgnoreCase).Success;
    }

    public static bool IsValidIndent(string value)
    {
        // Check if string is a single line composed entirely of either tabs or spaces, with a minimum count of 1
        return Regex.Match(value, "^(?:\t+| +)$").Success;
    }
}
