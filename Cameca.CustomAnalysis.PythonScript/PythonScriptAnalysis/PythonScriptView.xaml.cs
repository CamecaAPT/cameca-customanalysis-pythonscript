using System.Windows;
using ICSharpCode.AvalonEdit.Folding;

namespace Cameca.CustomAnalysis.PythonScript.PythonScriptAnalysis;

/// <summary>
/// Interaction logic for PythonScriptView.xaml
/// </summary>
internal partial class PythonScriptView
{
    public PythonScriptView()
    {
        InitializeComponent();

        Loaded += OnLoaded;
	}

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
	    Loaded -= OnLoaded;

	    var textEditor = ScriptEditorTextBox;
	    var foldingManager = FoldingManager.Install(textEditor.TextArea);
	    var foldingStrategy = new TabFoldingStrategy();
	    foldingStrategy.UpdateFoldings(foldingManager, textEditor.Document);
	    textEditor.Document.TextChanged += (o, args) => foldingStrategy.UpdateFoldings(foldingManager, textEditor.Document);
	}
}
