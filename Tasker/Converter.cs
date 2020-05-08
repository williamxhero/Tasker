using java.text;
using System;

namespace Tasker
{

	public static class Converter
	{
		public static bool IsntDeveloper(this ManPos mp)
		{
			return ! mp.ToString().StartsWith("Dev");
		}

		public static bool IsSpecific(this ManName mn)
		{
			return mn != ManName.TEAM && mn != ManName.PEER;
		}

		public static string ToText(this Status s)
		{
			switch (s)
			{
				case Status.No: return "";
				case Status.Cancel: return "已取消";
				case Status.Wait: return "未开始";
				case Status.Doing: return "开发中";
				case Status.Pause: return "暂停中";
				case Status.Done: return "测试中";
				case Status.Closed: return "已完成";
			}
			return "";
		}

		public static int ToInt(this Status s)
		{
			switch (s)
			{
				case Status.No: return 0;
				case Status.Cancel: return 1;
				case Status.Wait: return 2;
				case Status.Doing: return 3;
				case Status.Pause: return 4;
				case Status.Done: return 5;
				case Status.Closed: return 6;
			}
			return 0;
		}

		public static bool YearIsNotOk(int year)
		{
			return year < 2000 || year > DateTime.Now.Year;
		}

		public static bool YearIsOK(this DateTime dt)
		{
			return !YearIsNotOk(dt.Year);
		}

		public static bool YearIsNotOK(this DateTime dt)
		{
			return YearIsNotOk(dt.Year);
		}

		public static DateTime ToDt(this java.util.Date dt)
		{
			DateFormat Fmtr = new SimpleDateFormat("yy-MM-dd");
			var FmtStr = Fmtr.format(dt);
			DateTime.TryParse(FmtStr, out DateTime Ret);
			return Ret;
		}

		public static float ToFloat(this string str)
		{
			float.TryParse(str.Trim(), out float Ret);
			return Ret;
		}

		public static uint ToUint(this string str)
		{
			uint.TryParse(str.Trim(), out uint Ret);
			return Ret;
		}

		public static DateTime ToDt(this string str)
		{
			DateTime.TryParse(str.Trim(), out DateTime Ret);
			return Ret;
		}

		public static Status ToStat(this string str)
		{
			switch (str.Trim().ToLower())
			{
				case "wait": return Status.Wait;
				case "doing": return Status.Doing;
				case "done": return Status.Done;
				case "pause": return Status.Pause;
				case "cancel": return Status.Cancel;
				case "closed": return Status.Closed;
			}
			return Status.No;
		}

		public static ManName ToMan(this string str)
		{
			switch (str.Trim().ToLower())
			{
				case "rbw": return ManName.RBW;
				case "jzt": return ManName.JZT;
				case "hhl": return ManName.HHL;
				case "gys": return ManName.GYS;
				case "xueg": return ManName.XG;
				case "zss": return ManName.ZSS;
				case "wzz": return ManName.WZZ;
				case "test02": return ManName.SL;
				case "lpb": return ManName.LPB;
				//case "hsl": return Man.HSL;
				case "hw0": return ManName.HW;
				case "admin": return ManName.HW;
			}
			return ManName.PEER;
		}
	}//class
}
