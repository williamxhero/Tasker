using System;

namespace Tasker
{
	public class BaseInfo
	{
		public uint Id;
		public string Name;
		public Status Stat;
		public uint NeedId; //需求Id

		//total points
		public float RealPts;
		public float EstPts;

		public bool NotStarted { get { return Stat == Status.Wait; } }
		public bool NotFinished { get { return Stat == Status.Doing || Stat == Status.Pause || Stat == Status.Testing; } }
		public bool IsFinished { get { return Stat == Status.Done; } }
		public bool IsDeleted { get { return Stat == Status.Cancel; } }

		public float Points { get { return (RealPts == 0) ? EstPts : RealPts; } }
		public bool PointsIsEst { get { return RealPts == 0; } }
		public string PointStr { get { return Points == 0 ? "?" : (PointsIsEst ? $"{Points:0.0} ?" : $"{Points:0.0}"); } }
		
		public override string ToString()
		{
			string TaskN = (Name.Length > 4) ? $"{Name.Substring(0, 3)}..." : Name;
			return $"[({Id})\"{TaskN}<{PointStr}>\"{Stat}]";
		}
	}

	/// <summary>
	/// 任务信息
	/// </summary>
	public class Task
	{
		public BaseInfo Inf = new BaseInfo();
		public Sprint Spt = new Sprint();
		public Resource Res = new Resource();
		public Phase Phs = new Phase();

		/// <summary>
		/// 当前人力资源
		/// </summary>
		public ManName ManRes
		{
			get
			{
				if (Inf.IsFinished) return Res.Finisher;
				return Res.Taker;
			}
		}

		public string ManResStr
		{
			get
			{
				if (ManRes == ManName.TEAM) return "";
				if (ManRes == ManName.PEER) return "";
				return ManRes.ToString();
			}
		}

		/// <summary>
		/// 任务阶段点数
		/// </summary>
		public float PtsPhs
		{
			get
			{
				var Comp = Phs.CompDeg > 0 ? Phs.CompDeg : 100f;
				return Tasker.MathRound(Inf.Points * (Comp - Phs.LstCompDeg) / 100f, 1);
			}
		}

		public string PtsPhsStr { get { return $"{PtsPhs:0.0}"; } }

		/// <summary>
		/// 估算阶段工天
		/// </summary>
		public float SchDaysPhs
		{
			get
			{
				float EstDays = 0;
				float Cnt = 0;
				
				float HistoryRate = 3f;//历史数据参考系数	   
				float PeerRate = 2f;//个人系数
				float TeamRate = 1f;//团队系数

				if (Phs.LastId > 0)
				{
					var LastPts = Phs.LastTask.PtsPhs;
					var LastDys = Phs.LastTask.ActDaysPhs;
					EstDays = PtsPhs * LastDys / LastPts;
					EstDays *= HistoryRate;
					Cnt = HistoryRate;
				}

				//已经分配到人了
				if (ManRes.IsSpecific())
				{
					var Spd = Tasker.ResCtl.GetSpeed(ManRes);
					if(Spd == Person.MinSpeed)
					{
						Spd = Tasker.ResCtl.GetSpeed(ManName.PEER);
					}
					EstDays += (PtsPhs / Spd) * PeerRate;
					Cnt += PeerRate;
				}

				//职务平均值
				var Spd1 =  Tasker.ResCtl.GetSpeed(Spt.Pos);
				if(Spd1 == Person.MinSpeed)
				{
					Spd1  = Tasker.ResCtl.GetSpeed(ManPos.Staff);
				}
				EstDays += (PtsPhs / Spd1) * TeamRate;
				Cnt += TeamRate;

				//取平均值
				return Tasker.MathRound(EstDays / Cnt, 1);
			}
		}

		/// <summary>
		/// 估算阶段工天字串
		/// </summary>
		public string SchDaysPhsStr { get { return SchDaysPhs == 0 ? "" : $"{SchDaysPhs:0.0}"; } }

		/// <summary>
		/// 估算阶段工时
		/// </summary>
		public float SchHrsPhs { get { return SchDaysPhs * Tasker.WHrsPD; } }

		/// <summary>
		/// 估算阶段工时字串
		/// </summary>
		public string SchHrsPhsStr { get { return SchHrsPhs == 0 ? "" : $"{SchHrsPhs:0.0}"; } }

		/// <summary>
		/// 实际阶段工天
		/// </summary>
		public float ActDaysPhs
		{
			get
			{
				if (Inf.IsFinished)
				{
					return Tasker.Wd.CountWDays(Res.StartedPhs, Res.FinishedPhs);
				}
				else
				{
					if (Res.StartedPhs.YearIsNotOK()) return 0;
					return Tasker.Wd.CountWDays(Res.StartedPhs, Tasker.Wd.DayN);
				}
			}
		}

		/// <summary>
		/// 实际阶段工天字串
		/// </summary>
		public string ActDaysPhsStr { get { return ActDaysPhs ==0 ? "" : $"{ActDaysPhs:0.0}"; } }

		/// <summary>
		/// 阶段完成度  0 - 100
		/// </summary>
		public uint FinDegPhs
		{
			get
			{
				//已结束：阶段完成
				if (Inf.IsFinished) return Phs.CompDeg;

				//未开始：
				if (Inf.NotStarted) return Phs.LstCompDeg;

				//已开始, 未结束：
				uint RemainPctg = 0;
				//测试中，进度 99
				if (Inf.Stat == Status.Testing)
				{
					RemainPctg = 100 - Phs.LstCompDeg - 1;
				}
				else
				{
					var Days = Tasker.Wd.CountWDays(Res.StartedPhs, Tasker.Wd.DayN);
					if (Days < SchDaysPhs) {
						// 预估周期内
						RemainPctg = (uint)Tasker.MathRound((Days / SchDaysPhs * 100) - Phs.LstCompDeg);
					} else {
						// 超过预估时间
						RemainPctg = (uint)Tasker.MathRound((100 - Phs.LstCompDeg) * 0.618f); 
					}
				}
				var Total = Phs.LstCompDeg + RemainPctg;
				return Total;
			}
		}

		public override string ToString()
		{
			return $"{Inf}{Res}{Phs}{Spt}";
		}
	}

	public class Resource
	{
		public ManName Taker; //目前所有者
		public ManName Finisher; //完成者
		public ManName Closer; //关闭者

		public DateTime StartedPhs;
		public DateTime FinishedPhs;
		public DateTime Closed;

		public string StartedStr { get { return StartedPhs.YearIsNotOK() ? "" : StartedPhs.ToString("yyyy/MM/dd"); } }
		public string FinishedStr { get { return FinishedPhs.YearIsNotOK() ? "" : FinishedPhs.ToString("yyyy/MM/dd"); } }
		public string ClosedStr { get { return Closed.YearIsNotOK() ? "" : Closed.ToString("yyyy/MM/dd"); } }

		public override string ToString()
		{
			string TStr = (Taker == ManName.TEAM) ? "?" : Taker.ToString();
			string FStr = (Finisher == ManName.TEAM) ? "?" : Finisher.ToString();
			string CStr = (Closer == ManName.TEAM) ? "?" : Closer.ToString();
			string SDt = (StartedPhs.YearIsNotOK()) ? "--" : StartedPhs.ToString("yyMMdd");
			string FDt = (FinishedPhs.YearIsNotOK()) ? "--" : FinishedPhs.ToString("yyMMdd");
			string CDt = (Closed.YearIsNotOK()) ? "--" : Closed.ToString("yyMMdd");
			return $"[S:{TStr}@{SDt};F:{FStr}@{FDt};C:{CStr}@{CDt}]";
		}
	}

	/// <summary>
	/// 迭代信息
	/// </summary>
	public class Sprint
	{
		public uint IterId; //迭代 ID
		public string IterName; //迭代名
		public uint ProjId; //项目 ID
		public string ProjName { get { return _ProjName; } set { SetProjPos(value); } }//项目名
		public ManPos Pos;
		public override string ToString()
		{
			return $"[P({ProjId}):\"{ProjName}\";S({IterId}):\"{IterName}\"]";
		}

		string _ProjName;
		void SetProjPos(string projName)
		{
			_ProjName = projName;
			Pos = (projName.ToLower().IndexOf("web") >= 0) ? ManPos.DevWeb : ManPos.DevUnity;
		}
	}

	public class Phase
	{
		/// <summary>
		/// 完成度 0 - 100
		/// </summary>
		public uint CompDeg;
		/// <summary>
		/// 上阶段ID
		/// </summary>
		public uint LastId;
		/// <summary>
		/// 上阶段完成度
		/// </summary>
		public uint LstCompDeg;
		/// <summary>
		/// 上阶段任务
		///</summary>
		public Task LastTask;

		public uint NextId;

		public override string ToString()
		{
			if(LastId > 0) return $"[({LastId}){LstCompDeg}%->{CompDeg}%]";
			return $"[{CompDeg}%]";
		}
	}


	public class ManSpd
	{
		public ManName Name;
		public float TotalPts; // this month
		public float Spd;// pts / day
		public override string ToString()
		{
			return $"{Name} {TotalPts}pts {Spd}p/d";
		}
	}

	public enum Status
	{
		No,
		Cancel,
		Wait,
		Doing,
		Pause,
		//DevDone,
		Testing,
		Done
	}

}
