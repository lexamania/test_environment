using System.Text;
using Test.MusConv.Models;

namespace Test
{
    public static class MusConvModelParser
    {
		public const string TAB5 = "\t\t\t\t\t";
		public const string TAB4 = "\t\t\t\t";
		public const string TAB3 = "\t\t\t";
		public const string TAB2 = "\t\t";
        public static void CreateMapper(ParserModel model)
		{
			var items = new List<ClassModel>();
			var fileContent = File.ReadAllText(model.SourceFilePath);
			var endIndex = 0;


			// Parser
			while(true)
			{
				var item = new ClassModel()
				{
					DestinationType = model.SourceClassName
				};

				// Service name
				endIndex = fileContent.CutMin(endIndex, $"/// <", "/// </", out var naming);
				if (endIndex == -1) break;
				item.ServiceName = naming?.Split("\n")[1].Replace("///", "").Replace("//", "").Trim();


				// Full constructor
				endIndex = fileContent.CutMin(endIndex, $"public {model.SourceClassName}", "\n\t\t}", out var fullText);
				if (endIndex == -1) break;
				fullText += "\n\t\t}";
				
				// Source args
				fullText.CutMin(0, "(", ")", out var parameters);
				var args = parameters.Replace("(", "").Replace(")", "").Split(",")[0].Trim().Split(' ');
				if (args.Length < 2) continue;

				item.SourceType = args[0];
				item.SourceItemName = args[1];
				
				// Fields
				fullText.CutMax(0, "{", "}", out var body);
				var fields = body.Replace("{", "").Replace("}", "").Split(';');
				foreach(var field in fields)
				{
					var values = field.Split('=', 2);
					var v1 = values[0].Trim();
					if (values.Length < 2 || v1.StartsWith("if") || v1.StartsWith("/") || v1.StartsWith("AllTracks")) continue;

					item.Fields.Add(new FieldModel()
					{
						SourceField = values[1].Trim().Replace($"{item.SourceItemName}.", "").Replace($"{item.SourceItemName}?.", ""),
						DestinationField = values[0].Trim()
					});
				}

				items.Add(item);
			};


			// Constructor
			var content = new StringBuilder();

			// File start
			content.Append(@"using AutoMapper;
using MusConv.ViewModels.Models;

namespace MusConv.ViewModels.Helper.Mapper.Profiles
{
	public class " + $"{model.DestinationClassName} : Profile\n" +
$"\t{{\n{TAB2}public {model.DestinationClassName}() \n{TAB2}{{\n");

			// File body
			foreach (var item in items)
			{
				var itemB = new StringBuilder();
				itemB.Append($"{TAB3}// {item.ServiceName}");
				itemB.Append($"\n{TAB3}CreateMap<{item.SourceType}, {item.DestinationType}>()");
				foreach (var field in item.Fields)
				{
					itemB.Append($"\n{TAB4}.ForMember(src => src.{field.DestinationField}, opt => {{");
					itemB.Append($"\n{TAB5}opt.PreCondition(src => src.{field.SourceField} != null);");
					itemB.Append($"\n{TAB5}opt.MapFrom(src => src.{field.SourceField.Replace("?.", ".")});");
					itemB.Append($"\n{TAB4}}})");
				}
				itemB.Append($"\n{TAB4}.ForAllOtherMembers(opt => opt.Ignore());\n\n");

				content.Append(itemB);
			}

			// File end
			content.Append("\t\t}\n\t}\n}");

			if (File.Exists(model.DestinationFilePath))
				File.Delete(model.DestinationFilePath);

			using var file = File.CreateText(model.DestinationFilePath);
			file.Write(content.ToString());
		}

		public static int CutMin(this string content, int endIndex, string text1, string text2, out string? res)
		{
			res = null;

			var startIndex = content.IndexOf(text1, endIndex);
			if (startIndex == -1) return -1;

			var lastIndex = content.IndexOf(text2, startIndex);
			if (lastIndex == -1) return -1;

			res = content.Substring(startIndex, lastIndex - startIndex);
			return lastIndex;
		}

		public static int CutMax(this string content, int endIndex, string text1, string text2, out string? res)
		{
			res = null;

			var startIndex = content.IndexOf(text1, endIndex);
			if (startIndex == -1) return -1;

			var lastIndex = content.LastIndexOf(text2);
			if (lastIndex == -1) return -1;

			res = content.Substring(startIndex, lastIndex - startIndex);
			return lastIndex;
		}
    }

	public class ClassModel
	{
		public string ServiceName { get; set; }
		public string SourceType { get; set; }
		public string SourceItemName { get; set; }
		public string DestinationType { get; set; }
		public List<FieldModel> Fields { get; set; } = new();
	}

	public class FieldModel
	{
		public string DestinationField { get; set; }
		public string SourceField { get; set; }
	}
}