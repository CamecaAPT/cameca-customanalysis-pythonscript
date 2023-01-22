using Prism.Mvvm;

namespace Cameca.CustomAnalysis.PythonScript.PythonScriptAnalysis;

internal abstract class OutputTabViewModel : BindableBase
{
	public abstract string Header { get; }
}
