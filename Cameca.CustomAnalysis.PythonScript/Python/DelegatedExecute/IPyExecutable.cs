using System.Threading;
using Python.Runtime;

namespace Cameca.CustomAnalysis.PythonScript.Python.DelegatedExecute;

internal interface IPyExecutable
{
	PyObject? Execute(PyModule scope, CancellationToken token);
}
