using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using System.Threading.Tasks;

namespace AutoUpdater
{
	public class Updater
	{
		public static Updater Current { get; } = new Updater(); // Singleton
		private XDocument VersionXML;

		/// <summary>
		/// 전달받은 URL로부터 XML을 읽어들입니다.
		/// </summary>
		/// <param name="UpdateURL">XML을 읽어들일 URL입니다.</param>
		public bool LoadVersion(string UpdateURL)
		{
			try
			{
				VersionXML = XDocument.Load(UpdateURL);
				if (VersionXML == null) return false;
			}
			catch
			{
				// XML 파일을 부르지 못한 경우
				return false;
			}
			return true;
		}

		/// <summary>
		/// 전달받은 인자로부터 최신 버전/URL 을 가져옵니다.
		/// </summary>
		/// <param name="IsSelfUpdate">업데이터의 버전을 반환할지 여부입니다.</param>
		/// <param name="bGetURL">버전이 아닌 URL을 반환할지 여부입니다.</param>
		public string GetOnlineVersion(bool IsSelfUpdate, bool bGetURL = false)
		{
			// XML 파일을 부르지 못한 경우
			if (VersionXML == null) return "";

			IEnumerable<XElement> Versions = VersionXML.Root.Descendants("Item");
			var GroupName = IsSelfUpdate ? "Updater" : "App";
			var ElementName = bGetURL ? "URL" : "Version";
			
			return Versions.Where(x => x.Element("Name")?.Value?.Equals(GroupName) ?? false)
				.FirstOrDefault()
				.Element(ElementName)
				.Value;
		}

		/// <summary>
		/// 뷰어 업데이트 이후 이동될 웹페이지를 가져옵니다.
		/// </summary>
		public string GetOnlineNotificationURL()
		{
			const string DefaultURL = "http://kcvkr.tistory.com/";

			// XML 파일을 부르지 못한 경우
			if (VersionXML == null) return DefaultURL;

			IEnumerable<XElement> Versions = VersionXML.Root.Descendants("Item");
			return
				Versions.Where(x => x.Element("Name")?.Value?.Equals("App") ?? false)
					.FirstOrDefault()
					?.Element("NoticeURL")
					?.Value
				?? DefaultURL;
		}

		/// <summary>
		/// 온라인의 버전이 더 최신인지 검사합니다.
		/// </summary>
		/// <param name="IsSelfUpdate">업데이터의 버전을 비교할지 여부입니다.</param>
		/// <param name="LocalVersionString">로컬에 존재하는 프로그램의 버전입니다.</param>
		public bool IsOnlineVersionGreater(bool IsSelfUpdate, string LocalVersionString)
		{
			// XML 파일을 부르지 못한 경우
			if (VersionXML == null) return true;

			string[] UnknownTable = new string[]
			{
				"알 수 없음","없음","Unknown","", null
			};
			if (UnknownTable.Contains(LocalVersionString)) return false;

			IEnumerable<XElement> Versions = VersionXML.Root.Descendants("Item");
			Version LocalVersion = new Version(LocalVersionString);
			var GroupName = IsSelfUpdate ? "Updater" : "App";

			var OnlineVersion = new Version(
					Versions.Where(x => x.Element("Name").Value.Equals(GroupName))
						.FirstOrDefault()
						?.Element("Version")
						?.Value
					?? "0.0.0.0");

			return LocalVersion.CompareTo(OnlineVersion) < 0;
		}

		/// <summary>
		/// 실제 업데이트를 수행합니다.
		/// </summary>
		/// <param name="IsSelfUpdate">업데이터를 업데이트할지 여부입니다.</param>
		/// <param name="RemoteURL">다운로드 받을 대상 URL입니다.</param>
		/// <param name="LocalPath">다운로드 받을 로컬 경로입니다.</param>
		/// <param name="LocalVersion">로컬에 존재하는 프로그램의 현재 버전입니다.</param>
		/// <returns>1: 성공, -1: 실패, 0: 없음</returns>
		public async Task<int> UpdateFile(bool IsSelfUpdate, string RemoteURL, string LocalPath, string LocalVersion)
		{
			var currentTime = new Func<long>(() => (long)(DateTime.UtcNow - DateTime.MinValue).TotalMilliseconds);

			var cursorX = Console.CursorLeft;
			var cursorY = Console.CursorTop;
			var PrevBytes = 0L;
			var nResult = 0;
			var time = currentTime();

			const int MaxRetries = 5;
			int Retries = 0;

			// Fix SSL/TLS issue, specify security protocols
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

			using (WebClient Client = new WebClient())
			{
				Client.DownloadProgressChanged += (s, e) =>
				{
					if (currentTime() - time < 500) return;
					time = currentTime();

					Console.CursorLeft = cursorX;
					Console.CursorTop = cursorY;
					Console.Write("                                                            ");

					Console.CursorLeft = cursorX;
					Console.CursorTop = cursorY;
					ErrorReport.Write(string.Format(
						"[{0} / {1}, {2}/s]",
						MeasureSize(e.BytesReceived),
						MeasureSize(e.TotalBytesToReceive),
						MeasureSize((long)((e.BytesReceived - PrevBytes) / 0.5m))
					), true);

					PrevBytes = e.BytesReceived;
				};
				Client.DownloadFileCompleted += (s, e) =>
				{
					Console.CursorLeft = cursorX;
					Console.CursorTop = cursorY;
					Console.Write("                                                            ");

					Console.CursorLeft = cursorX;
					Console.CursorTop = cursorY;
					ErrorReport.WriteLine("[ OK ]");
				};

				while (Retries < MaxRetries)
				{
					try
					{
						if (IsSelfUpdate || "Forced Upgrades".Equals(LocalVersion) || IsOnlineVersionGreater(IsSelfUpdate, LocalVersion))
						{
							await Client.DownloadFileTaskAsync(new Uri(RemoteURL), LocalPath);
							nResult = 1;
						}
					}
					catch(Exception e)
					{
						// 오류가 발생해 다운로드를 실패한 경우
						ErrorReport.WriteLine(e.ToString());
						nResult = -1;
					}

					// 알 수 없는 오류로 다운로드를 실패한 경우
					var fi = new FileInfo(LocalPath);
					if (!fi.Exists || fi.Length == 0)
						nResult = -1;

					if (nResult == -1)
					{
						Retries++;
						ErrorReport.WriteLine(string.Format("오류가 발생하여 재시작합니다. ({0}/{1})", Retries, MaxRetries));
					}
				}
			}
			return nResult;
		}

		private string MeasureSize(long bytes)
		{
			string[] Units = new string[]
			{
				"bytes","KBs","MBs","GBs","TBs"
			};

			decimal x = bytes;
			int unit = 0;

			while (x >= 1000.0m)
			{
				x /= 1000.0m;
				unit++;
			}
			return string.Format("{0} {1}", decimal.Round(x, 2).ToString(), Units[unit]);
		}
	}
}