using CSharpCodeAnalyzer.Helpers;
using CSharpCodeAnalyzer.Services;
using TestMain.MusConvParsers.ServicesClients.Models;
using TestMain.MusConvParsers.ServicesClients.Models.Base;
using TestMain.MusConvParsers.ServicesClients.Models.Content;
using TestMain.MusConvParsers.ServicesClients.Models.Elements;
using TestMain.MusConvParsers.ServicesClients.Models.Enum;

namespace TestMain.MusConvParsers.ServicesClients;

public class ServiceClientParser
{
	public const string ManagersFolderPath = @"C:\Content\Job\musconv\musconv\MusConv.ViewModels\Services\MusicServices\Managers";
	public const string ModelFolderPath = @"C:\Content\Job\musconv\musconv\MusConv.ViewModels\Services\MusicServices\Models";
	public const string CreatePath = @"C:\Content\Job\musconv\musconv\MusConv.ViewModels\Services\MusicServices\Clients";

	private readonly string[] ProtectedPropertyStart = { "private" };
	private readonly string[] TypesExceptions = { "string", "int", "List" };
	private readonly string[] AcceptedUsings = { "MusConv.Lib" };
	private readonly string[] UnacceptedUsings = { "MusConv", "System", "ServiceStack", "Nancy" };
	private readonly string[] RemovedElements = { "Album", "Artist", "Track", "CreatePlaylist", "Playlist", "Manager", "Export", "Searcher", "Search", "Stats", "View", "Related" };
	private readonly string[] PathExceptions = { @"C:\Content\Job\musconv\musconv\MusConv.ViewModels\Services\MusicServices\Managers\Common",
		@"C:\Content\Job\musconv\musconv\MusConv.ViewModels\Services\MusicServices\Managers\Base" };
	private readonly string[] NameExceptions = { "_manager" };

	public void UpdateFiles()
	{
		var files = FileExtension.GetFilesRecursively($"{ManagersFolderPath}\\Tracks");
		foreach (var file in files.Where(x => !PathExceptions.Any(p => x.StartsWith(p))))
		{
			RemoveServiceFromManagersInModels(file);
		}
	}

	public void RemoveService(string path)
	{
		var fileContent = FileExtension.GetFileContentAsText(path);
		fileContent = fileContent.Replace("using MusConv.ViewModels.Services.MusicServices.Models.Base;\n", "")
			.Replace("MusicServiceBase service, ", "")
			.Replace("MusicServiceBase service", "")
			.Replace(": base(service)", "")
			.Replace("base(service, ", "base(")
			.Replace("new(service, ", "new(")
			.Replace("new(service)", "new()")
			.Replace("new(Service, ", "new(")
			.Replace("new(Service)", "new()")
			.Replace("new (service, ", "new(")
			.Replace("new (service)", "new()")
			.Replace("new (Service, ", "new(")
			.Replace("new (Service)", "new()");

		File.WriteAllText(path, fileContent);
	}

	public void RemoveServiceFromManagersInModels(string path)
	{
		var fileContent = FileExtension.GetFileContentAsText(path);
		fileContent = fileContent.Replace(" ManagerBase", " TrackManagerBase");

		File.WriteAllText(path, fileContent);
	}

	public void ReplaceClientsInManagers(string path)
	{
		var modelName = ClearName(Path.GetFileNameWithoutExtension(path));
		var fullName = GetFullName(modelName);
		var createPath = $"{CreatePath}\\{fullName}.cs";
		if (!File.Exists(createPath)) return;

		var fileContent = new LineContent(FileExtension.GetFileContentAsLines(path));
		var properties = FindProtectedProperties(fileContent);
		var usings = FindUsings(fileContent);
		if (properties.Count == 0) return;

		var fullText = fileContent.Content;

		// Usings
		var usingInsertIndex = GetUsingInsertIndex(fullText);
		fullText = fullText.Insert(usingInsertIndex + 1, "using MusConv.ViewModels.Services.MusicServices.Clients;\n");

		// Properties
		var clientProperty = properties.FirstOrDefault(x => x.Element.Title == "_client") ?? properties.First();
		if (clientProperty.Element.Title != "_client")
			fullText = fullText.Replace(clientProperty.Element.Title, "_client");

		fullText = fullText.Replace($"private {clientProperty.Element.ReturnType}", $"private readonly {fullName}");
		fullText = fullText.Replace($"{clientProperty.Element.ReturnType} ", $"{fullName} ");
		fullText = fullText.Replace("_client.", $"_client.Client.");
		foreach (var prop in properties.Where(x => x != clientProperty))
		{
			fullText = fullText.Replace($"{prop.ElementContent}\n", "")
				.Replace($"            {prop.Element.Title} = {prop.Element.Title.Replace("_", "")};\n", "")
				.Replace($", {prop.Element.ReturnType} {prop.Element.Title.Replace("_", "")}", "")
				.Replace($"{prop.Element.Title}.", $"_client.{prop.Element.Title}.");
		}

		File.WriteAllText(path, fullText);
	}

	public void CreateClients(string path)
	{
		var modelName = ClearName(Path.GetFileNameWithoutExtension(path));
		var fullName = GetFullName(modelName);
		var createPath = $"{CreatePath}\\{fullName}.cs";
		if (File.Exists(createPath)) return;

		var fileContent = new LineContent(FileExtension.GetFileContentAsLines(path));
		var properties = FindProtectedProperties(fileContent);
		var usings = FindUsings(fileContent);
		if (properties.Count == 0) return;

		var template = FileExtension.GetFileContentAsText(@"C:\Content\Programing\Programs\Test\TestMain\MusConvParsers\ServicesClients\Models\Templates\ShellTemplate.txt");
		template = template.Replace("{Usings}", string.Join("\n", usings.Select(x => x.ElementContent.ToString())));
		template = template.Replace("{Title}", fullName);

		var clientProperty = properties.FirstOrDefault(x => x.Element.Title == "_client") ?? properties.First();
		template = template.Replace("{Parent}", clientProperty.Element.ReturnType);
		template = template.Replace("{Properties}", GetOtherProperties(properties, clientProperty));

		using var stream = File.CreateText(createPath);
		stream.Write(template.Trim());
	}

	public List<ElementShell<PropertyModel>> FindProtectedProperties(ContentBase fileContent)
		=> fileContent.ContentByLines.GetWithIndex()
			.Where(x => IsLineAccessable(x.Value))
			.Select(x => new CodeContent(fileContent).SetCodeElement(CodeElementType.Body, new Range(x.Index, x.Index + 1)))
			.Select(x => new ElementShell<PropertyModel>()
			{
				ElementContent = x,
				Element = ParsePropertyLine(x.ToString())
			})
			.Where(x => !TypesExceptions.Contains(x.Element.ReturnType))
			.Where(x => !NameExceptions.Contains(x.Element.Title))
			.ToList();

	public List<ElementShell<UsingModel>> FindUsings(ContentBase fileContent)
		=> FindUsingEnumerable(fileContent)
			.Where(x => x.Element.AccessModifier == UsingModifier.None)
			.Where(x => AcceptedUsings.Any(u => x.Element.Path.StartsWith(u))
				|| !UnacceptedUsings.Any(u => x.Element.Path.StartsWith(u)))
			.ToList();
	// => fileContent.ContentByLines.GetWithIndex()
	// 	.Where(x => x.Value.StartsWith("using"))
	// 	.Select(x => new CodeContent(fileContent).SetCodeElement(CodeElementType.Body, new Range(x.Index, x.Index + 1)))
	// 	.Select(x => new ElementShell<UsingModel>()
	// 	{
	// 		ElementContent = x,
	// 		Element = ParseUsingLine(x.ToString())
	// 	})
	// 	.Where(x => x.Element.AccessModifier == UsingModifier.None)
	// 	.ToList();

	private IEnumerable<ElementShell<UsingModel>> FindUsingEnumerable(ContentBase fileContent)
	{
		var content = fileContent.ContentByLines;
		var index = -1;

		while (content[++index].StartsWith("using"))
		{
			var codeElement = new CodeContent(fileContent)
				.SetCodeElement(CodeElementType.Body, new Range(index, index + 1));
			var element = ParseUsingLine(content[index]);

			yield return new ElementShell<UsingModel>()
			{
				ElementContent = codeElement,
				Element = element
			};
		}
	}

	private bool IsLineAccessable(string line)
		=> ProtectedPropertyStart.Any(x => line.Contains(x));

	private PropertyModel ParsePropertyLine(string line)
	{
		var words = line.Trim().TrimEnd(';').Split(' ');
		return words.Length switch
		{
			3 => new()
			{
				AccessModifier = words[0],
				ReturnType = words[1],
				Title = words[2]
			},
			_ => new()
			{
				AccessModifier = $"{words[0]} {words[1]}",
				ReturnType = words[2],
				Title = words[3]
			},
		};
	}

	private UsingModel ParseUsingLine(string line)
	{
		var tempLine = line.TrimEnd(';').Replace("using", "").Trim();

		if (line.Contains("static"))
		{
			var words = tempLine.Split();
			return new UsingModel()
			{
				AccessModifier = UsingModifier.Static,
				Path = words[1]
			};
		}

		if (line.Contains('='))
		{
			var words = tempLine.Split('=');
			return new UsingModel()
			{
				AccessModifier = UsingModifier.Replacement,
				Path = words[1].Trim(),
				TempName = words[0].Trim()
			};
		}

		return new UsingModel()
		{
			AccessModifier = UsingModifier.None,
			Path = tempLine
		};
	}

	private string ClearName(string name)
	{
		foreach (var text in RemovedElements)
			name = name.Replace(text, "");
		return name;
	}

	private string GetOtherProperties(List<ElementShell<PropertyModel>> properties, ElementShell<PropertyModel> except)
	{
		var pr = properties.Where(x => x != except).ToList();
		return string.Join("\n", pr.Select(x => $"        public {x.Element.ReturnType} {x.Element.Title} {{ get; set; }}"));
	}

	private string GetFullName(string title) => $"{title}ModelClient";

	private int GetUsingInsertIndex(string text) => text.IndexOf("\nnamespace") - 1;
}