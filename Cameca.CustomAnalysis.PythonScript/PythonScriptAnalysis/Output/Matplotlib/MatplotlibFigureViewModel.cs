using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Cameca.CustomAnalysis.PythonScript.Python;
using CommunityToolkit.Mvvm.Input;
using Python.Runtime;

namespace Cameca.CustomAnalysis.PythonScript.PythonScriptAnalysis.Output.Matplotlib;

internal class MatplotlibFigureViewModel : OutputTabViewModel
{
    public override string Header { get; }

    private const int Dpi = 96;

    private readonly PythonManager _pythonManager;
    private readonly byte[] _pickedMatplotlibFigure;

    private ImageSource? _figureImage = null;
    public ImageSource? FigureImage
    {
        get => _figureImage;
        set => SetProperty(ref _figureImage, value);
    }

    public ICommand RenderFigureImageCommand { get; }

    public MatplotlibFigureViewModel(PythonManager pythonManager, string header, byte[] pickedMatplotlibFigure)
    {
        Header = header;
        _pythonManager = pythonManager;
        _pickedMatplotlibFigure = pickedMatplotlibFigure;
        RenderFigureImageCommand = new RelayCommand<Size>(RenderFigureImageForSize);
    }

    private void RenderFigureImageForSize(Size size)
    {
        FigureImage = RenderFigureImage(_pickedMatplotlibFigure, FigureRenderOptions.FromSize(size, Dpi));
    }

    private ImageSource? RenderFigureImage(byte[] serializedFigure, FigureRenderOptions renderOptions)
    {
        if (!_pythonManager.Initialize())
        {
            return null;
        }

        using var gilState = Py.GIL();
        using var scope = Py.CreateScope();

        try
        {
            var figure = LoadFigure(scope, serializedFigure);
            if (figure is null)
            {
                return null;
            }
            return ShapeFigure(scope, figure, renderOptions);
        }
        catch
        {
            return null;
        }
    }
    
    private static PyObject? LoadFigure(PyModule scope, byte[]? serializedFigure)
    {
        if (serializedFigure == null)
        {
            return null;
        }
        var pickle = scope.Import("pickle");
        var bytes = serializedFigure.ToPython();
        return pickle.InvokeMethod("loads", bytes);
    }

    private static ImageSource? ShapeFigure(PyModule scope, PyObject figure, FigureRenderOptions renderOptions)
    {
        figure.InvokeMethod("set_dpi", new PyInt(renderOptions.Dpi));
        figure.InvokeMethod("set_size_inches", new PyFloat(renderOptions.WidthInches), new PyFloat(renderOptions.WidthHeight));

        var io = scope.Import("io");
        var buff = io.InvokeMethod("BytesIO");
        var kwargs = new PyDict();
        kwargs["format"] = new PyString("png");
        figure.InvokeMethod("savefig", new PyObject[] { buff }, kwargs);
        buff.InvokeMethod("seek", 0.ToPython());

        byte[] imgBuffer;
        using (var buffer = buff.InvokeMethod("read").GetBuffer())
        {
            imgBuffer = new byte[buffer.Length];
            buffer.Read(imgBuffer, 0, imgBuffer.Length, 0);
        }

        using var stream = new MemoryStream(imgBuffer);
        var decoder = BitmapDecoder.Create(
            stream,
            BitmapCreateOptions.PreservePixelFormat,
            BitmapCacheOption.OnLoad);
        BitmapSource bitmapSource = decoder.Frames[0];
        bitmapSource.Freeze();
        return bitmapSource;
    }
}
