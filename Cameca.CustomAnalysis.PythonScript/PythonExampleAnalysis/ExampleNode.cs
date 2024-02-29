using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.PythonScript.Python.DelegatedExecute;
using Cameca.CustomAnalysis.PythonScript.Python.DelegatedExecute.Executors.FunctionWrappedFlatScript;
using Cameca.CustomAnalysis.PythonScript.Python.DelegatedExecute.Executors;
using Cameca.CustomAnalysis.PythonScript.PythonScriptAnalysis.Adapters;
using Cameca.CustomAnalysis.PythonScript.PythonScriptAnalysis;
using Cameca.CustomAnalysis.Utilities;
using Cameca.CustomAnalysis.PythonScript.Python.DelegatedExecute.Executors.LocalModule;
using Cameca.CustomAnalysis.PythonScript.Images;
using Python.Runtime;
using Prism.Ioc;

namespace Cameca.CustomAnalysis.PythonScript.PythonExampleAnalysis;

[NodeType(NodeType.DataFilter)]
internal class ExampleNode : StandardAnalysisNodeBase
{
	public const string UniqueId = "Cameca.CustomAnalysis.PythonScript.ExampleNode";
	private readonly PyExecutor pyExecutor;
	private readonly IContainerProvider containerProvider;
	public static INodeDisplayInfo DisplayInfo { get; } = new NodeDisplayInfo($"Example: {Resources.PythonScriptDisplayName}", ImagesContainer.Python16x16);

	public ExampleNode(
		PyExecutor pyExecutor,
		IStandardAnalysisNodeBaseServices services,
		IContainerProvider containerProvider)
		: base(services)
	{
		this.pyExecutor = pyExecutor;
		this.containerProvider = containerProvider;
	}

	protected async Task<IIonData?> GetOwnerIonData(IProgress<double>? progress = null, CancellationToken cancellationToken = default)
	{
		return Services.IonDataProvider.Resolve(InstanceId) is { OwnerNodeId: { } ownerNodeId }
			? await Services.IonDataProvider.GetIonData(ownerNodeId, progress, cancellationToken)
			: null;
	}

	protected override async void OnDoubleClick()
	{
		base.OnDoubleClick();
		await Run(Array.Empty<IPyExecutorMiddleware>(), default);
	}

	public async Task Run(IEnumerable<IPyExecutorMiddleware> middleware, CancellationToken token)
	{
		try
		{
			if (await GetOwnerIonData(cancellationToken: token) is not { } ionData)
			{
				return;
			}
			//var sectionArray = sections.ToArray();
			//if (sectionArray.Any())
			//{
			//	// Ensure we have the ion data available
			//	if (!await ionData.EnsureSectionsAvailable(sectionArray, reconstructionSectionsProvider.Resolve(InstanceId), cancellationToken: token))
			//	{
			//		// This should be unlikely. Could occur through copying analysis tree, recipes or favorites
			//		throw new InvalidOperationException("One or more selected sections were unavailable");
			//	}
			//}

			var contextProvider = new APSuiteContextProvider(
				ionData,
				containerProvider.Resolve<IIonDisplayInfoProvider>().Resolve(InstanceId),
				containerProvider.Resolve<IMassSpectrumRangeManagerProvider>().Resolve(Services.NodeInfoProvider.GetRootNodeContainer(InstanceId).NodeId),
				containerProvider.Resolve<INodePropertiesProvider>().Resolve(InstanceId),
				containerProvider.Resolve<INodeElementDataSetProvider>().Resolve(InstanceId),
				containerProvider.Resolve<IElementDataSetService>(),
				containerProvider.Resolve<IReconstructionSectionsProvider>().Resolve(InstanceId),
				containerProvider.Resolve<IExperimentInfoProvider>().Resolve(InstanceId));

			var executable = new LocalModuleExecutable("Cluster_Analysis_Main", "main", new IPyObjectProvider[] { contextProvider });
			var wrapper = new HandlePythonExceptionWrapper<LocalModuleExecutable>(executable);
			await pyExecutor.Execute(wrapper, middleware, token);
		}
		catch (TaskCanceledException)
		{
			// Expected on cancellation
		}
		catch (PythonException)
		{
			// Handled internally for diaplay, but re-thrown to avoid treating as "successful" for post-processing
			// Capture here again and suppress, and exception details are reported to stderr
			// Potentially log in the future
		}

		DataStateIsValid = true;
	}
}
