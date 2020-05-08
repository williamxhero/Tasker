using System;
using System.Net;
using System.Text;

namespace Tasker
{
	partial class Tasker
	{
		private string URL = "http://192.168.3.250:81/GetTasks.php";

		public string GetHtml(string condition)
		{
			//id, name, status, realStarted, finishedBy finishedDate, closedBy, closedDate, storyPoint
			WebClient WC = new WebClient {
				Credentials = CredentialCache.DefaultCredentials
			};
			WC.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
			byte[] Post = Encoding.UTF8.GetBytes("cond=" + condition);
			byte[] Page = WC.UploadData(URL, "POST", Post);
			string Html = Encoding.UTF8.GetString(Page);
			return Html;
		}

		public static float MathRound(float v, int d)
		{
			return (float)Math.Round(v, d);
		}

		public static int MathRound(float v)
		{
			return (int)Math.Round(v);
		}
	}
}
