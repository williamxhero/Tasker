using System;
using System.Collections.Generic;


namespace Tasker
{
	public partial class Tasker
	{
		class Sql
		{
			static public string InMon(string col) { return $"(t.{col} >= \"{Wd.Day1Str}\" and t.{col} <= \"{Wd.DayNStr}\")";}
			//Finished from this Month
			static public string FFTM { get { return $"t.finishedDate >= \"{Wd.Day1Str}\" OR ({StUnd})"; } }
			static public string CITM { get { return  $"{InMon("closedDate")} and t.status = \"closed\""; } }
			static public string StUnd { get { return $"t.status != \"done\" and t.status != \"closed\" and t.status != \"cancel\""; }}
			static public string StUndInMon { get { return $"t.finishedDate > \"{Wd.DayNStr}\" OR ({StUnd})"; } }
			static public string StFin { get { return "(t.status = \"done\" or t.status = \"closed\")"; } }
			static public string Finished { get { return $"{StFin} and ({InMon("finishedDate")} or {InMon("realStarted")})";  } }
		}

		Dictionary<ManName, ManSpd> GetTesterSpeed()
		{
			var ManTotalPts = new Dictionary<ManName, ManSpd>();
			var Ents = GetTasks(Sql.CITM);
			if (Ents.Count == 0) return ManTotalPts;

			foreach(var M in ResCtl.Persons)
			{
				if(M.Value.Pos == ManPos.DevTest)
				{
					ManTotalPts[M.Value.Name] = new ManSpd { Name = M.Value.Name};
				}
			}

			foreach(var Ent in Ents)
			{
				if (ManTotalPts.ContainsKey(Ent.Res.Closer))
				{
					ManTotalPts[Ent.Res.Closer].TotalPts += Ent.Inf.Points;
				}
			}

			var Days = Wd.CountWDays1ToN();
			foreach(var Pair in ManTotalPts)
			{
				var Ms = ManTotalPts[Pair.Key];
				Ms.Spd = Ms.TotalPts / Days;
			}

			return ManTotalPts;
		}


		/// <summary>
		/// 定期发布的任务
		/// </summary>
		/// <returns></returns>
		public bool SavePubTasks()
		{
			var Ents = GetTasks(Sql.FFTM);
			if (Ents.Count == 0) return false;

			Ents.Sort((a, b) => {
				var IntA = a.Inf.Stat.ToInt();
				var IntB = b.Inf.Stat.ToInt();
				return IntA > IntB ? -1 : IntA < IntB ? 1 : 0;
			});

			var Fd = new FileData.WTFConfig {
				ShowID = false,
				ShowPoints = false,
				ShowEstSch_AsEst = true,
				ShowEstSch_AsRemain = true,
				ShowEstSch_NotWhenDone = true,
				ShowStatus = true,
				ShowStatus_NoWait = true,
			};

			Flr.WriteTaskFile("BUP", Ents, Fd);

			return true;
		}

		/// <summary>
		/// 得到未完成的任务（已开始：进行中/或暂停）
		/// </summary>
		public bool SaveUnfinishedTasks()
		{
			var Ents = GetTasks(Sql.StUndInMon);
			if (Ents.Count == 0) return false;

			Flr.WriteTaskFile("未完成", Ents);
			return true;
		}

		/// <summary>
		/// 需要评估的任务
		/// </summary>
		public void Get0PtsFinishedTasks()
		{
			string SqlCond = $"t.storyPoint = 0";

			var Ents = GetTasks(SqlCond);
			List<Task> ZeroPts = new List<Task>();
			foreach (var Ent in Ents)
			{
				if (Ent.Inf.RealPts > 0f) continue;
				ZeroPts.Add(Ent);
			}

			if (ZeroPts.Count > 0)
			{
				Flr.WriteTaskFile("需评估", ZeroPts, new FileData.WTFConfig { ShowPoints = false });
			}
		}


		/// <summary>
		/// 计算/保存 某月完成的任务，和每个人的速度
		/// </summary>
		public void SaveFinishedTasks()
		{
			var Ents = GetTasks(Sql.Finished);
			Flr.WriteTaskFile("完成", Ents);
		}

		public void SaveSpeed()
		{
			var Ents = GetTasks(Sql.Finished);

			//只统计可用人手
			Dictionary<ManName, ManSpd> ManPts = new Dictionary<ManName, ManSpd>();
			ManSpd All = new ManSpd { Name = ManName.TEAM, Spd = 0, TotalPts = 0 };
			ManPts[ManName.TEAM] = All;

			foreach (var Ent in Ents)
			{
				if (ManPts.ContainsKey(Ent.ManRes))
				{
					ManSpd Ms = ManPts[Ent.ManRes];
					Ms.TotalPts += Ent.PtsPhs;
				}
				else
				{
					ManPts[Ent.ManRes] = new ManSpd { Name = Ent.ManRes, Spd = 0, TotalPts = Ent.PtsPhs };
				}

				All.TotalPts += Ent.PtsPhs;
			}

			All.Spd = MathRound(All.TotalPts / Wd.WDaysInM, 1);

			List<ManSpd> MS = new List<ManSpd>();
			foreach (var M in ManPts.Values)
			{
				M.Spd = M.TotalPts / Wd.WDaysInM;
				MS.Add(M);
			}

			var Ts = GetTesterSpeed();
			foreach(var Tr in Ts)
			{
				MS.Add(Tr.Value);
			}

			MS.Sort((msA, msB) => { return msA.Spd < msB.Spd ? 1 : (msA.Spd > msB.Spd ? -1 : 0); });
			Flr.WriteSpdFile(MS);
		}


		List<Task> NullRepoTasks(List<Task> tsks)
		{
			List<Task> Ret = new List<Task>();
			foreach(var T in tsks)
			{
				if (T.Spt.IterName.IndexOf("Repo")<0)
				{
					Ret.Add(T);
				}
			}
			return Ret;
		}

		List<Task> GetTasksByIds(List<uint> ids)
		{
			if (ids.Count <= 0) return new List<Task> { };

			string SqlCond = $"t.Id in ({string.Join(",", ids)})";
			return GetTasks(SqlCond);
		}

		/// <summary>
		/// GetTasks Not Deleted
		/// </summary>
		public List<Task> GetTasks(string condition)
		{
			return GetTasks(condition, false);
		}

		List<Task> GetTasks(string condition, bool inner=true)
		{
			List<Task> Ents = new List<Task>();
			string Cont = GetHtmlTasks(condition);
			if (Cont.Contains("__GETTASK_ERROR__")) return Ents;

			string[] Lines = Cont.Split("\n".ToCharArray());

			if (!inner)
			{
				Id2Task = new Dictionary<uint, Task>();
			}

			foreach (string Line in Lines)
			{
				if (string.IsNullOrEmpty(Line.Trim())) continue;

				var Cols = Line.Split("\"".ToCharArray());

				var Ent = new Task { };
				uint I = 0;
				Ent.Inf.Id = Cols[I++].ToUint();
				Ent.Inf.Name = Cols[I++].ToName();
				Ent.Inf.Stat = Cols[I++].ToStat();
				Ent.Inf.NeedId = Cols[I++].ToUint();
				
				Ent.Res.Taker = Cols[I++].ToMan();
				Ent.Res.StartedPhs = Cols[I++].ToDt();
				Ent.Res.Finisher = Cols[I++].ToMan();
				Ent.Res.FinishedPhs = Cols[I++].ToDt();
				Ent.Res.Closer = Cols[I++].ToMan();
				Ent.Res.Closed = Cols[I++].ToDt();

				Ent.Inf.RealPts = Cols[I++].ToFloat();
				Ent.Phs.CompDeg = Cols[I++].ToUint();
				Ent.Phs.NextId = Cols[I++].ToUint();
				Ent.Phs.LastId = Cols[I++].ToUint();
				Ent.Inf.EstPts = Cols[I++].ToFloat();

				Ent.Spt.IterId = Cols[I++].ToUint();
				Ent.Spt.IterName = Cols[I++].Trim();
				Ent.Spt.ProjId = Cols[I++].ToUint();
				Ent.Spt.ProjName = Cols[I++].Trim();

				Ents.Add(Ent);
			}

			if (Ents.Count == 0) return Ents;

			LoadPreTasks(Ents);

			Ents = NullRepoTasks(Ents); // ORDER 1
			if (Ents.Count == 0) return Ents;

			Ents = MergeTasks(Ents); // ORDER 2

			//others:
			Ents = SetTaskInfoAsWd(Ents);
			Ents = SplitTaskFinishedBefore20200331(Ents);
			Ents = SortTasksSpt(Ents);
			Ents = FillLastTask(Ents);

			return Ents;
		}

		List<Task> FillLastTask(List<Task> tsks)
		{
			foreach (var T in tsks)
			{
				if (T.Phs.LastId == 0) continue;
				var LTask = GetLastTask(T.Phs.LastId);
				if (LTask == null) continue;
				if (LTask.Inf.Id == 0) continue;
				T.Phs.LastTask = LTask;
				T.Phs.LstCompDeg = LTask.Phs.CompDeg;
			}
			return tsks;
		}

		/// <summary>
		/// 根据 WorkingDay 将任务状态恢复到未开始。
		/// </summary>
		List<Task> SetTaskInfoAsWd(List<Task> tsks)
		{
			var ZeroDate = new DateTime(1, 1, 1, 0, 0, 0);

			foreach (var T in tsks)
			{
				bool NotStartedInM = false;

				if (T.Res.StartedPhs.YearIsOK() && Wd.DateIsNotEarlier(T.Res.StartedPhs))
				{
					T.Res.StartedPhs = ZeroDate;
					NotStartedInM = true;
				}

				if (T.Res.FinishedPhs.YearIsOK() && Wd.DateIsNotEarlier(T.Res.FinishedPhs))
				{
					T.Res.FinishedPhs = ZeroDate;
					NotStartedInM = true;
				}

				if (T.Res.Closed.YearIsOK() && Wd.DateIsNotEarlier(T.Res.Closed))
				{
					T.Res.Closed = ZeroDate;
					T.Inf.Stat = Status.Testing;
				}

				if (NotStartedInM)
				{
					T.Phs.CompDeg = 0;
					T.Inf.Stat = Status.Wait;
				}
			}
			return tsks;
		}

		List<Task> SortTasksSpt(List<Task> tsks)
		{
			tsks.Sort((a, b) => 
			{
				if (a.Spt.IterId == b.Spt.IterId) return 0;
				if (a.Spt.ProjId == b.Spt.ProjId)
				{
					if (a.Spt.IterId < b.Spt.IterId) return -1;
					return 1;
				}
				if (a.Spt.ProjId < b.Spt.ProjId) return -1;
				return 1;
			});
			return tsks;
		}

		void LoadPreTasks(List<Task> tsks)
		{
			List<uint> PreIds = new List<uint>();
			foreach (var Task in tsks)
			{
				if (Task.Phs.LastId > 0)
				{
					PreIds.Add(Task.Phs.LastId);
				}
			}
			if (PreIds.Count == 0) return;

			var Ts = GetTasksByIds(PreIds);
			if (Ts.Count == 0) return;

			foreach (var T in Ts)
			{
				Id2Task[T.Inf.Id] = T;
			}

			LoadPreTasks(Ts);
		}

		/// <summary>
		/// 相同 名称/状态/人员 任务合并
		/// </summary>
		List<Task> MergeTasks(List<Task> tsks)
		{
			Dictionary<string, Task> NameTask = new Dictionary<string, Task>();
			foreach(var T in tsks)
			{
				string St = T.Inf.IsFinished ? "2" : (T.Inf.NotStarted ? "0" : "1");
				string Key = $"{T.Inf.Name.Trim().ToLower()}{St}{T.ManRes}";
				if (NameTask.ContainsKey(Key))
				{
					Task Exist = NameTask[Key];
					Exist.Res.StartedPhs = DtMin(Exist.Res.StartedPhs, T.Res.StartedPhs);
					Exist.Res.FinishedPhs = DtMax(Exist.Res.FinishedPhs, T.Res.FinishedPhs);
					Exist.Res.Closed = DtMax(Exist.Res.Closed, T.Res.Closed);
					Exist.Inf.EstPts += T.Inf.EstPts;
					Exist.Inf.RealPts += T.Inf.RealPts;
				}
				else
				{
					NameTask[Key] = T;
				}
			}

			tsks = new List<Task>();
			foreach (var Pr in NameTask)
			{
				tsks.Add(Pr.Value);
			}
			return tsks;
		}


		/// <summary>
		/// 处理老数据 : 2020.3月份及以前的。
		/// 确保在3月之前开始的任务，得到拆分。
		/// 4月开始，任务会通过禅道保证在当月开始及结束。
		/// </summary>
		List<Task> SplitTaskFinishedBefore20200331(List<Task> tsks)
		{
			if (Wd.Year >= 2020 && Wd.Mon > 3) return tsks;

			foreach (var T in tsks)
			{
				if (!T.Inf.IsFinished)
				{
					//TODO
					//没有完成的，需要向后拆：
					//3月的任务已经全部结束掉了，或者拆了新阶段。所以只需要向前拆。
					//2月及以前的就不管了。
					continue;
				}
				if (T.Res.FinishedPhs.Year >= 2020 && T.Res.FinishedPhs.Month > 3) continue;

				bool StartFinishInSameMonth = (T.Res.StartedPhs.Month == T.Res.FinishedPhs.Month && T.Res.StartedPhs.Year == T.Res.FinishedPhs.Year);
				if (StartFinishInSameMonth) { continue; }

				//3月之前开始的，当时没有拆分任务，那么按 当前完成度，照完时间占比，拆分一下
				var TotalDaySpent = Wd.CountWDays(T.Res.StartedPhs, T.Res.FinishedPhs);
				var SpentThisMonth = Wd.CountWDays(Wd.Day1, T.Res.FinishedPhs);
				var PctgInM = (float)SpentThisMonth / TotalDaySpent;

				T.Phs.LastId = 1;
				T.Phs.LstCompDeg = T.Phs.CompDeg - (uint)(PctgInM * 100f);
				T.Res.StartedPhs = Wd.Day1;
			}
			return tsks;
		}


		DateTime DtMax(DateTime a, DateTime b)
		{
			if (a.Year > b.Year) return a;
			if (a.Year < b.Year) return b;
			if (a.Month > b.Month) return a;
			if (a.Month < b.Month) return b;
			if (a.Day > b.Day) return a;
			return b;
		}

		DateTime DtMin(DateTime a, DateTime b)
		{
			if (a.Year > b.Year) return b;
			if (a.Year < b.Year) return a;
			if (a.Month > b.Month) return b;
			if (a.Month < b.Month) return a;
			if (a.Day > b.Day) return b;
			return a;
		}
	}//class
}
