using System;
using Python.Runtime;

namespace Cameca.CustomAnalysis.PythonScript.PythonScriptAnalysis;

internal class CaptureFilterIndices : CaptureManagedResults<ReadOnlyMemory<ulong>[]>
{
	public CaptureFilterIndices() : base(ToEnumerableIndices) {}

	private static ReadOnlyMemory<ulong>[] ToEnumerableIndices(PyObject? value)
	{
		if (value is null)
		{
			return new ReadOnlyMemory<ulong>[] { Array.Empty<ulong>() };
		}
		else
		{
			var converted = value.As<ulong[]>();
			return new ReadOnlyMemory<ulong>[] { converted };
		}
		
	}
}
