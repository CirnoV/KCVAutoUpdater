using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Reflection;

namespace AutoUpdater
{
	class Program
	{
		static void Main(string[] args)
		{
			ErrorReport error = new ErrorReport();

			var CurrentDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
			bool AfterSelfUpdate = false;

			Console.Title = "제독업무도 바빠! 자동 업데이트 프로그램";

			string ProgramVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
			ErrorReport.WriteLine("제독업무도 바빠! 자동 업데이트 프로그램");
			ErrorReport.WriteLine("AutoUpdater Version: " + ProgramVersion);
			ErrorReport.WriteLine("----------------------------------------------");
			ErrorReport.WriteLine();

			// 자가 업데이트를 수행하는 경우, tmp 폴더에 다운로드 후 실행
			// 상위 폴더에 업데이터가 존재하는 경우 본인을 상위 폴더에 복사 후
			// renew 파라메터로 실행, tmp 폴더가 삭제됨
			// 상위 폴더 업데이터 [../AutoUpdater.exe]

			Thread.Sleep(3500);

			// 기존 업데이트중 실패한 파일 혹은 자가 업데이트로 남은 폴더 삭제
			var TempDirectories = new string[]
			{
				UpdaterCore.UpdateDirectory,
				"UpdateBin", "tmp"
			};
			foreach (var Temp in TempDirectories)
			{
				var UpdateTempDirectory = Path.Combine(CurrentDirectory, Temp);
				try
				{
					if (Directory.Exists(UpdateTempDirectory))
						Directory.Delete(UpdateTempDirectory, true);
				}
				catch { }
			}

			try
			{
				if (args != null)
				{
					if (args.Length > 0)
					{
						if (args[0] == "renew")
							AfterSelfUpdate = true;
					}
				}

				UpdaterCore UpdateCore = new UpdaterCore();
				var upperDirectory = CurrentDirectory;

				try
				{ // 상위 폴더가 없을수도 있음
					for (int i = 0; i < 2; i++)
					{
						upperDirectory = UpdateCore.UpperFolder(CurrentDirectory);
						if (!AfterSelfUpdate && File.Exists(Path.Combine(upperDirectory, "AutoUpdater.exe")))
						{
							ErrorReport.WriteLine("상위폴더에 AutoUpdater.exe가 감지되었습니다. 자가업데이트를 시행합니다.");
							Deflate.Current.CopyFolder(CurrentDirectory, upperDirectory, false);

							Process newProcess = new Process();
							newProcess.StartInfo.FileName = "AutoUpdater.exe";
							newProcess.StartInfo.WorkingDirectory = upperDirectory;
							newProcess.StartInfo.Arguments = "renew";
							newProcess.Start();
							newProcess.Refresh();
							return;
						}
					}
				}
				catch { }

				ErrorReport.WriteLine("서버로부터 버전 정보를 받는중...");
				ErrorReport.WriteLine();

				UpdateCore.Prepare();

				if (!AfterSelfUpdate) UpdateCore.Update(true, CurrentDirectory, "AutoUpdater.exe");
				if (UpdateCore.UpdaterUpdated) return;

				if (File.Exists(Path.Combine(CurrentDirectory, "KanColleViewer.exe")))
				{
					UpdateCore.Update(false, CurrentDirectory, "KanColleViewer.exe");
				}
				else // KanColleViewer.exe 파일이 없는경우
				{
					ErrorReport.WriteLine("제독업무도 바빠!의 실행파일이 없습니다!");
					ErrorReport.WriteLine();
					ErrorReport.Write("최신버전을 새로 다운로드/설치하시겠습니까? (y/n): ");
					ErrorReport.Write("", true);

					var t = Console.ReadLine();
					if (t.Length <= 0) return;

					if ("yYㅛ".Contains(t.Substring(0, 1)))
						UpdateCore.Update(false, CurrentDirectory);
				}
			}
			catch (Exception e)
			{
				error.catcherror(e, CurrentDirectory);
				ErrorReport.WriteLine("에러발생 : ");
				ErrorReport.WriteLine(e.Message);
			}

			ErrorReport.WriteLine();
			ErrorReport.Write("자동 업데이트를 종료합니다... in ");
			{
				int cursorX = Console.CursorLeft;
				int cursorY = Console.CursorTop;

				for (int i = 2; i >= 0; i--)
				{
					Console.CursorLeft = cursorX;
					Console.CursorTop = cursorY;
					Console.Write("                ");

					Console.CursorLeft = cursorX;
					Console.CursorTop = cursorY;
					ErrorReport.Write(i + " sec", true);

					Thread.Sleep(1000);
				}
			}
		}
	}
}
