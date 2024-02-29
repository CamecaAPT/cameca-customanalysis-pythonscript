using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Prism.Events;

namespace Cameca.CustomAnalysis.PythonScript.PythonExampleAnalysis;

internal class ExampleNodeMenuFactory : AnalysisMenuFactoryBase
{
	public ExampleNodeMenuFactory(IEventAggregator eventAggregator)
		: base(eventAggregator)
	{
	}

	protected override INodeDisplayInfo DisplayInfo => ExampleNode.DisplayInfo;
	protected override string NodeUniqueId => ExampleNode.UniqueId;
	public override AnalysisMenuLocation Location { get; } = AnalysisMenuLocation.Analysis;
}
