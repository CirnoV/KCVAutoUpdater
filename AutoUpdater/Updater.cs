using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Linq;

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
		public int UpdateFile(bool IsSelfUpdate, string RemoteURL, string LocalPath, string LocalVersion)
		{
			var MainFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);

			using (WebClient Client = new WebClient())
			{
				try
				{
					if (IsSelfUpdate || "Forced Upgrades".Equals(LocalVersion) || IsOnlineVersionGreater(IsSelfUpdate, LocalVersion))
					{
						Client.DownloadFile(RemoteURL, LocalPath);
						return 1;
					}
				}
				catch
				{
					// 다운로드를 실패한 경우
					return -1;
				}
			}
			return 0;
		}
	}
}