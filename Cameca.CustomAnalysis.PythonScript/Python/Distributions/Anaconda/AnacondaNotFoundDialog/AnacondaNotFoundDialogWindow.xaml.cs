using System.Windows.Input;
using Prism.Services.Dialogs;

namespace Cameca.CustomAnalysis.PythonScript.Python.Distributions.Anaconda.AnacondaNotFoundDialog;
/// <summary>
/// Interaction logic for AnacondaNotFoundDialogWindow.xaml
/// </summary>
public partial class AnacondaNotFoundDialogWindow : IDialogWindow
{
	public AnacondaNotFoundDialogWindow()
	{
		InitializeComponent();
	}

	public IDialogResult Result { get; set; } = new DialogResult();

	private void OnMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Pressed)
		{
			DragMove();
		}
	}
}
