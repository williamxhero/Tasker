using System;

namespace Tasker
{
	class Program
	{
		static Tasker Tsk = new Tasker();
		static readonly bool ProductOnly = true;
		static readonly bool TaskerNow = true;
		static readonly int Year = 2020;
		static readonly int Month = 5;

		static void Main(string[] args)
		{
			Tsk.SetDateNow();
			Tsk.SaveProductInfo();

			if (!ProductOnly)
			{
				DoTasks();
			}

			Console.WriteLine("Done");
		}

		private static void DoTasks()
		{
			if (TaskerNow)
			{
				Tsk.SetDateNow();
				Tsk.SaveSpeed();
				//Tsk.SavePubTasks();

				return;
			}

			Tsk.SetDate(Year, Month);
			Tsk.SaveFinishedTasks();
			Tsk.SaveSpeed();
			Tsk.Get0PtsFinishedTasks();
			Tsk.SaveUnfinishedTasks();
		}



	}//class
}//namespace
