using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.PythonScript.Images;
using Cameca.CustomAnalysis.PythonScript.Python.DelegatedExecute;
using Cameca.CustomAnalysis.PythonScript.Python.DelegatedExecute.Executors.FunctionWrappedFlatScript;
using Cameca.CustomAnalysis.Utilities;

namespace Cameca.CustomAnalysis.PythonScript.PythonScriptAnalysis;

[DefaultView(PythonScriptViewModel.UniqueId, typeof(PythonScriptViewModel))]
internal class PythonScriptNode : StandardAnalysisNodeBase
{
	public const string UniqueId = "Cameca.CustomAnalysis.PythonScript.PythonScriptNode";

	private readonly PyExecutor _pyExecutor;
	private readonly INodeInfoProvider _nodeInfoProvider;
	private INodeInfo? _nodeInfo;

	public static INodeDisplayInfo DisplayInfo { get; } = new NodeDisplayInfo(Resources.PythonScriptDisplayName, ImagesContainer.Python16x16);

	public string Title => _nodeInfo?.Title ?? DisplayInfo.Title;

	public string ScriptText { get; set; } = "";

	public PythonScriptNode(PyExecutor pyExecutor, INodeInfoProvider nodeInfoProvider, IStandardAnalysisNodeBaseServices services)
        : base(services)
	{
		_pyExecutor = pyExecutor;
		_nodeInfoProvider = nodeInfoProvider;
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
			var functionWrapper = new FunctionWrapper(script);
			var executable = new FunctionWrappedScriptExecutable(functionWrapper);
			await _pyExecutor.Execute(executable, middleware, token);
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
