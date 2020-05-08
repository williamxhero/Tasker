using net.sf.mpxj;
using MpTask = net.sf.mpxj.Task;
using System.Collections.Generic;

namespace Tasker
{
	public static class MPXJExtension
	{

		public static List<WorkingDate.ExceptDays> GetExceptions(this ProjectCalendarContainer pcc)
		{
			List<WorkingDate.ExceptDays> Ret = new List<WorkingDate.ExceptDays>();

			for (int idx = 0; idx < pcc.size(); idx++)
			{
				var Cld = pcc.get(idx) as ProjectCalendar;
				var XpList = Cld.getCalendarExceptions();

				for (int idx2 = 0; idx2 < XpList.size(); idx2++)
				{
					var Xp = XpList.get(idx2) as ProjectCalendarException;
					Ret.Add(new WorkingDate.ExceptDays {
						From = Xp.getFromDate().ToDt(),
						To = Xp.getToDate().ToDt(),
						IsWorking = Xp.getWorking()
					});
				}

			}

			return Ret;
		}

		/// <summary>
		///  Task. 得到所有前置任务。
		///  0：主任务，1：后续，2：1的后续， ……
		/// </summary>
		public static List<uint> GetPredIds(this MpTask tsk)
		{
			List<uint> Ret = new List<uint>();
			MpTask Tsk = tsk;
			java.util.List Ps = Tsk.getPredecessors();

			while (Ps.size() > 0)
			{
				var Rel = Ps.get(0) as Relation;
				Tsk = Rel.getTargetTask();

				var TId = Tsk.getID().ToString().ToUint();
				if (TId <= 0) continue;

				Ret.Add(TId);
				Ps = Tsk.getPredecessors();
			}
			Ret.Reverse();
			return Ret;
		}

		public static Resource GetResource(this MpTask tsk)
		{
			var ResAs = tsk.getResourceAssignments();
			if (ResAs.size() == 0) return null;

			var ResA = ResAs.get(0) as ResourceAssignment;
			var Reso = ResA.getResource();

			var Res = new Resource() {
				Finisher = Reso.getName().ToMan(),
				FinishedPhs = ResA.getFinish().ToDt(),
				StartedPhs = ResA.getStart().ToDt()
			};

			return Res;
		}



		public static List<MpTask> GetChildTasks(this ProjectFile pf)
		{
			var tasks = pf.getChildTasks();
			return GetSubTasks(tasks);
		}

		public static List<MpTask> GetChildTasks(this MpTask tsk)
		{
			var tasks = tsk.getChildTasks();
			return GetSubTasks(tasks);
		}

		private static List<MpTask> GetSubTasks(java.util.List tasks)
		{
			List<MpTask> ChildTasks = new List<MpTask>();
			for (int Idx = 0; Idx < tasks.size(); Idx++)
			{
				var Ct = tasks.get(Idx) as MpTask;
				if (Ct.getName() == null) continue;
				ChildTasks.Add(Ct);
			}
			return ChildTasks;
		}

	}
}
