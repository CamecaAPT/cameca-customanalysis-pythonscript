using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.PythonScript.Python.DelegatedExecute.Executors;
using Python.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using System.Windows.Media;
using Cameca.CustomAnalysis.Utilities;
using Range = Cameca.CustomAnalysis.Interface.Range;

namespace Cameca.CustomAnalysis.PythonScript.PythonScriptAnalysis.Adapters;

internal class ApsDataObjectProvider : IPyObjectProvider
{
	private readonly IIonData ionData;
	private readonly string[] sections;
	private readonly IIonDisplayInfo? ionDisplayInfo;
	private readonly IMassSpectrumRangeManager? rangeManager;

	public ApsDataObjectProvider(
		IIonData ionData,
		string[] sections,
		IIonDisplayInfo? ionDisplayInfo,
		IMassSpectrumRangeManager? rangeManager)
	{
		this.ionData = ionData;
		this.sections = sections;
		this.ionDisplayInfo = ionDisplayInfo;
		this.rangeManager = rangeManager;
	}

	public PyObject GetPyObject(PyModule scope)
	{
		// Build ion_type_info Python object
		var ion_type_info = new PyList();
		var clrIonTypeRecords = CollectIonTypeInformation(
			ionData,
			ionDisplayInfo,
			rangeManager);
		foreach (var ionTypeInfo in clrIonTypeRecords)
		{
			var entry = new PyDict()
			{
				["name"] = ionTypeInfo.Name.ToPython(),
				["formula"] = FormulaToPyDict(ionTypeInfo.Formula),
				["volume"] = ionTypeInfo.Volume.ToPython(),
				["color"] = ColorToPyTuple(ionTypeInfo.Color),
				["ranges"] = RangeListToPyList(ionTypeInfo.Ranges)
			};
			ion_type_info.Append(entry);
		}

		var np = scope.Import("numpy", "np");

		var aps_ns_dict = new PyDict();

		var iondata_adapter = scope.Import("adapters.iondata");

		var section_data = new PyObject[sections.Length];
		for (int i = 0; i < sections.Length; i++)
		{
			var t = ToDtype(np, ionData.Sections[sections[i]].Type);
			section_data[i] = iondata_adapter.InvokeMethod("prepare_section_data", ionData.ToPython(),
				sections[i].ToPython(), t);
		}

		ulong chunkOffset = 0;
		foreach (var chunk in ionData.CreateSectionDataEnumerable(sections))
		{
			//var pos = chunk.ReadSectionData<Vector3>(IonDataSectionName.Position).Span[3];
			var handles = sections
				.Select(s => chunk.ReadSectionData<byte>(s).Pin())
				.ToArray();

			IntPtr[] memPtrs;
			unsafe
			{
				memPtrs = handles
					.Select(x => new IntPtr(x.Pointer))
					.ToArray();
			}

			var longAddrPtrs = memPtrs.Select(x => x.ToInt64()).ToArray();

			for (int i = 0; i < sections.Length; i++)
			{
				var sectionInfo = ionData.Sections[sections[i]];
				iondata_adapter.InvokeMethod("fill_array",
					section_data[i].ToPython(),
					ToDtype(np, sectionInfo.Type),
					(chunk.Length * sectionInfo.ValuesPerRecord).ToPython(),
					longAddrPtrs[i].ToPython(),
					(chunkOffset * sectionInfo.ValuesPerRecord).ToPython());
			}

			chunkOffset += (ulong)(chunk.Length);
		}

		for (int i = 0; i < sections.Length; i++)
		{
			var valuesPerRecord = ionData.Sections[sections[i]].ValuesPerRecord;
			iondata_adapter.InvokeMethod("reshape_array",
				section_data[i],
				ionData.IonCount.ToPython(),
				valuesPerRecord.ToPython());

			aps_ns_dict[TitleToSnakeCase(sections[i])] = section_data[i];
		}
		var types = scope.Import("types");
		aps_ns_dict["ion_type_info"] = ion_type_info;
		return types.InvokeMethod("SimpleNamespace", Array.Empty<PyObject>(), aps_ns_dict);
	}

	private static readonly Dictionary<Type, string> dtypeMap = new()
	{
		{ typeof(float), "float32" },
		{ typeof(double), "float64" },
		{ typeof(byte), "uint8" },
		{ typeof(short), "int16" },
		{ typeof(int), "int32" },
		{ typeof(long), "int64" },
	};

	private static string TitleToSnakeCase(string titleCase)
	{
		return Regex.Replace(titleCase.Trim(), "(?<!^)(?=[A-Z])", "_").ToLower().Replace(" _", "_").Replace(" ", "_");
	}
	private static PyObject? ToDtype(PyObject np, Type type) => np.GetAttr(dtypeMap[type]);

	private static PyDict FormulaToPyDict(IonFormula formula)
	{
		var dict = new PyDict();
		foreach ((var name, var count) in formula)
		{
			dict[name] = new PyInt(count);
		}
		return dict;
	}

	private static PyObject ColorToPyTuple(Color? color)
	{
		if (!color.HasValue)
			return PyObject.None;
		return new PyTuple(new PyObject[]
		{
			new PyFloat(color.Value.ScR),
			new PyFloat(color.Value.ScG),
			new PyFloat(color.Value.ScB),
			new PyFloat(color.Value.ScA),
		});
	}

	private static PyList RangeListToPyList(List<Range> ranges)
	{
		var pyTupleArray = ranges
			.Select(x => (PyObject)new PyTuple(new PyObject[] { new PyFloat(x.Min), new PyFloat(x.Max) }))
			.ToArray();
		return new PyList(pyTupleArray);
	}

	private record IonTypeInfoClr(
		string Name,
		IonFormula Formula,
		double Volume,
		Color? Color,
		List<Range> Ranges);

	private static List<IonTypeInfoClr> CollectIonTypeInformation(
		IIonData ionData,
		IIonDisplayInfo? ionDisplayInfo,
		IMassSpectrumRangeManager? rangeManager)
	{
		var rangesLookup = rangeManager?.GetRanges() ?? new Dictionary<IonFormula, IonRangeDefinition>();
		var ionTypeRecords = new List<IonTypeInfoClr>();
		foreach (var ionTypeInfo in ionData.Ions)
		{
			var color = ionDisplayInfo?.GetColor(ionTypeInfo);
			var ranges = rangesLookup.TryGetValue(ionTypeInfo.Formula, out var rangeDef)
				? rangeDef.Ranges.ToList()
				: new List<Range>();
			var record = new IonTypeInfoClr(
				ionTypeInfo.Name,
				ionTypeInfo.Formula,
				ionTypeInfo.Volume,
				color,
				ranges);
			ionTypeRecords.Add(record);
		}
		return ionTypeRecords;
	}
}
