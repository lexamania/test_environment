using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AutoMapper;
using Test.MusConv;

namespace Test
{
	public class ClassA
	{
		public ClassB B { get; set; }
	}
	public class ClassB
	{
		public string BName { get; set; }
	}
	public class ClassC
	{
		public string CName { get; set; }
	}

	public static class test2
	{
		public static string ReverseWords(string text)
		{
			var words = text.Split(" ");
			var reversed = new string[words.Length];
			for(int i = 0; i < words.Length; ++i)
			{
				var letters = new char[words[i].Length];
				for(int j = words[i].Length-1, k = 0; j >= 0; --j, ++k)
				{
					letters[k] = words[i][j];
				}
				reversed[i] = new string(letters);
			}

			return string.Join(" ", reversed);
		}

		public static int GetSecondLargestInt(IEnumerable<int> numbers)
		{
			if (numbers == null || numbers.Count() == 0)
			{
				return 0;
			}

			int max = numbers.First();
			int secondMax = numbers.First();

			foreach (int number in numbers)
			{
				if (number > max)
				{
					secondMax = max;
					max = number;
				}
			}

			return secondMax;
		}

		[Flags]
		public enum Status
		{
			Funny = 0x01,
			Hilarious = 0x02,
			Boring = 0x04,
			Cool = 0x08,
			Interesting = 0x10,
			Informative = 0x20,
			Error = 0x40
		}

		delegate void Printer();

		public static async Task Main(string[] args)
		{
			MapperProfileParser.CreateProfiles();
		}
		
	}


	public class OwnStack
	{
		private Element<object> LastElement { get; set; }

		public void Push(object value)
		{
			var newelement = new Element<object>() 
			{
				Value = value,
				PrevElement = LastElement
			};

			LastElement = newelement;
		}

		public object Pop()
		{
			if (LastElement == null) return "";

			var value = LastElement.Value;
			LastElement = LastElement.PrevElement;

			return value;
		}

		public class Element<TElement>
		{
			public TElement Value { get; set; }
			public Element<TElement> PrevElement { get; set; }
		}
	}
}