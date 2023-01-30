using System.Threading;
using Cameca.CustomAnalysis.PythonScript.Python.DelegatedExecute;
using Python.Runtime;

namespace Cameca.CustomAnalysis.PythonScript.PythonScriptAnalysis;

/// <summary>
/// While view models may add middleware specific to the view rendering,
/// there are certain preprocessing events that should be performed regardless
/// of the mode of the node running (i.e. headless or with a view)
/// </summary>
internal class PythonScriptNodeRequiredPreprocessing : IPyExecutorMiddleware
{
	public void Preprocess(PyModule scope, CancellationToken token)
	{
		// It seems that running a script without importing matplotlib, then trying import it later fails.
		// My best guess is that the issue is due to numpy in some way, likely not releasing some memory or handle
		// As we assume that we want matplotlib available for this extension, simply ensure that it is at least
		// loaded first before any other script action is taken
		try
		{
			scope.Import("matplotlib");
		}
		catch { }
	}

	public void PostProcess(PyModule scope, PyObject? results, CancellationToken token) { }

	public void Finalize(PyModule scope) { }
}
