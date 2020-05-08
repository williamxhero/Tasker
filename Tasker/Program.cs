using System;

namespace Tasker
{
	class Program
	{
		static Tasker Tsk = new Tasker();

		static void Main(string[] args)
		{
			//Tsk.SetDate(2020, 4);
			//Tsk.SaveFinishedTasks();
			//Tsk.Get0PtsFinishedTasks();
			//Tsk.SaveUnfinishedTasks();

			Tsk.SetDateNow();
			Tsk.SavePubTasks();

			Console.WriteLine("Done");
		}
	}






}//namespace
