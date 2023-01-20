using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Cameca.CustomAnalysis.PythonScript.Python;

internal static class PyPathTools
{
	/// <summary>
	/// In the given directory, find latest python dll with full version name (i.e. prefer python39.dll over python38.dll, and prefer python39.dll over python3.dll)
	/// Assume all files in the directory match that pattern python\d{1,2}.dll
	/// </summary>
	/// <param name="directory"></param>
	/// <returns>Absolute path of highest version python*.dll if found, else <c>null</c></returns>
	public static string? ResolvePythonDll(string directory)
	{
		var initialCandidates = Directory.EnumerateFiles(directory, "python*.dll", SearchOption.TopDirectoryOnly).Select(f => new FileInfo(f));
		int bestCandidateVersion = 0;
		FileInfo? bestCandidate = null;
		foreach (var candidate in initialCandidates)
		{
			if (Regex.Match(candidate.Name, @"python(?<Version>\d{1,2}).dll", RegexOptions.IgnoreCase) is not { Success: true } match) continue;
			if (!int.TryParse(match.Groups["Version"].Value, out int version)) continue;
			if (version > bestCandidateVersion)
			{
				bestCandidateVersion = version;
				bestCandidate = candidate;
			}
		}
		return bestCandidate?.FullName;
	}
}
