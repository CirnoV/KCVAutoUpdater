using System;
using System.Diagnostics;
using System.IO;

namespace AutoUpdater
{
	/// <summary>
	/// 처리되지 않은 오류가 발생했을 때 error.log 로 저장하는 처리를 하는 클래스입니다.
	/// </summary>
	public class ErrorReport
	{
		public ErrorReport()
		{
			const string path = "AutoUpdater.log";

			AppDomain.CurrentDomain.UnhandledException +=
				(sender, args) => ReportException(sender, args.ExceptionObject as Exception);

			try
			{
				if (File.Exists(path))
					File.Delete(path);

				TextWriterTraceListener tr = new TextWriterTraceListener(File.CreateText(path));
				tr.Name = "AutoUpdater";
				tr.TraceOutputOptions = TraceOptions.DateTime;

				Trace.Listeners.Add(tr);
				Trace.AutoFlush = true;
			}
			catch { }
		}

		private static void ReportException(object sender, Exception exception)
		{
			#region 상수
			const string messageFormat = @"
===========================================================
ERROR, date = {0}, sender = {1},
{2}
";
			const string path = "error.log";
			#endregion

			try
			{
				var message = string.Format(messageFormat, DateTimeOffset.Now, sender, exception);

				Debug.WriteLine(message);
				File.AppendAllText(path, message);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
			}
		}
		public void catcherror(Exception e,string MainFolder)
		{
			#region 상수
			const string messageFormat = @"
===========================================================
ERROR, date = {0}, sender = {1},
{2}
";
			const string path = "error.log";
			#endregion

			var message = string.Format(messageFormat, DateTimeOffset.Now, AppDomain.CurrentDomain, e);

			Debug.WriteLine(message);
			File.AppendAllText(Path.Combine(MainFolder, path), message);
		}

		public static void Write(string Text = "", bool LogNewLine = false)
		{
			if(LogNewLine) Trace.WriteLine(Text);
			else Trace.Write(Text);

			Console.Write(Text);
		}
		public static void WriteLine(string Text = "")
		{
			Trace.WriteLine(Text);
			Console.WriteLine(Text);
		}
	}
}
