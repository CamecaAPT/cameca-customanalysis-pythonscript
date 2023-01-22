using System.Collections.Generic;
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

	public static INodeDisplayInfo DisplayInfo { get; } = new NodeDisplayInfo(Resources.PythonScriptDisplayName, ImagesContainer.Python16x16);
	
	public string ScriptText { get; set; } = "";

	public PythonScriptNode(PyExecutor pyExecutor, IStandardAnalysisNodeBaseServices services)
        : base(services)
    {
	    _pyExecutor = pyExecutor;
    }

    public async Task RunScript(
	    string script,
	    IEnumerable<IPyExecutorMiddleware> middleware,
	    CancellationToken token)
	{
		var functionWrapper = new FunctionWrapper(script);
		var executable = new FunctionWrappedScriptExecutable(functionWrapper);
		await _pyExecutor.Execute(executable, middleware, token);
    }
}
