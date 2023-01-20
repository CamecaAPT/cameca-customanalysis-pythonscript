using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;

namespace Cameca.CustomAnalysis.PythonScript.PythonScriptAnalysis;

[DefaultView(PythonScriptViewModel.UniqueId, typeof(PythonScriptViewModel))]
internal class PythonScriptNode : StandardAnalysisNodeBase
{
    public const string UniqueId = "Cameca.CustomAnalysis.PythonScript.PythonScriptNode";
    
    public static INodeDisplayInfo DisplayInfo { get; } = new NodeDisplayInfo("Python Script");

    public PythonScriptNode(IStandardAnalysisNodeBaseServices services)
        : base(services)
    {
    }
}