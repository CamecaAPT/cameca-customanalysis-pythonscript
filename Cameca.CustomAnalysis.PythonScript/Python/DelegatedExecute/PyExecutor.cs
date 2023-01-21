using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Python.Runtime;

namespace Cameca.CustomAnalysis.PythonScript.Python.DelegatedExecute;

internal class PyExecutor
{
	private readonly TimeSpan _cancellationPollInterval = TimeSpan.FromMilliseconds(200);
	private readonly TimeSpan _cancellationTimeout = TimeSpan.FromSeconds(5);

	private readonly PythonManager _pythonManager;

	public PyExecutor(PythonManager pythonManager)
	{
		_pythonManager = pythonManager;
	}

	public async Task Execute(IPyExecutable executable, IEnumerable<IPyExecutorMiddleware> middleware, CancellationToken token)
	{
		if (!_pythonManager.Initialize())
		{
			return;
		}

		var middlewaresList = middleware.ToList();

		using var allowThreads = new PyAllowThreads();
		long pythonThreadId = long.MinValue;

		var task = Task.Run(TaskRunner, token);
		// Inner function that will be run on a separate task for cancellation support
		void TaskRunner()
		{
			using var _ = Py.GIL();
			Interlocked.Exchange(ref pythonThreadId, (long)PythonEngine.GetPythonThreadID());
			using var scope = Py.CreateScope();
			try
			{
				foreach (var item in middlewaresList)
				{
					item.Preprocess(scope, token);
					if (token.IsCancellationRequested)
					{
						return;
					}
				}
				var results = executable.Execute(scope, token);
				if (token.IsCancellationRequested)
				{
					return;
				}

				// Post-process hooks
				// Iterate in reverse order for nested middleware by priority
				for (int i = middlewaresList.Count - 1; i >= 0; i--)
				{
					middlewaresList[i].PostProcess(scope, results, token);
					if (token.IsCancellationRequested)
					{
						return;
					}
				}
			}
			finally
			{
				// Iterate in reverse order for nested middleware by priority
				for (int i = middlewaresList.Count - 1; i >= 0; i--)
				{
					middlewaresList[i].Finalize(scope);
				}
			}
		}

		// Execute with cancellation check polling. Send Python interrupt on cancel
		Task pollTask;
		while (true)
		{
			pollTask = await Task.WhenAny(task, Task.Delay(_cancellationPollInterval, token));

			if (task.IsCompleted)
			{
				break;
			}
			// If cancellation request was triggered, interrupt the running Python thread
			if (token.IsCancellationRequested)
			{
				using (Py.GIL())
				{
					PythonEngine.Interrupt((ulong)Interlocked.Read(ref pythonThreadId));
				}
				break;
			}
		}
		// If task is cancelled and interrupt was sent, then await cleanup and return
		// Do not pass cancellation token to timeout task. Only should complete due to timeout
		// and only relevant after cancellation already has been triggered.
		// ReSharper disable once MethodSupportsCancellation
		var finalizeTask = await Task.WhenAny(pollTask, Task.Delay(_cancellationTimeout));
		await finalizeTask;
	}
}
