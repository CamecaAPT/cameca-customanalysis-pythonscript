using System;
using Prism.Services.Dialogs;

namespace Cameca.CustomAnalysis.PythonScript.Python.Distributions.Anaconda.AnacondaNotFoundDialog;

internal static class AnacondaNotFoundDialogServiceExtensions
{
	public static void ShowAnacondaNotFound(this IDialogService dialogService, string url, Action<IDialogResult> callback)
	{
		dialogService.ShowDialog(
			nameof(AnacondaNotFoundDialogView),
			new DialogParameters
			{
				{ "DownloadUrl", url },
			},
			callback,
			nameof(AnacondaNotFoundDialogWindow));
	}
}
