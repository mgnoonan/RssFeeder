using System;
using System.IO;
using System.Text;

namespace RssFeeder.Console.Utility
{
	/// <summary>
	/// Summary description for FileTools.
	/// </summary>
	public class FileTools
	{
		public FileTools()	{}

		public static string GetFullPathName(string path, string fileName)
		{
			StringBuilder sb = new StringBuilder();

			if(path.Trim().Length > 0)
			{
				sb.Append(path.Trim());

				if(!path.EndsWith("\\"))
					sb.Append("\\");
			}

			sb.Append(fileName.Trim());

			return sb.ToString();
		}

		public static string ExtractPath(string fullPath)
		{
			string path;

			if(fullPath.EndsWith("\\"))
				path = fullPath;
			else
				path = fullPath.Substring(0, fullPath.LastIndexOf("\\") + 1);

			return path;
		}

		public static StreamWriter CreateTextFile(string fileName)
		{
			if(File.Exists(fileName))
			{
				throw new IOException("File already exists.");
			}

			return File.CreateText(fileName);
		}
	}
}
