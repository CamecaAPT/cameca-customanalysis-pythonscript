using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Prism.Events;

namespace Cameca.CustomAnalysis.PythonScript;

internal class PythonScriptNodeMenuFactory : AnalysisMenuFactoryBase
{
    public PythonScriptNodeMenuFactory(IEventAggregator eventAggregator)
        : base(eventAggregator)
    {
    }

    protected override INodeDisplayInfo DisplayInfo => PythonScriptNode.DisplayInfo;
    protected override string NodeUniqueId => PythonScriptNode.UniqueId;
    public override AnalysisMenuLocation Location { get; } = AnalysisMenuLocation.Analysis;
}