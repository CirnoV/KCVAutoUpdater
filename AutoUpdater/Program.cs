using KCVKiller;
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

			KCVKillers ProcessKiller = new KCVKillers();
			var CurrentDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
			bool AfterSelfUpdate = false;

			Console.Title = "제독업무도 바빠! 자동 업데이트 프로그램";

			string ProgramVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
			Console.WriteLine("제독업무도 바빠! 자동 업데이트 프로그램");
			Console.WriteLine("AutoUpdater Version: " + ProgramVersion);
			Console.WriteLine("----------------------------------------------");
			Console.WriteLine();

			// 자가 업데이트를 수행하는 경우, tmp 폴더에 다운로드 후 실행
			// 상위 폴더에 업데이터가 존재하는 경우 본인을 상위 폴더에 복사 후
			// renew 파라메터로 실행, tmp 폴더가 삭제됨
			// 상위 폴더 업데이터 [../AutoUpdater.exe]


			// 기존 업데이트중 실패한 파일 혹은 자가 업데이트로 남은 폴더 삭제
			var UpdateTempDirectory = Path.Combine(CurrentDirectory, UpdaterCore.UpdateDirectory);
			if (Directory.Exists(UpdateTempDirectory))
				Directory.Delete(UpdateTempDirectory, true);

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
				var upperDirectory = UpdateCore.UpperFolder(CurrentDirectory);
				upperDirectory = UpdateCore.UpperFolder(upperDirectory);
				// 패치 후 2단계 위로 가야 함 UpdaterTemp/AutoUpdater/AutoUpdater.exe

				if (!AfterSelfUpdate && File.Exists(Path.Combine(upperDirectory, "AutoUpdater.exe")))
				{
					Console.WriteLine("상위폴더에 AutoUpdater.exe가 감지되었습니다. 자가업데이트를 시행합니다.");
					Deflate.Current.CopyFolder(CurrentDirectory, upperDirectory, false);

					Process newProcess = new Process();
					newProcess.StartInfo.FileName = "AutoUpdater.exe";
					newProcess.StartInfo.WorkingDirectory = upperDirectory;
					newProcess.StartInfo.Arguments = "renew";
					newProcess.Start();
					newProcess.Refresh();
					return;
				}

				int dispCount = 0, totalCount = 0;
				string[] dispTable = new string[] {
					".   ", " .  ", "  . ",
					"   .", "  . ", " .  "
				};
				while (true)
				{
					Console.CursorLeft = 0;
					Console.Write("제독업무도 바빠!의 종료를 확인중입니다 ");
					Console.Write($"[{dispTable[dispCount % 7]}]");

					if (++dispCount >= 6)
					{
						dispCount = 0;
						totalCount++;

						ProcessKiller.KCV();
						if (ProcessKiller.IsKCVDead) break;

						if (totalCount == 10)
						{
							Console.WriteLine();
							Console.WriteLine();
							Console.WriteLine("뷰어 프로세스가 정상적으로 종료되지 않았습니다.");
							Console.WriteLine("윈도우 작업 관리자를 통해 직접 뷰어 프로세스를 종료해주시기 바랍니다.");
							Console.CursorTop = 4;
						}
					}

					Thread.Sleep(250);
				}

				Console.CursorTop = 5;
				Console.CursorLeft = 0;
				for (int i = 0; i < 4; i++)
					Console.WriteLine("                                                                               ");
				// 작업관리자 메시지 지우기

				Console.CursorTop = 4;
				Console.CursorLeft = 0;
				Console.WriteLine("제독업무도 바빠!의 종료를 확인중입니다 [ OK ]");
				Console.WriteLine();

				Console.WriteLine("서버로부터 버전 정보를 받는중...");
				Console.WriteLine();

				UpdateCore.Prepare();

				if (!AfterSelfUpdate) UpdateCore.Update(true, CurrentDirectory, "AutoUpdater.exe");
				if (UpdateCore.UpdaterUpdated) return;

				if (File.Exists(Path.Combine(CurrentDirectory, "KanColleViewer.exe")))
				{
					UpdateCore.Update(false, CurrentDirectory, "KanColleViewer.exe");
				}
				else // KanColleViewer.exe 파일이 없는경우
				{
					Console.WriteLine("제독업무도 바빠!의 실행파일이 없습니다!");
					Console.WriteLine();
					Console.Write("최신버전을 새로 다운로드/설치하시겠습니까? (y/n): ");

					var t = System.Console.ReadLine();
					if (t.Length <= 0) return;
					if ("yYㅛ".Contains(t.Substring(0, 1)))
						UpdateCore.Update(false, CurrentDirectory);
				}
			}
			catch (Exception e)
			{
				error.catcherror(e, CurrentDirectory);
				Console.WriteLine("에러발생 : ");
				Console.WriteLine(e.Message);
			}
		}
	}
}
