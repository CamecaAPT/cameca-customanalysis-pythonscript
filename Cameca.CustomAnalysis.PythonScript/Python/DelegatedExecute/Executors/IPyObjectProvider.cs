using Python.Runtime;

namespace Cameca.CustomAnalysis.PythonScript.Python.DelegatedExecute.Executors;

internal interface IPyObjectProvider
{
	PyObject GetPyObject(PyModule scope);
}
