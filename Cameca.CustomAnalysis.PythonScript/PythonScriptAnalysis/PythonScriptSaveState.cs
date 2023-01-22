using System.Collections.Generic;

namespace Cameca.CustomAnalysis.PythonScript.PythonScriptAnalysis;

public class PythonScriptSaveState
{
	public string Title { get; init; } = "";
	public string ScriptText { get; init; } = "";
	public List<string> Sections { get; init; } = new();
}
