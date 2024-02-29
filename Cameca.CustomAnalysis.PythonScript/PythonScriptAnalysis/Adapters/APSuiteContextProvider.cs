using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.PythonScript.Python.DelegatedExecute.Executors;
using Python.Runtime;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Cameca.CustomAnalysis.PythonScript.PythonScriptAnalysis.Adapters;

internal class APSuiteContextProvider : IPyObjectProvider
{
	private readonly IIonData ionData;
	private readonly IIonDisplayInfo? ionDisplayInfo;
	private readonly IMassSpectrumRangeManager? rangeManager;
	private readonly INodeProperties? properties;
	private readonly INodeElementDataSet? nodeElementDataSet;
	private readonly IElementDataSetService elementDataSetService;
	private readonly IReconstructionSections? reconstructionSections;
	private readonly IExperimentInfoResolver? experimentInfoResolver;

	public APSuiteContextProvider(
		IIonData ionData,
		IIonDisplayInfo? ionDisplayInfo,
		IMassSpectrumRangeManager? rangeManager,
		INodeProperties? properties,
		INodeElementDataSet? nodeElementDataSet,
		IElementDataSetService elementDataSetService,
		IReconstructionSections? reconstructionSections,
		IExperimentInfoResolver? experimentInfoResolver)
	{
		this.ionData = ionData;
		this.ionDisplayInfo = ionDisplayInfo;
		this.rangeManager = rangeManager;
		this.properties = properties;
		this.nodeElementDataSet = nodeElementDataSet;
		this.elementDataSetService = elementDataSetService;
		this.reconstructionSections = reconstructionSections;
		this.experimentInfoResolver = experimentInfoResolver;
	}

	public PyObject GetPyObject(PyModule scope)
	{
		var os = scope.Import("os");
		os.GetAttr("environ")["PYTHON_NET_MODE"] = new PyString("CSharp");
		//Environment.SetEnvironmentVariable("PYTHON_NET_MODE", "CSharp");
		var pyapsuite = scope.Import("pyapsuite");
		//var pyapsuite = scope.Import("adapters.apsuite_context");

		// Pass in some functions that requre C# work
		// TODO: Potentially extract to a C# class library and call directly from the script
		var functions = new PyDict();
		functions["ToIntPtr"] = new Func<MemoryHandle, IntPtr>(CSharpFunctions.ToIntPtr).ToPython();
		//functions["SetRanges"] = new Action<IMassSpectrumRangeManager, Dictionary<IonFormula, IonRangeDefinition>>(CSharpFunctions.SetRanges).ToPython();
		functions["SetRanges"] = new Func<IMassSpectrumRangeManager, Dictionary<IonFormula, IonRangeDefinition>, bool>(CSharpFunctions.SetRanges).ToPython();
		
		// Pass in node scope resolved services
		var services = new PyDict();
		services["IIonDisplayInfo"] = ionDisplayInfo.ToPython();
		services["IMassSpectrumRangeManager"] = rangeManager.ToPython();
		services["INodeProperties"] = properties.ToPython();
		services["INodeElementDataSet"] = nodeElementDataSet.ToPython();
		services["IElementDataSetService"] = elementDataSetService.ToPython();
		services["IIonDisplayInfo"] = ionDisplayInfo.ToPython();
		services["IReconstructionSections"] = reconstructionSections.ToPython();
		services["IExperimentInfoResolver"] = experimentInfoResolver.ToPython();

		var context = pyapsuite.InvokeMethod(
			"APSuiteContext",
			ionData.ToPython(),
			services,
			functions);
		return context;
	}
}

internal static class CSharpFunctions
{
	public unsafe static IntPtr ToIntPtr(MemoryHandle handle) => new IntPtr(handle.Pointer);
	
	// TODO: Make SetRanges not async, or figure out a good "Python to async .NET" wrapper (probably not consistantly possible)
	public static bool SetRanges(IMassSpectrumRangeManager rangeManager, Dictionary<IonFormula, IonRangeDefinition> ranges)
	{
		return Application.Current.Dispatcher.Invoke(() => rangeManager.SetRangesSync(ranges));
	}
}
