using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Cameca.CustomAnalysis.PythonScript.PythonScriptAnalysis.Output.Matplotlib;

internal class SizeChangedToSizeConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is not SizeChangedEventArgs eventArgs)
		{
			return DependencyProperty.UnsetValue;
		}
		return eventArgs.NewSize;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
