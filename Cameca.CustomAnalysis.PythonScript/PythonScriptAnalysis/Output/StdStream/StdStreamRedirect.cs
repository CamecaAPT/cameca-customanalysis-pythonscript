using Cameca.CustomAnalysis.PythonScript.Python.DelegatedExecute;
using Python.Runtime;
using System;
using System.Threading;

namespace Cameca.CustomAnalysis.PythonScript.PythonScriptAnalysis.Output.StdStream;

internal class StdstreamRedirect : IPyExecutorMiddleware
{
	private const string StringIONetWrapperDefinition = @"
import io

class StringIONetWrapper(io.StringIO):
    def __init__(self, callback, *args, **kwargs):
        self.callback = callback
        super().__init__(*args, **kwargs)

    def write(self, value):
        self.callback(value)
        super().write(value)
";

	private readonly Action<object?> _stdwriteCallback;

	public StdstreamRedirect(Action<object?> stdwriteCallback)
	{
		_stdwriteCallback = stdwriteCallback;
	}

	public void Preprocess(PyModule scope, CancellationToken token)
	{
		var stdstream = PyModule.FromString("stdstream", StringIONetWrapperDefinition);
		var sys = scope.Import("sys");
		var callback = new Action<object?>(_stdwriteCallback).ToPython();
		var stdout_wrapper = stdstream.InvokeMethod("StringIONetWrapper", callback);

		sys.SetAttr("stdout", stdout_wrapper);
		sys.GetAttr("stdout").InvokeMethod("flush");
		sys.SetAttr("stderr", stdout_wrapper);
		sys.GetAttr("stderr").InvokeMethod("flush");
	}

	public void PostProcess(PyModule scope, PyObject? results, CancellationToken token) { }

	public void Finalize(PyModule scope)
	{
		var sys = scope.Import("sys");
		sys.GetAttr("stdout").InvokeMethod("flush");
		sys.GetAttr("stderr").InvokeMethod("flush");
	}
}
