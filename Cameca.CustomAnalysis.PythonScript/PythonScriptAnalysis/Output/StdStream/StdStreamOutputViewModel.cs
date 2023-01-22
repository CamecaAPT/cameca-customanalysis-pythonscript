using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace Cameca.CustomAnalysis.PythonScript.PythonScriptAnalysis.Output.StdStream;

internal class StdStreamOutputViewModel : OutputTabViewModel
{
	public override string Header { get; } = Resources.StdStreamOutputHeader;

	private readonly object _outputItemsLock = new();

	/// <summary>
	/// Collection of strings from redirected stdout and stderr.
	/// Collection is used to support appending text instead of rebuilding immutable single string.
	/// This allows for updates to the displayed without replacement. Text can be selected while more
	/// is added at the same time.
	/// </summary>
	public ObservableCollection<string> OutputItems { get; } = new();

	public ICommand ClearOutputCommand { get; }
	public ICommand StartNewRunCommand { get; }

	private bool _isClearOnRun = true;
	public bool IsClearOnRun
	{
		get => _isClearOnRun;
		set => SetProperty(ref _isClearOnRun, value);
	}

	public StdStreamOutputViewModel()
	{
		ClearOutputCommand = new RelayCommand(ClearOutput);
		StartNewRunCommand = new RelayCommand(StartNewRun);
	}

	private void StartNewRun()
	{
		if (IsClearOnRun)
		{
			ClearOutput();
		}
	}

	private void ClearOutput() => OutputItems.Clear();

	public void DispatchAddOutputItem(string value)
	{
		Application.Current.Dispatcher.BeginInvoke(() =>
		{
			lock (_outputItemsLock)
			{
				OutputItems.Add(value);
			}
		});
	}
}
