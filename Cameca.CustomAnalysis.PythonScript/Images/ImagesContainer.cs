using System;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Cameca.CustomAnalysis.PythonScript.Images;

internal static class ImagesContainer
{
    private const string Python16x16Path = $"Images/python-16x16.png";

    public static ImageSource Python16x16 { get; } = CreateFromResourcePath(Python16x16Path);

    /// <summary>
    /// A small helper to create an <see cref="ImageSource"/> from an assembly resource
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private static ImageSource CreateFromResourcePath(string path)
    {
        var assembly = Assembly.GetCallingAssembly();
        var uri = new Uri($"pack://application:,,,/{assembly.GetName().Name};component/{path}");
        var image = new BitmapImage(uri);
        image.Freeze();
        return image;
    }
}
