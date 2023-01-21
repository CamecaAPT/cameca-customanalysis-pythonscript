using System;
using System.Collections.Generic;
using System.Linq;

namespace Cameca.CustomAnalysis.PythonScript.Python.DelegatedExecute.Executors.FunctionWrappedFlatScript;

internal class FunctionWrapper
{
	private const string DefaultFunctionName = "run";
	private const string DefaultIndent = "\t";

	public string Script { get; }

	public string FunctionName { get; }

	public string Indent { get; }

	public IReadOnlyList<ParameterDefinition> PositionalArguments { get; }

	public IReadOnlyList<ParameterDefinition> KeywordArguments { get; }

	public FunctionWrapper(string script, IEnumerable<ParameterDefinition>? positionalArguments = null, IEnumerable<ParameterDefinition>? keywordArguments = null, string functionName = DefaultFunctionName, string indent = DefaultIndent)
	{
		Script = script;
		FunctionName = PythonSyntax.IsValidIdentifier(functionName)
			? functionName
			: throw new ArgumentException(string.Format(Resources.InvalidPythonIdentifierExceptionMessage, functionName), nameof(functionName));
		Indent = PythonSyntax.IsValidIndent(indent)
			? indent :
			throw new ArgumentException(string.Format(Resources.InvalidPythonIndentationExceptionMessage, indent), nameof(indent));
		PositionalArguments = positionalArguments?.ToList() ?? new();
		KeywordArguments = keywordArguments?.ToList() ?? new();
	}
}
