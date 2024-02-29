using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.PythonScript.Images;
using Cameca.CustomAnalysis.PythonScript.Python.DelegatedExecute;
using Cameca.CustomAnalysis.PythonScript.Python.DelegatedExecute.Executors;
using Cameca.CustomAnalysis.PythonScript.Python.DelegatedExecute.Executors.FunctionWrappedFlatScript;
using Cameca.CustomAnalysis.PythonScript.PythonScriptAnalysis.Adapters;
using Cameca.CustomAnalysis.Utilities;
using Prism.Ioc;
using Python.Runtime;

namespace Cameca.CustomAnalysis.PythonScript.PythonScriptAnalysis;

[DefaultView(PythonScriptViewModel.UniqueId, typeof(PythonScriptViewModel))]
[NodeType(NodeType.DataFilter)]
internal class PythonScriptNode : StandardAnalysisNodeBase
{
	public const string UniqueId = "Cameca.CustomAnalysis.PythonScript.PythonScriptNode";

	private readonly PyExecutor _pyExecutor;
	private readonly INodeDataFilterProvider _nodeDataFilterProvider;
	private readonly INodeInfoProvider _nodeInfoProvider;
	private readonly IIonDisplayInfoProvider _ionDisplayInfoProvider;
	private readonly IMassSpectrumRangeManagerProvider _rangeManagerProvider;
	private readonly IReconstructionSectionsProvider _reconstructionSectionsProvider;
	private readonly IContainerProvider containerProvider;
	private INodeInfo? _nodeInfo;

	private readonly IReadOnlyList<IPyExecutorMiddleware> requiredMiddleware = new List<IPyExecutorMiddleware>
	{
		new PythonScriptNodeRequiredPreprocessing(),
	};

	public static INodeDisplayInfo DisplayInfo { get; } = new NodeDisplayInfo(Resources.PythonScriptDisplayName, ImagesContainer.Python16x16);

	public string Title => _nodeInfo?.Title ?? DisplayInfo.Title;

	private string _scriptText = "";

	public string ScriptText
	{
		get => _scriptText;
		set
		{
			_scriptText = value;
			DataStateIsValid = false;
		}
	}

	public PythonScriptNode(
		PyExecutor pyExecutor,
		INodeDataFilterProvider nodeDataFilterProvider,
		INodeInfoProvider nodeInfoProvider,
		IIonDisplayInfoProvider ionDisplayInfoProvider,
		IMassSpectrumRangeManagerProvider rangeManagerProvider,
		IReconstructionSectionsProvider reconstructionSectionsProvider,
		IStandardAnalysisNodeBaseServices services,
		IContainerProvider containerProvider)
        : base(services)
	{
		_pyExecutor = pyExecutor;
		_nodeDataFilterProvider = nodeDataFilterProvider;
		_nodeInfoProvider = nodeInfoProvider;
		_ionDisplayInfoProvider = ionDisplayInfoProvider;
		_rangeManagerProvider = rangeManagerProvider;
		_reconstructionSectionsProvider = reconstructionSectionsProvider;
		this.containerProvider = containerProvider;
	}

	protected override void OnCreated(NodeCreatedEventArgs eventArgs)
	{
		if (eventArgs is { Trigger: EventTrigger.Load, Data: { } loadData })
		{
			try
			{
				var loadState = JsonSerializer.Deserialize<PythonScriptSaveState>(loadData);
				UpdateTitle(loadState?.Title ?? DisplayInfo.Title);
				ScriptText = loadState?.ScriptText ?? "";
				SelectedSection = loadState?.Sections ?? Enumerable.Empty<string>().ToList();
			}
			catch (JsonException) { }
			catch (NotSupportedException) { }
		}
	}

	protected override byte[]? GetSaveContent()
	{
		var serializedState = JsonSerializer.Serialize(new PythonScriptSaveState
		{
			Title = _nodeInfo?.Title ?? "",
			ScriptText = ScriptText,
			Sections = SelectedSection,
		});
		return Encoding.UTF8.GetBytes(serializedState);
	}

	protected override void OnAdded(NodeAddedEventArgs eventArgs)
	{
		_nodeInfo = _nodeInfoProvider?.Resolve(InstanceId);
		if (_nodeDataFilterProvider.Resolve(InstanceId) is { } nodeDataFilter)
		{
			nodeDataFilter.FilterDelegate = FilterDelegate;
		}
	}

	private async IAsyncEnumerable<ReadOnlyMemory<ulong>> FilterDelegate(IIonData ownerIonData, IProgress<double>? progress, [EnumeratorCancellation] CancellationToken token)
	{
		var captureMiddleware = new CaptureFilterIndices();
		await RunScript(ScriptText, SelectedSection, new[]{ captureMiddleware }, token);
		if (!captureMiddleware.HasResult)
		{
			yield return Array.Empty<ulong>();
			yield break;
		}

		foreach (var chunkIndices in captureMiddleware.Value ?? Enumerable.Empty<ReadOnlyMemory<ulong>>())
		{
			yield return chunkIndices;
		}
	}

	protected async Task<IIonData?> GetOwnerIonData(IProgress<double>? progress = null, CancellationToken cancellationToken = default)
	{
		return Services.IonDataProvider.Resolve(InstanceId) is { OwnerNodeId: { } ownerNodeId }
			? await Services.IonDataProvider.GetIonData(ownerNodeId, progress, cancellationToken)
			: null;
	}

	public static readonly string[] DefaultSections = new[]
	{
		IonDataSectionName.Position,
		IonDataSectionName.Mass,
		IonDataSectionName.IonType,
	};

	public List<string> SelectedSection = DefaultSections.ToList();

	public IEnumerable<string> GetCurrentSections()
	{
		if (Services.IonDataProvider.Resolve(InstanceId)?.GetValidIonData() is { } ionData)
		{
			return ionData.Sections.Keys.Concat(SelectedSection).Distinct();
		}
		return SelectedSection;
	}

	public async Task<IEnumerable<string>> GetAvailableSections()
	{
		IEnumerable<string> sections = Enumerable.Empty<string>();
		if (await GetOwnerIonData() is { } ionData)
		{
			sections = sections.Concat(ionData.Sections.Keys);
		}
		if (_reconstructionSectionsProvider.Resolve(InstanceId) is not { IsAddSectionAvailable: true } reconstructionSections)
		{
			return sections;
		}

		var availableSections = (await reconstructionSections.GetAvailableSections())
			.Select(x => x.Name);
		return sections.Concat(availableSections).Distinct();
	}

	public async Task RunScript(
	    string script,
		IEnumerable<string> sections,
	    IEnumerable<IPyExecutorMiddleware> middleware,
	    CancellationToken token)
	{
		try
		{
			if (await GetOwnerIonData(cancellationToken: token) is not { } ionData)
			{
				return;
			}
			var sectionArray = sections.ToArray();
			if (sectionArray.Any())
			{
				// Ensure we have the ion data available
				if (!await ionData.EnsureSectionsAvailable(sectionArray, _reconstructionSectionsProvider.Resolve(InstanceId), cancellationToken: token))
				{
					// This should be unlikely. Could occur through copying analysis tree, recipes or favorites
					throw new InvalidOperationException("One or more selected sections were unavailable");
				}
			}
			var apsDataProvider = new ApsDataObjectProvider(
				ionData,
				sectionArray,
				_ionDisplayInfoProvider.Resolve(InstanceId),
				_rangeManagerProvider.Resolve(InstanceId));
			var contextProvider = new APSuiteContextProvider(
				ionData,
				_ionDisplayInfoProvider.Resolve(InstanceId),
				_rangeManagerProvider.Resolve(Services.NodeInfoProvider.GetRootNodeContainer(InstanceId).NodeId),
				containerProvider.Resolve<INodePropertiesProvider>().Resolve(InstanceId),
				containerProvider.Resolve<INodeElementDataSetProvider>().Resolve(InstanceId),
				containerProvider.Resolve<IElementDataSetService>(),
				containerProvider.Resolve<IReconstructionSectionsProvider>().Resolve(InstanceId),
				containerProvider.Resolve<IExperimentInfoProvider>().Resolve(InstanceId));
			var functionWrapper = new FunctionWrapper(
				script,
				positionalArguments: new[]
				{
					new ParameterDefinition("aps", apsDataProvider),
					new ParameterDefinition("context", contextProvider),
				});
			var executable = new FunctionWrappedScriptExecutable(functionWrapper);
			// This isn't the right solution as the exception handing should be isolated to the VM
			// as the output is only guaranteed to be redirected there.
			// Ideal solution would be to have StdStream redirect register a sys.excepthook to write out
			// any exceptions, then catch and suppress PythonException in the VM
			// Unfortunately I can not get sys.excepthook to trigger. So this is my solution for now
			// TODO: Improve exception handing. Either resolve sys.excepthook issue or provide custom hooks for middleware
			var wrapper = new HandlePythonExceptionWrapper<FunctionWrappedScriptExecutable>(executable);
			await _pyExecutor.Execute(wrapper, requiredMiddleware.Concat(middleware), token);
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

	public void UpdateTitle(string title)
	{
		if (string.IsNullOrWhiteSpace(title))
			return;
		Services.EventAggregator.PublishRenameNode(InstanceId, title);
	}
}
