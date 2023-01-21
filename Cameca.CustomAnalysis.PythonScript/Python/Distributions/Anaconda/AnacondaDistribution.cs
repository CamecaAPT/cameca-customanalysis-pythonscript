using Microsoft.Extensions.Logging;
using Python.Runtime;
using System;
using System.IO;
using System.Linq;
using Prism.Services.Dialogs;
using System.Diagnostics;
using Cameca.CustomAnalysis.PythonScript.Python.Distributions.Anaconda.AnacondaNotFoundDialog;

namespace Cameca.CustomAnalysis.PythonScript.Python.Distributions.Anaconda;

internal class AnacondaDistribution : IPyDistribution
{
	private readonly ILogger<AnacondaDistribution> _logger;
	private readonly AnacondaAutoResolver _autoResolver;
	private readonly IDialogService _dialogService;

	public AnacondaDistribution(ILogger<AnacondaDistribution> logger, AnacondaAutoResolver autoResolver, IDialogService dialogService)
	{
		_logger = logger;
		_autoResolver = autoResolver;
		_dialogService = dialogService;
	}

	/// <summary>
	/// Configure paths for an Anaconda distribution.
	/// If Anaconda cannot be found, prompt for installation.
	/// </summary>
	/// <returns></returns>
	public bool Initialize()
	{
		if (_autoResolver.AutoLocateAnacondaPath() is not { } condaPath)
		{
			ShowPromptForDownload();
			return false;
		}

		// TODO: Prompt for environment selection
		// For now use root env: (base)

		try
		{
			return InitializeAnacondaImpl(condaPath);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, AnacondaResources.LogWarningGeneralInitializeException);
			return false;
		}
	}

	private bool InitializeAnacondaImpl(string condaPath)
	{
		// Locate and set Python DLL using the highest version python.dll found in the anaconda directory
		if (PyPathTools.ResolvePythonDll(condaPath) is not { } dllPath)
		{
			return false;
		}
		Runtime.PythonDLL = dllPath;

		// Create new path environment variable with conda execution paths first

		var condaActivatePaths = new string[]
		{
			condaPath,
			Path.Join(condaPath, "Scripts"),
			Path.Join(condaPath, "Library"),
			Path.Join(condaPath, "bin"),
			Path.Join(condaPath, "Library", "bin"),
			Path.Join(condaPath, "Library", "mingw-w64", "bin"),
		};

		string updatedPath = PrependToPathSeparatedValue(Environment.GetEnvironmentVariable("PATH"), condaActivatePaths);
		Environment.SetEnvironmentVariable("PATH", updatedPath, EnvironmentVariableTarget.Process);
		Environment.SetEnvironmentVariable("PYTHONHOME", condaPath, EnvironmentVariableTarget.Process);

		PythonEngine.PythonHome = condaPath;

		PythonEngine.Initialize();

		// Apply distribution specific adjustments
		ApplyIntelMklFix();

		return true;
	}

	private void ShowPromptForDownload()
	{
		var downloadUrl = AnacondaResources.AnacondaDownloadUrl;
		_dialogService.ShowAnacondaNotFound(downloadUrl, result =>
		{
			if (result.Result == ButtonResult.OK)
			{
				LaunchDownloadUrl(downloadUrl);
			}
		});
	}

	/// <summary>
	/// Execute download URL with ShellExecute to open download link in default browser
	/// </summary>
	/// <param name="downloadUrl"></param>
	private static void LaunchDownloadUrl(string downloadUrl)
	{
		Process.Start(new ProcessStartInfo(downloadUrl)
		{
			UseShellExecute = true,
		});
	}

	// TODO: Remove unsafe and undocumented workaround for mkl libraries though either AP Suite update or out of process Python
	/// <summary>
	/// Add KMP_DUPLICATE_LIB_OK = TRUE to os.environ. Must be called after <see cref="PythonEngine.Initialize()"/>
	/// </summary>
	/// <remarks>
	/// Anaconda by default includes libraries that attempt to load libiomp5md.dll, making this an Anaconda specific adjustment.
	/// 
	/// Solves crash when utilizing some libraries that load Intel MKL
	/// "OMP: Error #15: Initializing libiomp5md.dll, but found libiomp5md.dll already initialized."
	/// According to error message, this workaround is unsafe and not recommended
	/// "As an unsafe, unsupported, undocumented workaround you can set the environment variable KMP_DUPLICATE_LIB_OK=TRUE to allow the program to continue to execute, but that may cause crashes or silently produce incorrect results."
	/// I don't believe this can be resolved without dynamically linking mkl in AP Suite instead of static linking or by running the Python interpreter in a separate process.
	/// </remarks>
	private static void ApplyIntelMklFix()
	{
		using var _ = Py.GIL();
		var os = Py.Import("os");
		os.GetAttr("environ")["KMP_DUPLICATE_LIB_OK"] = new PyString("TRUE");
	}

	private static string PrependToPathSeparatedValue(string? source, params string[] paths)
	{
		// Parse all existing value into individual path components
		var parsedCurPaths = (source?.Split(Path.PathSeparator) ?? Enumerable.Empty<string>())
			.Where(x => !string.IsNullOrEmpty(x))
			.ToList();

		// Parse all new value into individual path components (each entry could have multiple paths itself)
		var parsedNewPaths = paths.SelectMany(pth => pth.Split(Path.PathSeparator))
			.Where(x => !string.IsNullOrEmpty(x))
			.ToList();

		var missingPaths = parsedNewPaths.Except(parsedCurPaths).ToList();

		// Prepend all missing paths to the existing list and rejoin
		return string.Join(Path.PathSeparator, missingPaths.Concat(parsedCurPaths));
	}
}
