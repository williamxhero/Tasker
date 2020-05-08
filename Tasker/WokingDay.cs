using net.sf.mpxj;
using net.sf.mpxj.reader;
using System;
using System.Collections.Generic;
using System.IO;

namespace Tasker
{
	public class WorkingDate
	{
		//This Year
		public int Year { get; private set; }
		//This Month
		public int Mon { get; private set; }
		public string Day1Str { get; private set; }
		public string DayNStr { get; private set; }
		public DateTime Day1 { get; private set; }
		public DateTime DayN { get; private set; }
		public string Digits4InM { get { return Day1.ToString("yy-MM"); } }
		public uint WDaysInM { get; private set; }
		public WorkingDate Last;

		/// <summary>
		/// 设置时间点，y年m月最后一天下班前。
		/// 所有此天之后发生的事情，就像是没有发生一样
		/// </summary>
		public bool SetLastYearMonth(int year, int month)
		{
			return SetYearMon(year, month, -1, true);
		}

		public bool SetLastYearMonthNow()
		{
			return SetYearMon(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, true); 
		}

		public bool YearIsNotOK() { return Converter.YearIsNotOk(Year); }

		bool SetYearMon(int year, int month, int lastDay, bool makeLast)
		{
			var LastDay = lastDay;
			if(LastDay == -1)
			{
				LastDay = GetMonthMaxDay(year, month);
			}

			Mon = month;
			Day1Str = $"{year}-{month}-1 09:00:00";
			DayNStr = $"{year}-{month}-{LastDay} 18:00:00";
			Day1 = DateTime.Parse(Day1Str);
			DayN = DateTime.Parse(DayNStr);
			var Ret = Reset(Tasker.BaseFolder, year);
			if (!Ret) return false;

			if (makeLast)
			{
				Last = new WorkingDate();
				var LstM = Day1.AddDays(-1);
				Ret = Last.SetYearMon(LstM.Year, LstM.Month, false);
				if (!Ret) return false;
			}

			return !YearIsNotOK();
		}

		bool SetYearMon(int year, int month, bool setLastMon)
		{
			return SetYearMon(year, month, -1, setLastMon);
		}

		/// <summary>
		/// 时间不早于
		/// </summary>
		public bool DateIsNotEarlier(DateTime dt)
		{
			if (dt.YearIsNotOK()) return true;
			if (dt.Year < Tasker.Wd.Year) return false;
			if (dt.Year > Tasker.Wd.Year) return true;
			if (dt.Month > Tasker.Wd.Mon) return true;
			return false;
		}

		public uint CountWDays(DateTime from, DateTime to)
		{
			DateTime TheDay = ToDayBegin(from < to ? from : to);
			DateTime To = ToDayEnd(from < to ? to : from);
			uint Cnt = 0;

			for (; TheDay <= To; TheDay = TheDay.AddDays(1))
			{
				bool? InX = InException(TheDay);
				if (InX != null)
				{
					if (InX == true) Cnt++;
					continue;
				}

				bool IsWeekend = (TheDay.DayOfWeek == DayOfWeek.Sunday || TheDay.DayOfWeek == DayOfWeek.Saturday);
				if (!IsWeekend) Cnt++;
			}

			return Cnt;
		}

		bool Reset(string path, int year)
		{
			if (Year == year) return true;

			Year = year;
			XptDays = new List<ExceptDays>();

			if (YearIsNotOK()) return false;

			if (string.IsNullOrEmpty(path)) return false;
			if (!Directory.Exists(path)) return false;
			string FileN = $@"{path}\{Year}.calendar.mpp";
			if (!File.Exists(FileN)) return false;

			UniversalProjectReader reader = new UniversalProjectReader();
			ProjectFile project = reader.read(FileN);
			var RootTasks = project.GetChildTasks();
			var Clds = project.getCalendars();
			var Xps = Clds.GetExceptions();

			foreach (var X in Xps)
			{
				XptDays.Add(new ExceptDays {
					From = ToDayBegin(X.From),
					To = ToDayEnd(X.To),
					IsWorking = X.IsWorking
				});
			}

			WDaysInM = CountWDays(Day1, DayN);
			return true;
		}

		/// <summary>
		/// 特殊休假工作日
		/// </summary>
		public class ExceptDays
		{
			public DateTime From;
			public DateTime To;
			public bool IsWorking;
			public override string ToString()
			{
				string dt;
				if (From.Day == To.Day && From.Month == To.Month && From.Year == To.Year)
				{
					dt = From.ToString("yy/MM/dd");
				}
				else { 
					dt = $"{From.ToString("yy/MM/dd")}-{To.ToString("yy/MM/dd")}";
				}
				string wd = IsWorking ? "W" : "H";
				return $"{wd}@{dt}";
			}
		}

		List<ExceptDays> XptDays;

		bool? InException(DateTime day)
		{
			if (XptDays == null) return null;

			foreach(var M in XptDays)
			{
				if(day >= M.From && day <= M.To)
				{
					return M.IsWorking;
				}
			}
			return null;
		}


		static DateTime ToDayBegin(DateTime dt)
		{
			return new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0);
		}

		static DateTime ToDayEnd(DateTime dt)
		{
			return new DateTime(dt.Year, dt.Month, dt.Day, 23, 59, 59);
		}

		static int GetMonthMaxDay(int year, int month)
		{
			var DayLast = 30;
			switch (month)
			{
				case 1:
				case 3:
				case 5:
				case 7:
				case 8:
				case 10:
				case 12:
					DayLast = 31;
					break;
				case 2:
					DayLast = DateTime.IsLeapYear(year) ? 29 : 28;
					break;
			}

			return DayLast;
		}

	}
}
