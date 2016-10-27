using System.IO;
using System.IO.Compression;

namespace AutoUpdater
{
	public class Deflate
	{
		public static Deflate Current { get; } = new Deflate(); // Singleton

		/// <summary>
		/// Zip 파일의 압축을 풉니다.
		/// </summary>
		/// <param name="SourceFile">압축을 풀 Zip 파일의 경로입니다.</param>
		/// <param name="ExtractLocation">압축을 풀 대상 디렉토리입니다.</param>
		public void ExtractZip(string SourceFile, string ExtractLocation)
		{
			ZipFile.ExtractToDirectory(SourceFile, ExtractLocation);
		}

		/// <summary>
		/// 폴더를 복사하고, 설정된 경우 원본 디렉토리를 삭제합니다.
		/// </summary>
		/// <param name="SourceDirectory">복사할 원본 디렉토리입니다.</param>
		/// <param name="DestDirectory">대상 디렉토리입니다.</param>
		/// <param name="RemoveSourceAfterCopy">복사 후 원본 디렉토리를 삭제할지 여부입니다.</param>
		public void CopyFolder(string SourceDirectory, string DestDirectory, bool RemoveSourceAfterCopy = false)
		{
			if (!Directory.Exists(DestDirectory))
				Directory.CreateDirectory(DestDirectory);

			string[] files = Directory.GetFiles(SourceDirectory);
			string[] directories = Directory.GetDirectories(SourceDirectory);

			// 파일 복사
			foreach (string file in files)
			{
				string name = Path.GetFileName(file);
				string dest = Path.Combine(DestDirectory, name);

				if(!RemoveSourceAfterCopy)
					File.Copy(file, dest, true);
				else
				{
					if (!file.Contains("AutoUpdater.exe"))
						if (!file.Contains("CommandLine.dll"))
							if (!file.Contains("KCVKiller.dll"))
								File.Copy(file, dest, true);
				}
			}

			// foreach 안에서 재귀 함수를 통해서 폴더 복사 및 파일 복사 진행
			foreach (string directory in directories)
			{
				string name = Path.GetFileName(directory);
				string dest = Path.Combine(DestDirectory, name);
				CopyFolder(directory, dest, RemoveSourceAfterCopy);
			}

			if (RemoveSourceAfterCopy)
				Directory.Delete(SourceDirectory, true);
		}

	}
}
