using System;
using System.Collections.Generic;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;

namespace Cameca.CustomAnalysis.PythonScript.PythonScriptAnalysis;

/// <summary>
/// Allows producing tab based foldings
/// </summary>
internal class TabFoldingStrategy
{
	internal class TabIndent
	{
		public int IndentSize;
		public int LineStart;
		public int LineEnd;
		public int StartOffset => LineStart + IndentSize - 1;
		public int TextLength => LineEnd - StartOffset;

		public TabIndent(int i_indentSize, int i_lineStart, int i_lineEnd)
		{
			IndentSize = i_indentSize;
			LineStart = i_lineStart;
			LineEnd = i_lineEnd;
		}
	}

	/// <summary>
	/// Creates a new TabFoldingStrategy.
	/// </summary>
	public TabFoldingStrategy()
	{

	}

	public void UpdateFoldings(FoldingManager manager, TextDocument document)
	{
		int firstErrorOffset;
		IEnumerable<NewFolding> foldings = CreateNewFoldings(document, out firstErrorOffset);
		manager.UpdateFoldings(foldings, firstErrorOffset);
	}

	/// <summary>
	/// Create <see cref="NewFolding"/>s for the specified document.
	/// </summary>
	public IEnumerable<NewFolding> CreateNewFoldings(TextDocument document, out int firstErrorOffset)
	{
		firstErrorOffset = -1;
		return CreateNewFoldings(document);
	}

	/// <summary>
	/// Create <see cref="NewFolding"/>s for the specified document.
	/// </summary>
	public IEnumerable<NewFolding> CreateNewFoldings(TextDocument document)
	{
		List<NewFolding> newFoldings = new List<NewFolding>();

		int documentIndent = 0;
		List<TabIndent> tabIndents = new List<TabIndent>();
		DocumentLine? lastNonEmptyLine = null;
		foreach (DocumentLine line in document.Lines)
		{
			if (line.Length == 0)
			{
				continue;
			}

			int lineIndent = 0;
			for (int i = line.Offset; i < line.EndOffset; i++)
			{
				char c = document.GetCharAt(i);
				if (c is '\t' or ' ')
				{
					lineIndent++;
				}
				else
				{
					break;
				}
			}
			if (lineIndent > documentIndent)
			{
				tabIndents.Add(new TabIndent(lineIndent, line.PreviousLine.Offset, line.PreviousLine.EndOffset));
			}
			else if (lineIndent < documentIndent && lastNonEmptyLine is not null)
			{
				List<TabIndent> closedIndents = tabIndents.FindAll(x => x.IndentSize > lineIndent);
				closedIndents.ForEach(x =>
				{
					newFoldings.Add(new NewFolding(x.StartOffset, lastNonEmptyLine.EndOffset)
					{
						Name = document.GetText(x.StartOffset, x.TextLength)
					});
					tabIndents.Remove(x);
				});
			}
			documentIndent = lineIndent;
			lastNonEmptyLine = line;
		}
		tabIndents.ForEach(x =>
		{
			newFoldings.Add(new NewFolding(x.StartOffset, document.TextLength));
		});

		newFoldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
		return newFoldings;
	}
}
