using System;
using System.Collections.Generic;
using CSharpCodeAnalyzer.Models.Elements;
using CSharpCodeAnalyzer.Services;

namespace Test
{
	public class Test
	{
		public static void Main(string[] args)
		{
			var parser = new CSharpParser();
			var example = @"C:\Content\Job\musconv\musconv\MusConv.ViewModels\ViewModels\Base\SectionViewModelBase.cs";
			var pathes = new string[]
			{
				@"C:\Content\Job\musconv\musconv\MusConv.ViewModels\ViewModels\SectionViewModels",
				@"C:\Content\Job\musconv\musconv\MusConv.ViewModels\ViewModels\WebViewViewModels"
			};

			var result = new List<ClassElement>();
			foreach(var path in pathes)
			{
				result.AddRange(parser.ReadFromFolder(path));
			}
			Console.WriteLine($"{result.Count}");

			parser.WriteToFilesWithExample(result, example);
		}
	}
}