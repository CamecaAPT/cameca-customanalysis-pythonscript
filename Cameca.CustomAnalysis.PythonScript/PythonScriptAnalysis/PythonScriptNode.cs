using System;
using System.Collections.Generic;
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

namespace Cameca.CustomAnalysis.PythonScript.PythonScriptAnalysis;

[DefaultView(PythonScriptViewModel.UniqueId, typeof(PythonScriptViewModel))]
internal class PythonScriptNode : StandardAnalysisNodeBase
{
	public const string UniqueId = "Cameca.CustomAnalysis.PythonScript.PythonScriptNode";

	private readonly PyExecutor _pyExecutor;
	private readonly INodeInfoProvider _nodeInfoProvider;
	private readonly IIonDisplayInfoProvider _ionDisplayInfoProvider;
	private readonly IMassSpectrumRangeManagerProvider _rangeManagerProvider;
	private INodeInfo? _nodeInfo;

	public static INodeDisplayInfo DisplayInfo { get; } = new NodeDisplayInfo(Resources.PythonScriptDisplayName, ImagesContainer.Python16x16);

	public string Title => _nodeInfo?.Title ?? DisplayInfo.Title;

	public string ScriptText { get; set; } = "";

	public PythonScriptNode(
		PyExecutor pyExecutor,
		INodeInfoProvider nodeInfoProvider,
		IIonDisplayInfoProvider ionDisplayInfoProvider,
		IMassSpectrumRangeManagerProvider rangeManagerProvider,
		IStandardAnalysisNodeBaseServices services)
        : base(services)
	{
		_pyExecutor = pyExecutor;
		_nodeInfoProvider = nodeInfoProvider;
		_ionDisplayInfoProvider = ionDisplayInfoProvider;
		_rangeManagerProvider = rangeManagerProvider;
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
		});
		return Encoding.UTF8.GetBytes(serializedState);
	}

	protected override void OnAdded(NodeAddedEventArgs eventArgs)
	{
		_nodeInfo = _nodeInfoProvider?.Resolve(InstanceId);
	}

	public async Task RunScript(
	    string script,
	    IEnumerable<IPyExecutorMiddleware> middleware,
	    CancellationToken token)
	{
		try
		{
			if (await GetIonData(cancellationToken: token) is not { } ionData)
			{
				return;
			}
			var apsDataProvider = new ApsDataObjectProvider(
				ionData,
				new string[]
				{
					IonDataSectionName.Position,
					IonDataSectionName.Mass,
					IonDataSectionName.IonType,
				},
				_ionDisplayInfoProvider.Resolve(InstanceId),
				_rangeManagerProvider.Resolve(InstanceId));
			var functionWrapper = new FunctionWrapper(
				script,
				positionalArguments: new[]
				{
					new ParameterDefinition("aps", apsDataProvider),
				});
			var executable = new FunctionWrappedScriptExecutable(functionWrapper);
			// This isn't the right solution as the exception handing should be isolated to the VM
			// as the output is only guaranteed to be redirected there.
			// Ideal solution would be to have StdStream redirect register a sys.excepthook to write out
			// any exceptions, then catch and suppress PythonException in the VM
			// Unfortunately I can not get sys.excepthook to trigger. So this is my solution for now
			// TODO: Improve exception handing. Either resolve sys.excepthook issue or provide custom hooks for middleware
			var wrapper = new HandlePythonExceptionWrapper<FunctionWrappedScriptExecutable>(executable);
			await _pyExecutor.Execute(wrapper, middleware, token);
		}
		catch (TaskCanceledException)
		{
			// Expected on cancellation
		}
	}

	public void UpdateTitle(string title)
	{
		if (string.IsNullOrWhiteSpace(title))
			return;
		Services.EventAggregator.PublishRenameNode(InstanceId, title);
	}
}
