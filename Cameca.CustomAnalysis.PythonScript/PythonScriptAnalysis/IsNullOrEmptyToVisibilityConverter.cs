using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace Cameca.CustomAnalysis.PythonScript.PythonScriptAnalysis;

internal class IsNullOrEmptyToVisibilityConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is not string strValue) return DependencyProperty.UnsetValue;
		return string.IsNullOrEmpty(strValue) ? Visibility.Visible : Visibility.Collapsed;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
