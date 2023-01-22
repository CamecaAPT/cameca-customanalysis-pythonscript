using System.Windows;

namespace Cameca.CustomAnalysis.PythonScript.PythonScriptAnalysis.Output.Matplotlib;

internal class FigureRenderOptions
{
	public double Width { get; }
	public double Height { get; }
	public int Dpi { get; }
	public double WidthInches { get; }
	public double WidthHeight { get; }

	public FigureRenderOptions(double width, double height, int dpi)
	{
		Width = width;
		Height = height;
		Dpi = dpi;
		WidthInches = Width / Dpi;
		WidthHeight = Height / Dpi;
	}

	public static FigureRenderOptions FromSize(Size size, int dpi)
	{
		return new FigureRenderOptions(size.Width, size.Height, dpi);

	}

	public static FigureRenderOptions FromFrameworkElement(FrameworkElement frameworkElement, int dpi)
	{
		return new FigureRenderOptions(frameworkElement.ActualWidth, frameworkElement.ActualHeight, dpi);
	}
}
