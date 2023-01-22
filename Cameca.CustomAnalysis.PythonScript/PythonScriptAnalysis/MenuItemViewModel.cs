using Prism.Mvvm;

namespace Cameca.CustomAnalysis.PythonScript.PythonScriptAnalysis;

internal class MenuItemViewModel : BindableBase
{
	public string Header { get; init; } = "";

	private bool _isChecked = false;

	public bool IsChecked
	{
		get => _isChecked;
		set => SetProperty(ref _isChecked, value);
	}
}
