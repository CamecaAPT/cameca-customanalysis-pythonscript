using Cameca.CustomAnalysis.PythonScript.Python.Distributions.Anaconda.AnacondaNotFoundDialog;
using Prism.Ioc;

namespace Cameca.CustomAnalysis.PythonScript.Python.Distributions.Anaconda;

internal static class AnacondaDistributionContainerExtensions
{
	public static IContainerRegistry RegisterAnacondaDistribution(this IContainerRegistry registry)
	{
		registry.RegisterDialogWindow<AnacondaNotFoundDialogWindow>(nameof(AnacondaNotFoundDialogWindow));
		registry.RegisterDialog<AnacondaNotFoundDialogView, AnacondaNotFoundDialogViewModel>();
		registry.RegisterSingleton<AnacondaRegistryResolver>();
		registry.RegisterSingleton<AnacondaAutoResolver>();
		registry.RegisterSingleton<IPyDistribution, AnacondaDistribution>(nameof(AnacondaDistribution));
		return registry;
	}
}
