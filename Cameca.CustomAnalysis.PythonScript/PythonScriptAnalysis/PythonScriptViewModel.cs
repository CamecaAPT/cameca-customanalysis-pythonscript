using Cameca.CustomAnalysis.Utilities;

namespace Cameca.CustomAnalysis.PythonScript.PythonScriptAnalysis;

internal class PythonScriptViewModel : AnalysisViewModelBase<PythonScriptNode>
{
    public const string UniqueId = "Cameca.CustomAnalysis.PythonScript.PythonScriptViewModel";

    public PythonScriptViewModel(IAnalysisViewModelBaseServices services)
        : base(services)
    {
    }
}
