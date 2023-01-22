using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.PythonScript.Python;
using Cameca.CustomAnalysis.PythonScript.Python.DelegatedExecute;
using Cameca.CustomAnalysis.PythonScript.PythonScriptAnalysis.Adapters;
using Cameca.CustomAnalysis.PythonScript.PythonScriptAnalysis.Output.Matplotlib;
using Cameca.CustomAnalysis.PythonScript.PythonScriptAnalysis.Output.StdStream;
using Cameca.CustomAnalysis.Utilities;
using CommunityToolkit.Mvvm.Input;
using Python.Runtime;

namespace Cameca.CustomAnalysis.PythonScript.PythonScriptAnalysis;

internal class PythonScriptViewModel : AnalysisViewModelBase<PythonScriptNode>
{
	public const string UniqueId = "Cameca.CustomAnalysis.PythonScript.PythonScriptViewModel";

	private readonly PythonManager _pythonManager;
	private readonly StdStreamOutputViewModel _outputViewModel;
	private readonly IViewModelCaptionProvider _viewModelCaptionProvider;
	private IViewModelCaption? _viewModelCaption;

	private string _title = "";
	public string Title
	{
		get => _title;
		set => SetProperty(ref _title, value, OnTitleChanged);
	}

	private string _scriptText = "";
	public string ScriptText
	{
		get => _scriptText;
		set => SetProperty(ref _scriptText, value, OnScriptTextChanged);
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

	public PythonScriptViewModel(PythonManager pythonManager, StdStreamOutputViewModel outputViewModel, IViewModelCaptionProvider viewModelCaptionProvider, IAnalysisViewModelBaseServices services)
		: base(services)
	{
		_pythonManager = pythonManager;
		_outputViewModel = outputViewModel;
		_viewModelCaptionProvider = viewModelCaptionProvider;
		_runScriptCommand = new AsyncRelayCommand(OnRunScript);
		_cancelScriptCommand = new RelayCommand(_runScriptCommand.Cancel, () => _runScriptCommand.CanBeCanceled);
		_runScriptCommand.CanExecuteChanged += (sender, args) => _cancelScriptCommand.NotifyCanExecuteChanged();
		_getAvailableSectionsCommand = new AsyncRelayCommand(OnGetAvailableSections);
		_scriptEditorKeyDownCommand = new RelayCommand<KeyEventArgs>(OnScriptEditorKeyDown);

		MenuItems.CollectionChanged += MenuItemsOnCollectionChanged;

		OutputTabs.Add(_outputViewModel);
		SelectedOutputTab = _outputViewModel;
	}

	private void MenuItemsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		foreach (var vm in e.NewItems?.OfType<MenuItemViewModel>() ?? Enumerable.Empty<MenuItemViewModel>())
		{
			vm.PropertyChanged += MenuItemViewModelOnPropertyChanged;
		}
	}

	private void MenuItemViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (sender is not MenuItemViewModel menuItemViewModel || Node is null)
		{
			return;
		}

		if (e.PropertyName is nameof(MenuItemViewModel.IsChecked))
		{
			if (menuItemViewModel.IsChecked && !Node.SelectedSection.Contains(menuItemViewModel.Header))
			{
				Node.SelectedSection.Add(menuItemViewModel.Header);
			}
			else if (!menuItemViewModel.IsChecked && Node.SelectedSection.Contains(menuItemViewModel.Header))
			{
				Node.SelectedSection.Remove(menuItemViewModel.Header);
			}
		}
	}

	protected override void OnAdded(ViewModelAddedEventArgs eventArgs)
	{
		_viewModelCaption = _viewModelCaptionProvider.Resolve(InstanceId);

		Title = Node?.Title ?? PythonScriptNode.DisplayInfo.Title;
		ScriptText = Node?.ScriptText ?? "";

		var selectedSection = new HashSet<string>(Node?.SelectedSection ?? Enumerable.Empty<string>());
		foreach (var section in Node?.GetCurrentSections() ?? PythonScriptNode.DefaultSections)
		{
			MenuItems.Add(new MenuItemViewModel
			{
				Header = section,
				IsChecked = selectedSection.Contains(section),
			});
		}
	}

	private void OnTitleChanged()
	{
		if (_viewModelCaption is null)
		{
			return;
		}

		_viewModelCaption.Caption = string.IsNullOrWhiteSpace(Title)
			? PythonScriptNode.DisplayInfo.Title
			: Title.Trim();
		Node?.UpdateTitle(_viewModelCaption.Caption);
	}

	private void OnScriptTextChanged()
	{
		if (Node is not null)
		{
			Node.ScriptText = ScriptText;
		}
	}

	private void OnScriptEditorKeyDown(KeyEventArgs? args)
	{
		if (args is null) return;

		if (args.Key == Key.Enter
			&& args.KeyboardDevice.Modifiers == ModifierKeys.Control
			&& !args.IsRepeat
			&& _runScriptCommand.CanExecute(null))
		{
			_runScriptCommand.Execute(null);
			args.Handled = true;
		}

		if (args.Key == Key.Pause
			&& !args.IsRepeat
			&& _cancelScriptCommand.CanExecute(null))
		{
			_cancelScriptCommand.Execute(null);
			args.Handled = true;
		}
	}

	private async Task OnGetAvailableSections(CancellationToken token)
	{
		if (Node is null) return;
		var availableSections = await Node.GetAvailableSections();
		var current = new HashSet<string>(MenuItems.Select(x => x.Header));
		foreach (var name in availableSections)
		{
			if (!current.Contains(name))
			{
				MenuItems.Add(new MenuItemViewModel
				{
					Header = name,
					IsChecked = false,
				});
			}
		}
	}

	private async Task OnRunScript(CancellationToken token)
	{
		if (Node is null)
		{
			return;
		}
		_outputViewModel.StartNewRunCommand.Execute(null);
		var middleware = new IPyExecutorMiddleware[]
		{
			new StdstreamRedirect(DispatchAddOutputItemPyCallback),
			new MatplotlibRenderer(this, _pythonManager),
		};
		try
		{
			await Node.RunScript(
				ScriptText,
				MenuItems.Where(x => x.IsChecked).Select(x => x.Header).ToArray(),
				middleware,
				token);
		}
		catch (PythonException)
		{
			// Expected when internal Python exception is raised
		}
	}

	private void DispatchAddOutputItemPyCallback(object? value)
	{
		_outputViewModel.DispatchAddOutputItem(value?.ToString() ?? "");
	}
}
