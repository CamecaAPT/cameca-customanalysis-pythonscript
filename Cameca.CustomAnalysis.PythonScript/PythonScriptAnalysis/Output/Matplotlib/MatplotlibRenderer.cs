using System;
using System.Threading;
using System.Windows;
using Cameca.CustomAnalysis.PythonScript.Python;
using Cameca.CustomAnalysis.PythonScript.Python.DelegatedExecute;
using Python.Runtime;

namespace Cameca.CustomAnalysis.PythonScript.PythonScriptAnalysis.Output.Matplotlib;

internal class MatplotlibRenderer : IPyExecutorMiddleware
{
    private readonly PythonScriptViewModel _pythonScriptViewModel;
    private readonly PythonManager _pythonManager;

    public MatplotlibRenderer(PythonScriptViewModel pythonScriptViewModel, PythonManager pythonManager)
    {
        _pythonScriptViewModel = pythonScriptViewModel;
        _pythonManager = pythonManager;
    }

    public void Preprocess(PyModule scope, CancellationToken token)
    {
        // Force any use of matplotlib to use Agg backend - interactive backends will break things
        try
        {
            var matplotlib = scope.Import("matplotlib");
            (matplotlib as PyModule)?.Reload();
            matplotlib.InvokeMethod("use", new PyString("Agg"));
        }
        catch { }
    }

    public void PostProcess(PyModule scope, PyObject? results, CancellationToken token)
    {
	    DispatchResetTabs();
        if (token.IsCancellationRequested)
        {
            return;
        }

        MatplotlibFigureViewModel? firstFigure = null;
        var pickle = scope.Import("pickle");
        var plt = scope.Import("matplotlib.pyplot", "plt");
        var fignums = plt.InvokeMethod("get_fignums");
        for (int i = 0; i < fignums.Length(); i++)
        {
            var fignum = fignums[i];
            plt.InvokeMethod("figure", fignum);
            var figure = plt.InvokeMethod("gcf");

            var bytes = pickle.InvokeMethod("dumps", figure);

            var cBytes = StreamedPyToClrBytes(scope, bytes, token: token);
            if (token.IsCancellationRequested) return;

            string title = GetFigureTitle(figure);
            var matplotlibVM = new MatplotlibFigureViewModel(_pythonManager, title, cBytes);
            firstFigure ??= matplotlibVM;
            DispatchAddTab(matplotlibVM);
        }

        plt.InvokeMethod("close", new PyString("all"));
        if (firstFigure is not null)
        {
	        DispatchSelect(firstFigure);
        }
	}

    internal void DispatchAddTab(OutputTabViewModel tabViewModel)
    {
	    Application.Current.Dispatcher.Invoke(() =>
	    {
		    _pythonScriptViewModel.OutputTabs.Add(tabViewModel);
	    });
    }

    internal void DispatchSelect(OutputTabViewModel tabViewModel)
    {
	    Application.Current.Dispatcher.Invoke(() =>
	    {
		    _pythonScriptViewModel.SelectedOutputTab = tabViewModel;
	    });
    }

	/// <summary>
	/// Remove all existing <see cref="MatplotlibFigureViewModel" /> tabs from the output pane
	/// </summary>
	private void DispatchResetTabs()
	{
		Application.Current.Dispatcher.Invoke(() =>
		{
			for (int i = _pythonScriptViewModel.OutputTabs.Count - 1; i >= 0; i--)
			{
				if (IsTypeOrSubclass(_pythonScriptViewModel.OutputTabs[i].GetType(), typeof(MatplotlibFigureViewModel)))
				{
					_pythonScriptViewModel.OutputTabs.RemoveAt(i);
				}
			}
		});
	}

    private static bool IsTypeOrSubclass(Type checkType, Type againstType)
    {
	    return checkType == againstType || checkType.IsSubclassOf(checkType);
    }


	public void Finalize(PyModule scope)
    {
        // Force any use of matplotlib to use Agg backend - interactive backends will break things
        try
        {
            var plt = scope.Import("matplotlib.pyplot", "plt");
            plt.InvokeMethod("close", new PyString("all"));
        }
        catch { }
    }

    private static byte[] StreamedPyToClrBytes(PyModule scope, PyObject bytes, int chunkSize = 4000, CancellationToken token = default)
    {
        if (token.IsCancellationRequested) return Array.Empty<byte>();

        var clrBytes = new byte[bytes.Length()];

        var io = scope.Import("io");

        var bytes_io = io.InvokeMethod("BytesIO", bytes);
        var chunk_size = new PyInt(chunkSize);

        long offset = 0L;

        while (true)
        {
            var chunk = bytes_io.InvokeMethod("read", chunk_size);
            var readChunkSize = chunk.Length();
            if (readChunkSize == 0)
                break;
            var chunkManaged = chunk.As<byte[]>();
            if (token.IsCancellationRequested)
                break;
            chunkManaged.CopyTo(clrBytes, offset);
            offset += chunkManaged.Length;
        }
        return clrBytes;
    }

    private static string GetFigureTitle(PyObject figure)
    {
        try
        {
            return $"Figure {figure.GetAttr("number").As<int>()}";
        }
        catch
        {
            return "Figure";
        }
    }
}
