﻿using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.PythonScript.Python;
using Cameca.CustomAnalysis.PythonScript.Python.Distributions.Anaconda;
using Cameca.CustomAnalysis.PythonScript.PythonExampleAnalysis;
using Cameca.CustomAnalysis.PythonScript.PythonScriptAnalysis;
using Cameca.CustomAnalysis.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Prism.Ioc;
using Prism.Modularity;

namespace Cameca.CustomAnalysis.PythonScript;

/// <summary>
/// Public <see cref="IModule"/> implementation is the entry point for AP Suite to discover and configure the custom analysis
/// </summary>
public class PythonScriptModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
	    containerRegistry.Register(typeof(ILogger<>), typeof(NullLogger<>));
	    containerRegistry.RegisterSingleton<PythonManager>();
	    containerRegistry.RegisterAnacondaDistribution();

		containerRegistry.AddCustomAnalysisUtilities(options => options.UseStandardBaseClasses = true);

        containerRegistry.Register<object, PythonScriptNode>(PythonScriptNode.UniqueId);
        containerRegistry.RegisterInstance(PythonScriptNode.DisplayInfo, PythonScriptNode.UniqueId);
        containerRegistry.Register<IAnalysisMenuFactory, PythonScriptNodeMenuFactory>(nameof(PythonScriptNodeMenuFactory));
        containerRegistry.Register<object, PythonScriptViewModel>(PythonScriptViewModel.UniqueId);

		containerRegistry.Register<object, ExampleNode>(ExampleNode.UniqueId);
		containerRegistry.RegisterInstance(ExampleNode.DisplayInfo, ExampleNode.UniqueId);
		containerRegistry.Register<IAnalysisMenuFactory, ExampleNodeMenuFactory>(nameof(ExampleNodeMenuFactory));
	}

	public void OnInitialized(IContainerProvider containerProvider)
    {
        var extensionRegistry = containerProvider.Resolve<IExtensionRegistry>();

        extensionRegistry.RegisterAnalysisView<PythonScriptView, PythonScriptViewModel>(AnalysisViewLocation.Default);
    }
}
