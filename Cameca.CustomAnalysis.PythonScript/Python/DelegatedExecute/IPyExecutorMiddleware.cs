using System.Threading;
using Python.Runtime;

namespace Cameca.CustomAnalysis.PythonScript.Python.DelegatedExecute;

internal interface IPyExecutorMiddleware
{
	void Preprocess(PyModule scope, CancellationToken token);

	void PostProcess(PyModule scope, PyObject? results, CancellationToken token);

	void Finalize(PyModule scope);
}
