using System;
using System.Threading;
using Cameca.CustomAnalysis.PythonScript.Python.DelegatedExecute;
using Python.Runtime;

namespace Cameca.CustomAnalysis.PythonScript.PythonScriptAnalysis;

internal class CaptureManagedResults<T> : IPyExecutorMiddleware
{
	// See function_wrapped_script.wrapper.py
	private const string ResultMethodName = "_run_results";

	private T? _value = default;
	public T? Value
	{
		get => _value;
		private set
		{
			_value = value;
			HasResult = true;
		}
	}

	public bool HasResult { get; private set; } = false;

	private readonly Func<PyObject?, T?> _mapFunc;

	public CaptureManagedResults(Func<PyObject?, T?>? mapFunc = null)
	{
		_mapFunc = mapFunc ?? DefaultMapFunc;
	}

	private static T? DefaultMapFunc(PyObject? value) => value is not null ? value.As<T>() : default;

	public void Preprocess(PyModule scope, CancellationToken token) { }

	public void PostProcess(PyModule scope, PyObject? results, CancellationToken token)
	{
		if (results is null)
		{
			return;
		}

		try
		{
			Value = _mapFunc(results[ResultMethodName]);
		}
		catch
		{
			// Pass
		}
	}

	public void Finalize(PyModule scope) { }
}
