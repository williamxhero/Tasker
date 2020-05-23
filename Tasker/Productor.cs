using System;
using System.Collections.Generic;
using System.Linq;

namespace Tasker
{
	partial class Tasker
	{

		List<Plan> GetAllPlans()
		{
			List<Plan> Plans = new List<Plan>();
			var Ps = GetProducts();
			foreach (var P in Ps)
			{
				if (P.Id == 8) continue; //平台任务不列出
				if(P.Plans.Count == 0)
				{
					Plans.Add(new Plan {
						PdId = P.Id, PdName = P.Name, PDM = P.PDM, Title = "", Id = 0});
					continue;
				}

				foreach (var Pl in P.Plans)
				{
					Plans.Add(Pl);
				}
			}

			Plans.Sort((a, b) => {
				if (a.End.Year < 2000) return 1;
				if (b.End.Year < 2000) return -1;
				if (a.End > b.End) return 1;
				if (a.End < b.End) return -1;
				return 0;
			});

			return Plans;
		}

		/// <summary>
		/// 所有项目的情况：
		/// 版本计划，需求，状态等
		/// </summary>
		public bool SaveProductInfo()
		{
			var Pls = GetAllPlans();
			using (var Xls = XApp.New("Products"))
			{
				Xls.EmptySheet();
				Xls.SetSheetName("Products");

				List<string> Titles = new List<string> {
					"产品", "计划", "开始", "发布","PDM","需求","状态","软件开发", "软件状态"
				};
				int LineNo = SetRow(Xls, 1, Titles);

				foreach (var P in Pls)
				{
					string Pdm = string.IsNullOrEmpty(P.PDM) ? "无" : P.PDM;
					List<string> PlRow = new List<string> {
							P.PdName,
							P.Title,
							P.BeginStr,
							P.EndStr,
							Pdm
						};

					int LineFrom = LineNo;
					LineNo = SetRow(Xls, LineNo, PlRow);

					if (P.Needs.Count == 0)
					{
						LineNo = Xls.SetRow(LineNo);
						continue;
					}

					var NInfo = GetNeedsInfo(P.Needs);
					foreach (var Ns in P.Needs)
					{
						List<string> NdsRow = new List<string> {
								"","","","","", Ns.Title
							};

						NeedInfo NI = null;
						NInfo?.TryGetValue(Ns.Id, out NI);

						if (NI != null)
						{
							if (NI.Pctg == 100)
							{
								Ns.Stg = Need.Stage.developed;
							}
							if (NI.Pctg == 99)
							{
								Ns.Stg = Need.Stage.testing;
							}
						}
						NdsRow.Add(Ns.StStgString);

						if (NI != null)
						{
							NdsRow.Add(NI.People);
							NdsRow.Add(NI.Status);
						}

						LineNo = SetRow(Xls, LineNo, NdsRow);
					}//Needs

					//前5 Col merge
					for(int Mc = 1; Mc<6; Mc++)
					{
						Xls.Merge(Mc, LineFrom, LineNo - 1);
					}
					
					LineNo = Xls.SetRow(LineNo);
				}//Products
				Xls.Save();
			}
			return true;
		}

		private static int SetRow(Xlsx Xls, int LineNo, List<string> PdRow)
		{
			Xls.SetRow(LineNo, PdRow);
			Xls.Bold(LineNo);
			Xls.FontMsYH(LineNo);
			Xls.AlignLeft(LineNo);
			return LineNo + 1;
		}

		class NeedInfo
		{
			public uint Id;
			public string People = "";
			public uint Pctg;
			public uint Cnt;
			public string Status = "";
			public override string ToString()
			{
				return $"({Id}){People}/{Pctg/Cnt}%/{Status}";
			}
		}

		Dictionary<uint, NeedInfo> GetNeedsInfo(List<Need> ns)
		{
			if (ns == null || ns.Count == 0) return null;

			var RP = new Dictionary<uint, NeedInfo>();
			var Ts = GetNeedsTasks(ns);
			foreach(var T in Ts)
			{
				if (!RP.ContainsKey(T.Inf.NeedId))
				{
					RP[T.Inf.NeedId] = new NeedInfo { Id = T.Inf.NeedId };
				}
				var NI = RP[T.Inf.NeedId];
				NI.People += ResCtl.ManName2StrName(T.ManRes) + " ";
				NI.Pctg += T.FinDegPhs;
				NI.Cnt++;
			}
			
			foreach(var V in RP.Values)
			{
				float Pctg = V.Pctg / (float)V.Cnt;
				V.Pctg = (uint)MathRound(Pctg);
				if(V.Pctg == 100)
				{
					V.Status = "已完成";
				}
				else if(V.Pctg == 99)
				{
					V.Status = "测试中";
				}
				else if(V.Pctg > 0)
				{
					V.Status = "开发中";
				}
			}
			return RP;
		}

		List<Task> GetNeedsTasks(List<Need> ns)
		{
			if (ns == null || ns.Count == 0) return null;

			var Ids = new List<uint>();
			foreach(var N in ns)
			{
				Ids.Add(N.Id);
			}

			string SqlCond = $"t.story in ({string.Join(",", Ids)})";
			return GetTasks(SqlCond);
		}

		class Need
		{
			public static Status ToStatus(string st)
			{
				switch (st)
				{
					case "draft": return Status.draft;
					case "active": return Status.active;
					case "closed": return Status.closed;
					case "changed": return Status.changed;
				}
				return Status.none;
			}

			public static Stage ToStage(string st)
			{
				switch(st)
				{
					case "wait": return Stage.wait;
					case "planned" : return  Stage.planned;
					case "developing": return Stage.developing;
					case "projected" : return  Stage.projected;
					case "developed" : return  Stage.developed;
					case "testing": return Stage.testing;
					case "tested": return Stage.tested;
					case "verified": return Stage.verified;
					case "released" : return  Stage.released;
					case "closed" : return  Stage.closed;
				}
				return Stage.none;
			}

			public enum Status
			{
				none,
				draft,
				active,
				closed,
				changed,
			}

			public enum Stage
			{
				none,
				wait,
				planned,
				projected,
				developing,
				developed,
				testing,
				tested,
				verified,
				released,
				closed,
			}

			public uint Id;
			public string Title;
			public Status St;
			public string StString { get
				{
					switch (St)
					{
						case Status.draft: return "编写中";
						case Status.active: return "开发中";
						case Status.closed: return "完成";
						case Status.changed: return "已变更";
					}
					return "";
				}
			}
			public Stage Stg;
			public string StgString { get
				{
					switch (Stg)
					{
						case Stage.wait: return "未确认";
						case Stage.planned: return "已确认"; //评审完成
						case Stage.projected: return "已安排"; //安排进迭代
						case Stage.developing: return "开发中";
						case Stage.developed: return "开发完成";
						case Stage.testing: return "测试中";
						case Stage.tested: return "已测试"; 
						case Stage.verified: return "已验收";
						case Stage.released: return "已发布";
						case Stage.closed: return "已关闭";
					}
					return "";
				}
			}
			public string StStgString
			{
				get
				{
					if (St == Status.draft) return "已提交";
					else if (St == Status.changed) return "已变更";
					else if(St== Status.closed) return "已关闭";
					else if (St == Status.active) return StgString;
					return "";
				}
			}
			public override string ToString()
			{
				return $"PL({Id}){Title}({StString}-{StgString})";
			}
			public override int GetHashCode()
			{
				return Id.GetHashCode();
			}
			public override bool Equals(object obj)
			{
				if (!(obj is Need r)) return false;
				return r.Id == Id;
			}

		}

		class Plan
		{
			public uint Id;
			public string Title;
			public DateTime Begin;
			public string BeginStr { get { return Begin.Year < 2000 ? "" : Begin.ToShortDateString(); } }
			public DateTime End;
			public string EndStr { get { return End.Year < 2000 ? "" : End.ToShortDateString(); } }
			public readonly List<Need> Needs = new List<Need>();

			public uint PdId;
			public string PdName;
			public string PDM; //产品经理

			public void AddNeed(Need nd)
			{
				foreach (var N in Needs)
				{
					if (N.Id == nd.Id)
					{
						N.Stg = nd.Stg;
						N.St = nd.St;
						N.Title = nd.Title;
						return;
					}
				}
				Needs.Add(nd);
			}
			public override string ToString()
			{
				return $"PL({Id}){Title}({Begin.ToShortDateString()}-{End.ToShortDateString()})";
			}
			public override int GetHashCode()
			{
				return Id.GetHashCode();
			}
			public override bool Equals(object obj)
			{
				if (!(obj is Plan r)) return false;
				return r.Id == Id;
			}
		}

		class Product
		{
			public uint Id;
			public string Name;
			public string PDM; //产品经理

			public readonly List<Plan> Plans = new List<Plan>();

			public override string ToString()
			{
				return $"PD({Id}){Name}";
			}

			public Plan GetPlan(uint pid)
			{
				foreach(var P in Plans)
				{
					if(P.Id == pid)
					{
						return P;
					}
				}
				var Pl = new Plan { Id = pid, PdId = Id, PdName = Name, PDM = PDM };
				Plans.Add(Pl);
				return Pl;
			}

			public override int GetHashCode()
			{
				return Id.GetHashCode();
			}
			public override bool Equals(object obj)
			{
				if (!(obj is Product r)) return false;
				return r.Id == Id;
			}
		}


		List<Product> GetProducts()
		{
			var Pds = new Dictionary<uint, Product>();

			string Cont = GetHtmlPrdcts();
			if (Cont.Contains("__GETTASK_ERROR__")) return Pds.Values.ToList();

			string[] Lines = Cont.Split("\n".ToCharArray());

			foreach (string Line in Lines)
			{
				if (string.IsNullOrEmpty(Line.Trim())) continue;

				var Cols = Line.Split("\"".ToCharArray());
				int I = 0;
				var PdId = Cols[I++].ToUint();
				if (PdId == 0) continue; //No Product

				Product Pd = null;
				if (Pds.ContainsKey(PdId))
				{
					Pd = Pds[PdId];
				}
				else
				{
					Pd = new Product { Id = PdId};
					Pds[PdId] = Pd;
				}

				Pd.Name = Cols[I++].ToName();
				Pd.PDM = Cols[I++].Trim();

				var PlanId  = Cols[I++].ToUint();
				if (PlanId == 0) continue; //No Plan

				var Plan = Pd.GetPlan(PlanId);
				Plan.Title = Cols[I++].ToName();
				Plan.Begin = Cols[I++].ToDt();
				Plan.End = Cols[I++].ToDt();

				var Nd = new Need {
					Id = Cols[I++].ToUint()
				};
				if (Nd.Id == 0) continue; //No Need.

				Nd.Title = Cols[I++].ToName();
				Nd.St = Need.ToStatus(Cols[I++].Trim());
				Nd.Stg = Need.ToStage(Cols[I++].Trim());
				Plan.AddNeed(Nd);
			}

			return Pds.Values.ToList(); 
		}
	}
}
