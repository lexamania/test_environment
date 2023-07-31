namespace CSharpCodeAnalyzer.Services;

public static class FileExtension
{
	public static string[] GetFilesRecursively(string folderPath)
	{
		var folders = Directory.GetDirectories(folderPath);
		var files = Directory.GetFiles(folderPath);
		return files.Concat(folders.SelectMany(x => GetFilesRecursively(x))).ToArray();
	}

	public static string[] GetFileContentAsLines(string filePath)
		=> File.ReadLines(filePath).ToArray();

	public static string GetFileContentAsText(string filePath)
		=> File.ReadAllText(filePath);
}