namespace Cameca.CustomAnalysis.PythonScript.Python;

/// <summary>
/// Implements high-level Python.NET logic for a specific Python distribution
/// </summary>
/// <remarks>
/// Example Python distributions may include CPython, Anaconda, PyPy, etc.
/// Any Python installation that requires specific logic for resolving core Python file paths
/// should have an associated implementation
/// </remarks>
internal interface IPyDistribution
{
	/// <summary>
	/// Initialize the Python distribution
	/// </summary>
	/// <remarks>
	/// Implementations should ensure that at minimum Runtime.PythonDLL is set and PythonEngine.Initialize is called.
	/// Other distribution specific configurations such as setting PythonEngine.PythonHome or manually editing core
	/// Python settings should be performed as needed
	/// </remarks>
	/// <returns></returns>
	bool Initialize();
}
