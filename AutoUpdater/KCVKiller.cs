using System;
using System.Diagnostics;

namespace KCVKiller
{
	public class KCVKillers
	{
		public bool IsKCVDead { get; set; }

		public void KCV()
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

			if (ProcessExists) IsKCVDead = false;
			else IsKCVDead = true;
		}
	}
}
