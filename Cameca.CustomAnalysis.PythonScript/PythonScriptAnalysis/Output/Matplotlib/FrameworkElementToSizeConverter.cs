using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Cameca.CustomAnalysis.PythonScript.PythonScriptAnalysis.Output.Matplotlib;

internal class FrameworkElementToSizeConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is not FrameworkElement frameworkElement)
		{
			return DependencyProperty.UnsetValue;
		}

		return new Size(frameworkElement.ActualWidth, frameworkElement.ActualHeight);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
