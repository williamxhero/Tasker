using System;
using System.Collections.Generic;

namespace Tasker
{
	partial class Tasker
	{
		public readonly static string BaseFolder = @"E:\Work\performance\Project";
		//每天工作小时：
		public readonly static float WHrsPD = 7.5f;

		public static WorkingDate Wd = new WorkingDate();
		public static FileData Flr = new FileData();
		public static XlsxApp XApp = new XlsxApp();
		public static Dictionary<uint, Task> Id2Task = new Dictionary<uint, Task>();
		public static ResAbility ResCtl = new ResAbility();

		public static Task GetLastTask(uint id)
		{
			if (id == 0) return null;
			Id2Task.TryGetValue(id, out Task Ret);
			return Ret;
		}

		/// <summary>
		/// Last Month Team Speed.
		/// </summary>
		static T[] GetEnumValues<T>() where T : Enum
		{
			return (T[])Enum.GetValues(typeof(T));
		}

		public void SetDateNow()
		{
			if (!Wd.SetLastYearMonthNow())
				throw new InvalidOperationException($"@ Wd.SetTMonNow() Failed.");

			ResCtl.Init();
		}

		public void SetDate(int year, int month)
		{
			if (!Wd.SetLastYearMonth(year, month))
				throw new InvalidOperationException($"@ Wd.SetTMon({year}, {month}) Failed.");

			ResCtl.Init();
		}
	}
}
