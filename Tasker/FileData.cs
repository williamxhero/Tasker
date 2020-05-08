
using System;
using System.Collections.Generic;
using System.IO;

namespace Tasker
{
	public class FileData
	{

		string GetTaskFileN(string Dt, string fN)
		{
			string Folder = $@"{Tasker.BaseFolder}\{Dt}";
			if (Directory.Exists(Folder))
			{
				Directory.CreateDirectory(Folder);
			}
			return $@"{Folder}\{Dt}{fN}.xlsx";
		}

		public string GetTaskFileN(string fN)
		{
			return GetTaskFileN(Tasker.Wd.Digits4InM, fN);
		}

		public string[] GetSpdFileNLM()
		{
			return new string[] { $@"{Tasker.BaseFolder}\{Tasker.Wd.Last.Year}.速度.xlsx", $"{Tasker.Wd.Last.Mon}月速度" };
		}

		public string[] GetSpdFileN()
		{
			return new string[] { $@"{Tasker.BaseFolder}\{Tasker.Wd.Year}.速度.xlsx", $"{Tasker.Wd.Mon}月速度" };
		}

		public class WTFConfig
		{
			public bool ShowID = true; //[禅道ID]
			public bool ShowPoints = true; //[点数]
			public bool ShowEstSch = true; //[(剩余)工期(估算)]
			public bool ShowEstSch_AsEst = false; //显示“估算”二字
			public bool ShowEstSch_AsRemain = false; //显示“剩余”二字
			public bool ShowEstSch_NotWhenDone = false; //完成的任务，不在显示估算工期
			public bool ShowActSch = true; //[实际工期]（已进行）
			public bool ShowTStarted = true; //[开始日期]
			public bool ShowTFinished = true; //[完成日期]
			public bool ShowResource = true; //[资源名称]
			public bool ShowPctg = true; //[完成百分比]
			public bool ShowPctg_DoneAsTested = false; //完成度以测试为主
			public bool ShowStatus = false; //[状态]
			public bool ShowStatus_NoWait = false; //不显示“未开始”
		}

		public void WriteTaskFile(string fileName, List<Task> ents, WTFConfig wtf=null)
		{
			WTFConfig Cfg = wtf ?? new WTFConfig();
			string FileName = GetTaskFileN(fileName);

			List<string> Titles = new List<string> { "任务名称" };

			if (Cfg.ShowEstSch)
			{
				string Sch = "工期";
				if(Cfg.ShowEstSch_AsEst)
				{
					Sch = Sch + "[预估]";
				}
				if (Cfg.ShowEstSch_AsRemain)
				{
					Sch = "剩余" + Sch;
				}
				Titles.Add(Sch);
			}

			if (Cfg.ShowActSch) Titles.Add("实际工期");
			if (Cfg.ShowTStarted) Titles.Add("开始时间");
			if (Cfg.ShowTFinished) Titles.Add("完成时间");
			if (Cfg.ShowResource) Titles.Add("资源名称");
			if (Cfg.ShowStatus) Titles.Add("状态");
			if (Cfg.ShowPctg) Titles.Add("完成百分比");

			using (var Xls = Tasker.XApp.New(FileName))
			{
				Xls.EmptySheet();
				Xls.SetSheetName(fileName);
				var LineNo = Xls.SetRow(Titles, 1);

				foreach (var Ent in ents)
				{
					if (Ent == null) continue;

					var Ids = Cfg.ShowID ? $"({GetIdStr(Ent)})" : "";

					//Points:
					var PtsStr = "";
					if (Cfg.ShowPoints)
					{
						var Pts = GetPtsStr(Ent);
						PtsStr = $"<{Pts}>";
					}

					string NextId = "";
					if (Ent.Phs.NextId > 0)
					{
						NextId = $"-({Ent.Phs.NextId})";
					}

					string Title = $"{Ids}[{Ent.Spt.IterName}] {Ent.Inf.Name}{PtsStr}{NextId}";

					List<string> Line = new List<string> { Title };

					if (Cfg.ShowEstSch)
					{
						bool NoVal = Cfg.ShowEstSch_NotWhenDone && Ent.Inf.IsFinished;
						Line.Add(NoVal ? "" : Ent.SchDaysPhsStr);
					}
					if (Cfg.ShowActSch) Line.Add(Ent.ActDaysPhsStr);
					if (Cfg.ShowTStarted) Line.Add(Ent.Res.StartedStr);
					if (Cfg.ShowTFinished) Line.Add(Ent.Res.FinishedStr);
					if (Cfg.ShowResource) Line.Add(Ent.ManResStr);

					if (Cfg.ShowStatus)
					{
						var St = Ent.Inf.Stat;
						if (Cfg.ShowStatus_NoWait && Ent.Inf.Stat == Status.Wait)
						{
							St = Status.No;
						}
						Line.Add(St.ToText());
					}

					if (Cfg.ShowPctg)
					{
						uint FinishDeg = Ent.FinDegPhs;

						if (Cfg.ShowPctg_DoneAsTested)
						{
							if (Ent.Inf.Stat == Status.Done)
							{
								FinishDeg = 99;
							}
							if (Ent.Inf.Stat == Status.Closed)
							{
								FinishDeg = 100;
							}
						}

						//已开始，但是0%，改为3%
						if (Ent.Inf.Stat == Status.Doing && FinishDeg == 0)
						{
							FinishDeg = 3;
						}

						Line.Add($"{FinishDeg}%");
					}

					LineNo = Xls.SetRow(Line, LineNo);
				}

				Xls.Save();
			}
		}

		private static string GetPtsStr(Task Ent)
		{
			// 没有阶段点数
			if (Ent.PtsPhs == Ent.Inf.TPoints)
			{
				return Ent.Inf.TPointStr;
			}
			return $"{Ent.PtsPhsStr}/{Ent.Inf.TPointStr}";
		}

		private static string GetIdStr(Task Ent)
		{
			string Ids = $"{Ent.Inf.Id}";
			if (Ent.Phs.LastId > 0)
			{
				Ids = $"{Ent.Phs.LastId}-" + Ids;
			}
			return Ids;
		}

		public List<ManSpd> LoadLastSpdFile()
		{
			List<ManSpd> MSs = new List<ManSpd>();

			var FnSn = GetSpdFileNLM();
			using (var Xls = Tasker.XApp.Open(FnSn[0], FnSn[1]))
			{
				Xls.FeRoUT((l, strs) =>
				{
					if (l == 1) return false;

					var Mn =(ManName)Enum.Parse(typeof(ManName), strs[0]);
					var Ms = new ManSpd { Name = Mn };

					float.TryParse(strs[1], out Ms.TotalPts);
					float.TryParse(strs[2], out Ms.Spd);
					MSs.Add(Ms);

					return false;
				});
			}
			return MSs;
		}

		public void WriteSpdFile(List<ManSpd> ms)
		{
			string[] FNSN= GetSpdFileN();

			List<string> Titles = new List<string> {
				"人员", "点数", "速度"
			};

			using (var Xls = Tasker.XApp.Open(FNSN[0], FNSN[1]))
			{
				Xls.EmptySheet();
				var LineNo = Xls.SetRow(Titles, 1);

				foreach (var M in ms)
				{
					List<string> Line = new List<string> { M.Name.ToString(), M.TotalPts.ToString("0.0"), M.Spd.ToString("0.0") };
					LineNo = Xls.SetRow(Line, LineNo);
				}

				Xls.Save();
			}
		}

	}//class
}
