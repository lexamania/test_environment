using System.Text;

namespace Test.MusConv
{
	public static class MapperProfileParser
	{
		private const string sourceFolder = "./MusConv/Source";
		private const string destFolder = "./MusConv/Results";
		private static string[] sourceFiles = new [] 
		{ 
			$"{sourceFolder}/AlbumMappingProfile.txt",
			$"{sourceFolder}/ArtistMappingProfile.txt",
			$"{sourceFolder}/TrackMappingProfile.txt",
			$"{sourceFolder}/PlaylistMappingProfile.txt",
		};
		
		private const string TAB3 = "\t\t\t";
		private const string TAB2 = "\t\t";
		private const string TAB1 = "\t";

		public static void CreateProfiles()
		{
			if (!Directory.Exists(destFolder))
				Directory.CreateDirectory(destFolder);

			var items = new Dictionary<string, List<string>>();

			foreach (var source in sourceFiles)
			{
				var fileContent = File.ReadAllText(source);
				var services = fileContent.Split("// ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		
				foreach (var part in services)
				{
					var data = part.Split("CreateMap", 2);

					var service = data[0].Trim();
					var text = $"{TAB3}CreateMap{data[1].Trim()}";

					if (items.ContainsKey(service))
					{
						items[service].Add(text);
					}
					else
					{
						items[service] = new () { text };
					}
				}
			}


			var classList = new List<string>();
			foreach (var item in items)
			{
				var className = item.Key.Replace(" ", "").Replace(".", "") + "MappingProfile";
				classList.Add(className);

				var destFileName = $"{destFolder}/{className}.cs";
				var content = new StringBuilder();

				content.Append("using System;\n"
					+ "using System.Globalization;\n"
					+ "using System.Linq;\n"
					+ "using AutoMapper;\n"
					+ "using MusConv.Abstractions.Extensions;\n"
					+ "using MusConv.ViewModels.Helper;\n"
					+ "using MusConv.ViewModels.Models;\n\n"
				);
				content.Append("namespace MusConv.ViewModels.Helper.Mappers.Profiles\n{\n");
				content.Append($"{TAB1}// {item.Key}\n");
				content.Append($"{TAB1}public class {className} : MappingProfileBase\n{TAB1}{{\n"
					+ $"{TAB2}public {className}()\n{TAB2}{{");

				foreach (var value in item.Value)
					content.Append($"\n{value}\n");

				content.Append($"{TAB2}}}\n{TAB1}}}\n}}");

				using var file = File.CreateText(destFileName);
				file.Write(content.ToString());
			}

			var classContent = new StringBuilder();
			foreach (var cl in classList.OrderBy(x => x))
				classContent.Append($"cfg.AddProfile<{cl}>();\n");

			using var classFile = File.CreateText("./Classes.txt");
			classFile.Write(classContent.ToString());
		}
	}
}