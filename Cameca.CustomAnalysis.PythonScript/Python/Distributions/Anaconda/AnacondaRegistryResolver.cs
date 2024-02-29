using Microsoft.Win32;
using System.IO;

namespace Cameca.CustomAnalysis.PythonScript.Python.Distributions.Anaconda;

/// <summary>
/// Identify Anaconda installation path from expected registry structure
/// </summary>
internal class AnacondaRegistryResolver
{
	// Registry key expected keys and values
	private const string ContinuumAnalyticsPath = "SOFTWARE\\Python\\ContinuumAnalytics";
	private const string SysArchitectureKey = "SysArchitecture";
	private const string SysArchitectureExpectedValue = "64bit";
	private const string SysVersionKey = "SysVersion";
	private const string SysVersionDefaultValue = "0";
	private const string InstallPathKey = "InstallPath";
	private const string DefaultKey = "";  // "(Default)"

	private static readonly RegistryKey RegistryBaseKey = Registry.CurrentUser;

	public string? CheckAnacondaLocationRegistry()
	{
		using var key = RegistryBaseKey.OpenSubKey(ContinuumAnalyticsPath);
		if (key is null) return null;
		float bestVersion = 0;
		string? selectedInstallKeyPath = null;
		foreach (var subkeyName in key.GetSubKeyNames())
		{
			using var subkey = key.OpenSubKey(subkeyName);
			if (subkey is null) continue;
			if (subkey.GetValue(SysArchitectureKey) as string != SysArchitectureExpectedValue) continue;

			var rawSysVersion = subkey.GetValue(SysVersionKey, SysVersionDefaultValue) as string;
			if (!float.TryParse(rawSysVersion, out float version))
				continue;
			if (version > bestVersion)
			{
				bestVersion = version;
				selectedInstallKeyPath = string.Join(Path.DirectorySeparatorChar, ContinuumAnalyticsPath, subkeyName, InstallPathKey);
			}
		}

		if (selectedInstallKeyPath is null) return null;

		using var installSubKey = RegistryBaseKey.OpenSubKey(selectedInstallKeyPath);
		return installSubKey?.GetValue(DefaultKey) as string;
	}
}
