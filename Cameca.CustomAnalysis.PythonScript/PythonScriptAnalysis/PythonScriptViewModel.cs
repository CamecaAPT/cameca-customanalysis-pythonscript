using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Cameca.CustomAnalysis.PythonScript.Python.DelegatedExecute;
using Cameca.CustomAnalysis.Utilities;
using CommunityToolkit.Mvvm.Input;

namespace Cameca.CustomAnalysis.PythonScript.PythonScriptAnalysis;

internal class PythonScriptViewModel : AnalysisViewModelBase<PythonScriptNode>
{
	public const string UniqueId = "Cameca.CustomAnalysis.PythonScript.PythonScriptViewModel";

	private string _title = "";
	public string Title
	{
		get => _title;
		set => SetProperty(ref _title, value);
	}

	private string _scriptText = "";
	public string ScriptText
	{
		get => _scriptText;
		set => SetProperty(ref _scriptText, value);
	}

	private readonly AsyncRelayCommand _runScriptCommand;
	public ICommand RunScriptCommand => _runScriptCommand;

	private readonly RelayCommand _cancelScriptCommand;
	public ICommand CancelScriptCommand => _cancelScriptCommand;

	private readonly AsyncRelayCommand _getAvailableSectionsCommand;
	public ICommand GetAvailableSectionsCommand => _getAvailableSectionsCommand;

	private readonly RelayCommand<KeyEventArgs> _scriptEditorKeyDownCommand;
	public ICommand ScriptEditorKeyDownCommand => _scriptEditorKeyDownCommand;

	public ObservableCollection<MenuItemViewModel> MenuItems { get; } = new();

	private OutputTabViewModel? _selectedOutputTab = null;
	public OutputTabViewModel? SelectedOutputTab
	{
		get => _selectedOutputTab;
		set => SetProperty(ref _selectedOutputTab, value);
	}

	public ObservableCollection<OutputTabViewModel> OutputTabs { get; } = new();

	public PythonScriptViewModel(IAnalysisViewModelBaseServices services)
		: base(services)
	{
		_runScriptCommand = new AsyncRelayCommand(OnRunScript);
		_cancelScriptCommand = new RelayCommand(_runScriptCommand.Cancel, () => _runScriptCommand.CanBeCanceled);
		_runScriptCommand.CanExecuteChanged += (sender, args) => _cancelScriptCommand.NotifyCanExecuteChanged();
		_getAvailableSectionsCommand = new AsyncRelayCommand(OnGetAvailableSections);
		_scriptEditorKeyDownCommand = new RelayCommand<KeyEventArgs>(OnScriptEditorKeyDown);
	}

	private void OnScriptEditorKeyDown(KeyEventArgs? args)
	{
		
	}

	private Task OnGetAvailableSections(CancellationToken token)
	{
		return Task.CompletedTask;
	}

	private async Task OnRunScript(CancellationToken token)
	{
		if (Node is null)
		{
			return;
		}
		var middleware = Enumerable.Empty<IPyExecutorMiddleware>();
		await Node.RunScript(ScriptText, middleware, token);
	}
}
