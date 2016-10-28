using System;
using System.Diagnostics;
using System.IO;
using AppSettings = AutoUpdater.Properties.Settings;

namespace AutoUpdater
{
	public class UpdaterCore
	{
		/// <summary>
		/// AutoUpdater.exe 가 업데이트 되었는지 여부
		/// </summary>
		public bool UpdaterUpdated { get; private set; }

		/// <summary>
		/// 업데이터가 사용할 임시폴더
		/// </summary>
		public static string UpdateDirectory => "UpdaterTemp";

		/// <summary>
		/// 업데이트 수행 전 온라인 버전을 불러옴
		/// </summary>
		public void Prepare()
		{
#if DEBUG
			Uri VerUri = new Uri(AppSettings.Default.KCVTestUpdateUrl);
#else
			Uri VerUri = new Uri(AppSettings.Default.KCVUpdateUrl);
#endif
			Updater.Current.LoadVersion(VerUri.AbsoluteUri); // config 파일에 정의된 URL에 XML 요청
		}

		/// <summary>
		/// 실제 업데이트 수행
		/// </summary>
		/// <param name="IsSelfUpdate">자가 업데이트를 시도하는지 여부</param>
		/// <param name="BaseDirectory">작업 디렉토리</param>
		/// <param name="_str_File">업데이트를 시도하는 파일명. 없다면 null</param>
		public void Update(bool IsSelfUpdate, string BaseDirectory, string _str_File = null)
		{
			Uri RemoteURL = new Uri(Updater.Current.GetOnlineVersion(IsSelfUpdate, true));
			var TempDirectory = Path.Combine(BaseDirectory, UpdateDirectory);
			var LocalDestination = Path.Combine(TempDirectory, "Temp.zip");

			var OnlineVersion = Updater.Current.GetOnlineVersion(IsSelfUpdate, false);

			int statusint = 0;
			string status = "";

			FileVersionInfo LocalVersion = null;
			bool NeedToUpdate = false;
			if (_str_File != null)
			{
				LocalVersion = FileVersionInfo.GetVersionInfo(Path.Combine(BaseDirectory, _str_File));
				NeedToUpdate = Updater.Current.IsOnlineVersionGreater(IsSelfUpdate, LocalVersion.FileVersion);
			}

			#region 자가 업데이트
			if (IsSelfUpdate)
			{
				if (_str_File == null) return; // 파일명 필요

				TempDirectory = Path.Combine(TempDirectory, "AutoUpdater");
				LocalDestination = Path.Combine(TempDirectory, "Updater.zip");
				Directory.CreateDirectory(TempDirectory);

				ErrorReport.WriteLine("현재 AutoUpdater 버전: " + LocalVersion.FileVersion);
				ErrorReport.WriteLine("최신 AutoUpdater 버전: " + OnlineVersion);
				ErrorReport.WriteLine();

				if (NeedToUpdate)
				{
					ErrorReport.Write("최신 파일 다운로드 시작... ");
					statusint = Updater.Current.UpdateFile(
						IsSelfUpdate,
						RemoteURL.ToString(),
						LocalDestination,
						LocalVersion.FileVersion
					);
				}

				switch (statusint)
				{
					case 1:
						status = "성공";
						break;
					case -1:
						status = "실패";
						break;
					default:
						status = "없음";
						break;
				}
				ErrorReport.WriteLine("AutoUpdater 파일 다운로드: " + status);

				try
				{
					if (File.Exists(LocalDestination))
					{
						ErrorReport.Write("압축을 해제합니다...");
						Deflate.Current.ExtractZip(LocalDestination, TempDirectory);
						File.Delete(LocalDestination);

						ErrorReport.WriteLine("압축 해제 완료");
						ErrorReport.WriteLine();
						this.UpdaterUpdated = true;

						Process MyProcess = new Process();
						MyProcess.StartInfo.FileName = "AutoUpdater.exe";
						MyProcess.StartInfo.WorkingDirectory = TempDirectory;
						MyProcess.Start();
						MyProcess.Refresh();
					}
					else ErrorReport.WriteLine();
				}
				catch (Exception e)
				{
					ErrorReport.WriteLine("에러발생 : ");
					ErrorReport.WriteLine(e.Message);
				}
				return;
			}
			#endregion

			#region 칸코레 뷰어가 없는경우
			if (_str_File == null)
			{
				TempDirectory = Path.Combine(TempDirectory, "Viewer");
				LocalDestination = Path.Combine(TempDirectory, "KCV.zip");
				Directory.CreateDirectory(TempDirectory);

				ErrorReport.Write("최신 파일 다운로드 시작... ");
				statusint = Updater.Current.UpdateFile(IsSelfUpdate, RemoteURL.ToString(), LocalDestination, "Forced Upgrades");

				switch (statusint)
				{
					case 1:
						status = "성공";
						break;
					case -1:
						status = "실패";
						break;
					default:
						status = "없음";
						break;
				}
				ErrorReport.WriteLine("업데이트 파일 다운로드: " + status);

				try
				{
					if (File.Exists(LocalDestination))
					{
						ErrorReport.Write("압축을 해제합니다...");

						Deflate.Current.ExtractZip(LocalDestination, TempDirectory);
						File.Delete(LocalDestination);

						ErrorReport.WriteLine("압축 해제 완료");
						ErrorReport.WriteLine("");
					}
					else return;
				}
				catch (Exception e)
				{
					ErrorReport.WriteLine("에러발생 : ");
					ErrorReport.WriteLine(e.Message);
					return;
				}

				try
				{
					ErrorReport.WriteLine("업데이트 적용중...");
					Deflate.Current.CopyFolder(TempDirectory, BaseDirectory, true);

					ErrorReport.WriteLine("뷰어 업데이트를 완료했습니다.");
					ErrorReport.WriteLine();

					this.Execute(BaseDirectory, true);
				}
				catch (Exception e)
				{
					ErrorReport.WriteLine("에러발생 : ");
					ErrorReport.WriteLine(e.Message);
				}
				return;
			}
			#endregion

			#region 칸코레 뷰어가 있는경우
			NeedToUpdate = Updater.Current.IsOnlineVersionGreater(IsSelfUpdate, LocalVersion.FileVersion);

			ErrorReport.WriteLine("현재 제독업무도 바빠! 버전: " + LocalVersion.FileVersion);
			ErrorReport.WriteLine("최신 제독업무도 바빠! 버전: " + OnlineVersion.ToString());
			ErrorReport.WriteLine();

			TempDirectory = Path.Combine(TempDirectory, "Viewer");
			LocalDestination = Path.Combine(TempDirectory, "KCV.zip");
			Directory.CreateDirectory(TempDirectory);

			ErrorReport.Write("최신 파일 다운로드 시작... ");
			statusint = Updater.Current.UpdateFile(IsSelfUpdate, RemoteURL.ToString(), LocalDestination, LocalVersion.FileVersion);
			switch (statusint)
			{
				case 1:
					status = "성공";
					break;
				case -1:
					status = "실패";
					break;
				default:
					status = "없음";
					break;
			}

			ErrorReport.WriteLine("업데이트 파일 다운로드: " + status);

			try
			{
				if (File.Exists(LocalDestination))
				{
					ErrorReport.WriteLine("압축을 해제합니다...");

					Deflate.Current.ExtractZip(LocalDestination, TempDirectory);
					File.Delete(LocalDestination);

					ErrorReport.WriteLine("압축 해제 완료");
					ErrorReport.WriteLine();

					try
					{
						if (Directory.Exists(TempDirectory))
						{
							new KCVKiller().WaitForTerminate();

							ErrorReport.WriteLine("업데이트 적용중...");
							Deflate.Current.CopyFolder(TempDirectory, BaseDirectory, true);

							ErrorReport.WriteLine("뷰어 업데이트를 완료했습니다.");
							ErrorReport.WriteLine();

							/*
							ErrorReport.WriteLine("기존 전투 미리보기 플러그인 파일을 비활성화합니다.");
							if (File.Exists(Path.Combine(BaseDirectory, "Plugins", "BattlePreview.dll")))
							{
								if (File.Exists(Path.Combine(BaseDirectory, "Plugins", "BattlePreview.dll.old")))
									File.Delete(Path.Combine(BaseDirectory, "Plugins", "BattlePreview.dll.old"));
								File.Move(Path.Combine(BaseDirectory, "Plugins", "BattlePreview.dll"), Path.Combine(BaseDirectory, "Plugins", "BattlePreview.dll.old"));
							}
							*/

							if (File.Exists(Path.Combine(BaseDirectory, _str_File)))
								this.Execute(BaseDirectory, true);
						}
						else ErrorReport.WriteLine();
					}
					catch (Exception e)
					{
						ErrorReport.WriteLine("에러발생 : ");
						ErrorReport.WriteLine(e.Message);
						return;
					}
				}
				else
				{
					if (File.Exists(Path.Combine(BaseDirectory, _str_File)))
						this.Execute(BaseDirectory, false);
				}
				return;
			}
			catch (Exception e)
			{
				ErrorReport.WriteLine("에러발생 : ");
				ErrorReport.WriteLine(e.Message);
				return;
			}
			#endregion

			// ErrorReport.WriteLine("업데이트 오류가 발생했습니다!");
		}

		/// <summary>
		/// 바로 위 상위 디렉토리를 반환합니다.
		/// </summary>
		/// <param name="BaseDirectory">기준 디렉토리입니다.</param>
		/// <returns></returns>
		public string UpperFolder(string BaseDirectory)
		{
			return Directory.GetParent(BaseDirectory).FullName;
		}

		/// <summary>
		/// 뷰어의 가장 최신 패치로그 페이지를 열고, 뷰어를 실행합니다.
		/// </summary>
		/// <param name="ViewerDirectory">뷰어가 존재하는 디렉토리입니다.</param>
		/// <param name="Updated">업데이트가 수행됐는지 여부입니다.</param>
		private void Execute(string ViewerDirectory, bool Updated)
		{
			Process MyProcess = new Process();

			// 패치로그 페이지
			var noti = Updater.Current.GetOnlineNotificationURL();
			MyProcess.StartInfo.FileName = noti;
			MyProcess.Start();
			MyProcess.Refresh();

			// 뷰어 프로그램
			MyProcess.StartInfo.WorkingDirectory = ViewerDirectory;
			MyProcess.StartInfo.FileName = "KanColleViewer.exe";
			MyProcess.Start();
			MyProcess.Refresh();
		}
	}
}
