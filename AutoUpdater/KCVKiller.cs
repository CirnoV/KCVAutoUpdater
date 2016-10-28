using System;
using System.Diagnostics;

namespace AutoUpdater
{
	internal class KCVKiller
	{
		public bool ProcessKilled()
		{
			bool ProcessExists = false;
			foreach (Process process in Process.GetProcesses())
			{
				if (process.ProcessName.StartsWith("KanColleViewer"))
				{
					if (!process.ProcessName.Contains("vshost"))
					{
						ProcessExists = true;
						break;
					}
				}
			}

			return !ProcessExists;
		}

		public void WaitForTerminate()
		{
			int dispCount = 0, totalCount = 0;
			string[] dispTable = new string[] {
					".   ", " .  ", "  . ",
					"   .", "  . ", " .  "
				};
			while (true)
			{
				Console.CursorLeft = 0;
				ErrorReport.Write("제독업무도 바빠!의 종료를 확인중입니다 ");
				ErrorReport.Write($"[{dispTable[dispCount % 7]}]");

				if (++dispCount >= 6)
				{
					dispCount = 0;
					totalCount++;

					if (this.ProcessKilled()) break;

					if (totalCount == 10)
					{
						ErrorReport.WriteLine();
						ErrorReport.WriteLine();
						ErrorReport.WriteLine("뷰어 프로세스가 정상적으로 종료되지 않았습니다.");
						ErrorReport.WriteLine("윈도우 작업 관리자를 통해 직접 뷰어 프로세스를 종료해주시기 바랍니다.");
						Console.CursorTop = 4;
					}
				}

				System.Threading.Thread.Sleep(250);
			}

			Console.CursorTop = 5;
			Console.CursorLeft = 0;
			for (int i = 0; i < 4; i++)
				ErrorReport.WriteLine("                                                                               ");
			// 작업관리자 메시지 지우기

			Console.CursorTop = 4;
			Console.CursorLeft = 0;
			ErrorReport.WriteLine("제독업무도 바빠!의 종료를 확인중입니다 [ OK ]");
			ErrorReport.WriteLine();
		}
	}
}
