using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cameca.CustomAnalysis.PythonScript.Python.DelegatedExecute.Executors.LocalModule;

internal class LocalModuleExecutable : IPyExecutable
{
	private string module_name;
	private string method_name;
	private IEnumerable<IPyObjectProvider> pyObjectProviders;

	public LocalModuleExecutable(string module, string method, IEnumerable<IPyObjectProvider> pyObjectProviders)
	{
		this.module_name = module;
		this.method_name = method;
		this.pyObjectProviders = pyObjectProviders;
	}

	public PyObject? Execute(PyModule scope, CancellationToken token)
	{
		var custom_module = scope.Import(this.module_name);
		var args = this.pyObjectProviders.Select(x => x.GetPyObject(scope)).ToArray();
		return custom_module.InvokeMethod(this.method_name, args);
	}
}
