using CSharpCodeAnalyzer.Helpers;
using CSharpCodeAnalyzer.Models.Base;
using CSharpCodeAnalyzer.Models.Elements;
using CSharpCodeAnalyzer.Models.Inners;

namespace CSharpCodeAnalyzer.Services;

public class CSharpParser
{
	public Dictionary<string, int> Order { get; set; } = new();

	public void WriteToFilesWithExample(List<ClassElement> classes, string examplePath)
	{
		var example = ReadFromFileByRegions(examplePath);
		var directoryName = "TestMovedClasses";
		if (Directory.Exists(directoryName))
			Directory.Delete(directoryName, true);
		Directory.CreateDirectory(directoryName);

		foreach (var cls in classes)
		{
			var content = ReworkFileWithExample(cls, example);
			var filepath = $"{directoryName}/{cls.FileName}";
			using (var file = File.CreateText(filepath))
			{
				file.Write(content);
			}
		}
	}
	public void WriteToFiles(List<ClassElement> classes)
	{
		var directoryName = "TestMovedClasses";
		if (Directory.Exists(directoryName))
			Directory.Delete(directoryName, true);
		Directory.CreateDirectory(directoryName);

		foreach (var cls in classes)
		{
			var content = ReworkFile(cls);
			var filepath = $"{directoryName}/{cls.FileName}";
			using (var file = File.CreateText(filepath))
			{
				file.Write(content);
			}
		}
	}

	public string ReworkFile(ClassElement cls)
	{
		var topFile = JoinManager.JoinLines(cls.FileLines[..cls.ContentStart]);
		var bottomFile = JoinManager.JoinLines(cls.FileLines[(cls.ContentEnd + 1)..]);

		var content = string.Join("\n\n", cls.Elements.Select(x => x.Content.Content));
		return $"{topFile}\n\t{{\n{content}\n\t}}\n{bottomFile}";
	}

	public string ReworkFileWithExample(ClassElement cls, Dictionary<string, List<CodeElementBase>> example)
	{
		var topFile = JoinManager.JoinLines(cls.FileLines[..cls.ContentStart]);
		var bottomFile = JoinManager.JoinLines(cls.FileLines[(cls.ContentEnd + 1)..]);
		var result = new Dictionary<string, List<CodeElementBase>>();

		foreach (var ex in example)
		{
			var exValues = ex.Value.Select(x => x.Title).ToList();
			var elements = new List<CodeElementBase>();

			elements = cls.Elements
				.Where(x => exValues.Contains(x.Title))
				.ToList();

			if (elements.Count > 0)
				result.Add(ex.Key, elements);
		}

		var other = cls.Elements
			.Where(x =>
				!result.SelectMany(x => x.Value).Any(y => x.Title == y.Title))
			.ToList();
		var properties = other.Where(x => x is PropertyElement).ToList();
		var methods = other.Where(x => x is MethodElement).ToList();
		var constructors = other.Where(x => x is ConstructorElement).ToList();

		AddToDictionary(result, "Constructors", constructors);
		AddToDictionary(result, "Fields", properties);

		if (methods.Count != 0)
		{
			var mtInitUpdate = methods
				.Where(x => x.Title.StartsWith("Initial_Update")
					|| x.Title.StartsWith("InitialUpdate")
					|| (x.Title.Contains("Init") && x.Title.Contains("Tab")))
				.ToList();
			var mtAuth = methods.Except(mtInitUpdate)
				.Where(x => x.Title.Contains("Account")
					|| x.Title.Contains("Auth")
					|| x.Title.Contains("Init"))
				.ToList();
			var mtOpen = methods.Except(mtInitUpdate).Except(mtAuth)
				.Where(x => x.Title.Contains("Open"))
				.ToList();
			var mtCommand = methods.Except(mtInitUpdate).Except(mtAuth).Except(mtOpen)
				.Where(x => x.Title.Contains("Command"))
				.ToList();
			var mtInner = methods
				.Except(mtInitUpdate)
				.Except(mtAuth)
				.Except(mtOpen)
				.Except(mtCommand)
				.ToList();

			AddToDictionary(result, "AuthMethods", mtAuth);
			AddToDictionary(result, "InitialUpdate", mtInitUpdate);
			AddToDictionary(result, "OpeningMethods", mtOpen);
			AddToDictionary(result, "Commands", mtCommand);
			AddToDictionary(result, "InnerMethods", mtInner);
		}

		var result2 = result
			.OrderBy(x => Order[x.Key])
			.Select(x => $"\t\t#region {x.Key}\n\n"
				+ JoinManager.JoinElements(x.Value)
				+ $"\n\n\t\t#endregion {x.Key}")
			.ToList();

		var content = string.Join("\n\n", result2);
		return $"{topFile}\n\t{{\n{content}\n\t}}\n{bottomFile}";
	}

	public List<ClassElement> ReadFromFolder(string path, SearchOption search = SearchOption.TopDirectoryOnly)
	{
		var files = Directory.GetFiles(path, "*.cs", search);
		Console.WriteLine($"{files.Length}");

		var result = files
			.Select(x => ReadFromFile(x))
			.Where(x => x != null)
			.ToList();
		return result;
	}

	public ClassElement ReadFromFile(string path)
	{
		var lines = File.ReadAllLines(path);
		var result = GetClass(lines, Path.GetFileNameWithoutExtension(path));
		if (result == null) return null;

		result.FileName = Path.GetFileName(path);
		result.FilePath = path;
		return result;
	}

	public Dictionary<string, List<CodeElementBase>> ReadFromFileByRegions(string path)
	{
		var name = "SectionViewModelBase";
		var lines = File.ReadAllLines(path);
		var result = new Dictionary<string, List<CodeElementBase>>();
		var start = 0;
		var i = 1;

		while (true)
		{
			var content = GetContentPart(lines, start, "#region", "#endregion", false);
			if (!content.Success) break;

			var title = content.Content[0].Replace("#region", "").Trim();
			var elements = new List<CodeElementBase>();
			start = content.EndLine + 1;

			elements.AddRange(GetConstructors(content.Content, name));
			elements.AddRange(GetProperties(content.Content));
			elements.AddRange(GetMethods(content.Content, name));

			result.Add(title, elements);
			Order.Add(title, i++);
		}

		return result;
	}

	public ClassElement GetClass(string[] lines, string className)
	{
		var starts = new string[] { "private", "internal", "public", "class" };

		var classes = new List<ClassElement>();
		for (int i = 0; i < lines.Length; ++i)
		{
			var topline = i;
			var botline = i;
			var line = lines[i].Trim();

			if (line.StartsWith(starts))
			{
				var data = JoinManager.SplitByElements(line);
				var newClass = new ClassElement();

				if (data[0] == "class")
				{
					newClass.AccessModifier = "private";
					newClass.Title = data[1];
				}
				else if (data[1] == "class")
				{
					newClass.AccessModifier = data[0];
					newClass.Title = data[2];
				}
				else if (data[1] == "sealed")
				{
					newClass.AccessModifier = data[0];
					newClass.Title = data[3];
				}
				else
				{
					continue;
				}

				var content = GetContentPart(lines, i);
				botline = content.EndLine;
				if (!content.Success) return new();

				newClass.Content = new()
				{
					Header = lines[i],
					Body = content.ToString(),
					StartLine = i,
					EndLine = content.EndLine,
					Content = lines.JoinLines(topline, botline)
				};

				newClass.Constructors = GetConstructors(content.Content, newClass.Title);
				newClass.Properties = GetProperties(content.Content);
				newClass.Methods = GetMethods(content.Content, newClass.Title);
				newClass.FileLines = lines;
				newClass.ContentStart = content.StartLine;
				newClass.ContentEnd = content.EndLine;

				classes.Add(newClass);
				i = content.EndLine;
			}
		}

		if (classes.Count == 1)
			return classes.First();

		return classes.FirstOrDefault(x => x.Title == className);
	}

	public List<ConstructorElement> GetConstructors(string[] lines, string className)
	{
		var result = new List<ConstructorElement>();
		var starts = new string[] { "public", "private", "internal", className };

		for (int i = 0; i < lines.Length; ++i)
		{
			var line = lines[i].Trim();
			if (line.StartsWith(starts))
			{
				if (GetArgsBeforeMethod(line, out var data) < 2)
					continue;

				var constructors = new ConstructorElement();

				if (data[0] == className)
				{
					constructors.AccessModifier = "private";
					constructors.Title = data[0];
				}
				else if (data[1] == className)
				{
					constructors.AccessModifier = data[0];
					constructors.Title = data[1];
				}
				else
				{
					continue;
				}

				int topline = i;
				int botline = i;

				var headerArgs = GetContentPart(lines, topline, "(", ")");
				if (!headerArgs.Success) continue;
				botline = headerArgs.EndLine;

				var content = GetContentPart(lines, i, "{", "}");
				if (!content.Success)
				{
					content = GetContentPart(lines, i, "=>", ";");
					if (!content.Success) continue;
				}
				botline = content.EndLine;

				var attributes = GetAttributes(lines, i);
				if (attributes.Success)
					topline = attributes.StartLine;

				var comment = GetComment(lines, i);
				if (comment.Success)
					topline = comment.StartLine;

				constructors.Content = new()
				{
					Comment = comment.ToString(),
					Attributes = attributes.ToString(),
					Header = headerArgs.ToString(),
					Body = content.ToString(),
					Content = lines.JoinLines(topline, botline),
				};

				result.Add(constructors);
				i = content.EndLine;
			}
		}

		return result;
	}

	public List<MethodElement> GetMethods(string[] lines, string className)
	{
		var result = new List<MethodElement>();
		var starts = new string[] { "public", "private", "protected" };

		for (int i = 0; i < lines.Length; ++i)
		{
			var line = lines[i].Trim();
			if (line.StartsWith(starts) && !line.Contains(className))
			{
				int topline = i;
				int botline = i;

				var headerArgs = GetContentPart(lines, topline, "(", ")");
				if (!line.Contains("(") || !headerArgs.Success) continue;
				botline = headerArgs.EndLine;

				var data = JoinManager.SplitByElements(lines[topline][0..headerArgs.StartIndex]);
				var method = new MethodElement();
				method.AccessModifier = data.First();
				method.Title = data.Last();

				var content = GetContentPart(lines, botline, "{", "}");
				if (!content.Success)
				{
					content = GetContentPart(lines, botline, "=>", ";");
					if (!content.Success) continue;
				}
				botline = content.EndLine;

				var attributes = GetAttributes(lines, topline);
				if (attributes.Success)
					topline = attributes.StartLine;

				var comment = GetComment(lines, topline);
				if (comment.Success)
					topline = comment.StartLine;

				method.Content = new()
				{
					Comment = comment.ToString(),
					Attributes = attributes.ToString(),
					Header = headerArgs.ToString(),
					Body = content.ToString(),
					Content = lines.JoinLines(topline, botline),
				};

				result.Add(method);
				i = content.EndLine;
			}
		}

		return result;
	}

	public List<PropertyElement> GetProperties(string[] lines)
	{
		var result = new List<PropertyElement>();
		var starts = new string[] { "public", "private", "protected" };

		for (int i = 0; i < lines.Length; ++i)
		{
			var line = lines[i].Trim();
			if (line.StartsWith("["))
			{
				line = line.ReadFrom("]").Trim();
			}

			if (line.StartsWith(starts))
			{
				int topline = i;
				int botline = i;
				var property = new PropertyElement();
				ContentPartModel content;

				if (line.Contains("(")) continue;

				if (line.EndsWith(";"))
				{
					content = new()
					{
						Success = true,
						StartLine = i,
						EndLine = i,
						FullContent = lines
					};
				}
				else
				{
					content = GetContentPart(lines, i, "=>", ";");
					if (!content.Success)
					{
						content = GetContentPart(lines, i, "=", ";");
						if (!content.Success)
							content = GetContentPart(lines, i);
					}
					if (!content.Success)
						continue;

					botline = content.EndLine;
				}

				var data = ClearPropertyLine(line).SplitByElements();
				var attributes = GetAttributes(lines, topline);
				if (attributes.Success)
					topline = attributes.StartLine;

				var comment = GetComment(lines, topline);
				if (comment.Success)
					topline = comment.StartLine;

				property.AccessModifier = data.First();
				property.Title = data.Last();

				property.Content = new()
				{
					Comment = comment.ToString(),
					Attributes = attributes.ToString(),
					Header = content.ToString(),
					Body = content.ToString(),
					Content = lines.JoinLines(topline, botline),
				};

				result.Add(property);
				i = content.EndLine;
			}
		}

		return result;
	}

	#region Inner

	private void AddToDictionary(Dictionary<string, List<CodeElementBase>> result, string title, List<CodeElementBase> elements)
	{
		if (elements.Count != 0)
		{
			if (!result.ContainsKey(title))
				result.Add(title, new());
			result[title].AddRange(elements);
		}
	}

	private int GetArgsBeforeMethod(string line, out string[] data)
	{
		data = line.ReadTo("(").SplitByElements();
		return data.Length;
	}

	private string ClearPropertyLine(string value)
		=> value.Split("=>").First()
			.Split("=").First()
			.Split("{").First()
			.Replace(";", "").Trim();

	private ContentPartModel GetAttributes(string[] lines, int headerLine)
	{
		var lineIndexes = new List<int>();

		while (headerLine-- > 0)
		{
			var line = lines[headerLine].Trim();
			if (line.StartsWith("["))
			{
				lineIndexes.Add(headerLine);
			}
			else
			{
				break;
			}
		}

		if (lineIndexes.Count == 0) return new();

		return new()
		{
			Success = true,
			StartLine = lineIndexes.Last(),
			EndLine = lineIndexes.First(),
			FullContent = lines,
		};
	}

	private ContentPartModel GetComment(string[] lines, int headerLine)
	{
		var lineIndexes = new List<int>();

		while (headerLine-- > 0)
		{
			var line = lines[headerLine].Trim();
			if (line.StartsWith("//"))
			{
				lineIndexes.Add(headerLine);
			}
			else
			{
				break;
			}
		}

		if (lineIndexes.Count == 0) return new();

		return new()
		{
			Success = true,
			StartLine = lineIndexes.Last(),
			EndLine = lineIndexes.First(),
			FullContent = lines,
		};
	}

	private ContentPartModel GetContentPart(string[] lines, int headerLine, string start = "{", string end = "}", bool isFirst = true)
	{
		var startIndexes = new List<int>();
		var endIndexes = new List<int>();
		var startLine = -1;

		for (int i = headerLine; i < lines.Length; ++i)
		{
			if (isFirst && (i > headerLine + 1 && startIndexes.Count == 0))
				return new();

			startIndexes.AddRange(lines[i].EveryIndexOf(start).ToArray());
			endIndexes.AddRange(lines[i].EveryIndexOf(end).ToArray());

			if (endIndexes.Count > startIndexes.Count)
				return new();

			if (startLine == -1 && startIndexes.Count != 0)
				startLine = i;

			if (startIndexes.Count == endIndexes.Count && startLine != -1)
			{
				return new()
				{
					Success = true,
					StartLine = startLine,
					StartIndex = startIndexes.First(),
					EndLine = i,
					EndIndex = endIndexes.Last(),
					FullContent = lines,
				};
			}
		}

		return new();
	}

	#endregion Inner
}