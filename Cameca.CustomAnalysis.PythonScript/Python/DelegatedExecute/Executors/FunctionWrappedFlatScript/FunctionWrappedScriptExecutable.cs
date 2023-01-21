using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Python.Runtime;

namespace Cameca.CustomAnalysis.PythonScript.Python.DelegatedExecute.Executors.FunctionWrappedFlatScript;

internal class FunctionWrappedScriptExecutable : IPyExecutable
{
	private readonly FunctionWrapper _functionWrapper;

	public FunctionWrappedScriptExecutable(FunctionWrapper functionWrapper)
	{
		_functionWrapper = functionWrapper;
	}

	public PyObject? Execute(PyModule scope, CancellationToken token)
	{
		var wrapper = scope.Import("function_wrapped_script.wrapper");
		var argNameList = new List<string>();
		var argValueList = new List<PyObject>();
		var kwargsNameList = new List<string>();
		PyDict? kwargsDict = null;
		foreach (var paramDef in _functionWrapper.PositionalArguments)
		{
			argNameList.Add(paramDef.Name);
			argValueList.Add(paramDef.ValueProvider.GetPyObject(scope));
		}
		foreach (var paramDef in _functionWrapper.KeywordArguments)
		{
			// Lazy create dict only if keyword arguments are present
			kwargsDict ??= new PyDict();
			kwargsNameList.Add(paramDef.Name);
			kwargsDict[paramDef.Name.ToPython()] = paramDef.ValueProvider.GetPyObject(scope);
		}
		var pyIndent = new PyString(_functionWrapper.Indent);
		var runFuncWrappedScript = wrapper
			.InvokeMethod("function_wrapper",
				new PyString(_functionWrapper.Script),
				new PyString($"def {_functionWrapper.FunctionName}({string.Join(", ", argNameList.Concat(kwargsNameList))})"),
				pyIndent)
			.As<string>();

		var moduleName = GetModuleName(runFuncWrappedScript);
		var module = PyModule.FromString(moduleName, runFuncWrappedScript);
		// Current implementation has any value returned from the top level of the script stored in "_run_results"
		// which can then be accessed through the locals parameter of any included middleware
		return module.InvokeMethod(_functionWrapper.FunctionName, argValueList.ToArray(), kwargsDict);
	}

	/// <summary>
	/// Generate module name from hash of script content
	/// </summary>
	/// <remarks>
	/// Python caches modules by name. By generating a hashed name by content, changes will generate a new module.
	/// Because the hash is derived from content, reverting changes to previous scripts will load from cache.
	/// </remarks>
	/// <param name="script"></param>
	/// <returns></returns>
	private static string GetModuleName(string script)
	{
		using var md5 = MD5.Create();
		var encodedBytes = Encoding.UTF8.GetBytes(script);
		var hashBytes = md5.ComputeHash(encodedBytes);
		// Ensure module name doesn't start with a number
		return $"_{Convert.ToHexString(hashBytes)}";
	}
}
