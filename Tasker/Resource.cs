using System;
using System.Collections.Generic;

namespace Tasker
{

	public class Person
	{
		public const float MinSpeed = 0.1f;

		public ManName Name;
		public ManPos Pos;
		public string StrName = "--";
		public float Spd = MinSpeed; //prevent div by 0

		public override string ToString()
		{
			return $"({Name})Pos({Pos})Spd({Spd:0.0})";
		}


	}

	public class PosInfo
	{
		/// <summary>
		/// 个数
		/// </summary>
		public int Cnt;
		/// <summary>
		/// 平均速度
		/// </summary>
		public float AveSpd = Person.MinSpeed; //prevent div by 0


		public override string ToString()
		{
			return $"Cnt({Cnt})ASpd({AveSpd:0.0})";
		}
	}

	/// <summary>
	/// 职位
	/// </summary>
	public enum ManPos
	{
		SoftDep, // whole team. software department.
		PM, // Proj Mgr
		Staff, // every one.

		DevTest,
		DevUnity,
		DevWeb,
	}

	/// <summary>
	/// 名字
	/// </summary>
	public enum ManName
	{
		TEAM, // whole team
		HW, // me
		PEER, // somebody

		// personal:
		//CY, //gone.
		GYS,
		HHL,
		//HSL, //gone.
		SL,
		JZT,
		RBW,
		WZZ,
		XG,
		ZSS,
		LPB,
	}

	/// <summary>
	/// 资源的各种统计
	/// </summary>
	public class ResAbility
	{
		public void Init()
		{
			InitPersions();
			InitDevCnt();
			InitSpeedLM();
		}

		/// <summary>
		/// 资源分类
		/// </summary>
		public ManPos GetDevType(ManName m)
		{
			Persons.TryGetValue(m, out Person P);
			if (P == null) return ManPos.Staff;
			return P.Pos;
		}

		/// <summary>
		/// 资源速度
		/// </summary>
		public float GetSpeed(ManName m)
		{
			Persons.TryGetValue(m, out Person P);
			if (P == null) return Person.MinSpeed;
			return P.Spd;
		}

		public float GetSpeed(ManPos dt)
		{
			Position.TryGetValue(dt, out PosInfo P);
			if (P == null) return Person.MinSpeed;
			return P.AveSpd;
		}

		public string ManName2StrName(ManName mn)
		{
			Persons.TryGetValue(mn, out Person P);
			if (P != null) return P.StrName;
			return "";
		}

		public Dictionary<ManName, Person> Persons = new Dictionary<ManName, Person>();
		public Dictionary<ManPos, PosInfo> Position = new Dictionary<ManPos, PosInfo>();

		/// <summary>
		/// Speed Last Month
		/// </summary>
		void InitSpeedLM()
		{
			Dictionary<ManPos, float> DtPts = new Dictionary<ManPos, float>();

			var MS = Tasker.Flr.LoadLastSpdFile();

			float TSpeed = 0;
			float TCnt = 0;

			foreach (var M in MS)
			{
				var Dt = GetDevType(M.Name);
				if (!Persons.ContainsKey(M.Name)) continue;

				Persons[M.Name].Spd = M.Spd;
				TSpeed += M.Spd;
				TCnt++;

				if (!DtPts.ContainsKey(Dt))
				{
					DtPts[Dt] = M.Spd;
				}
				else
				{
					DtPts[Dt] += M.Spd;
				}
			}

			foreach (var Ds in DtPts)
			{
				if (Ds.Key.IsntDeveloper()) continue;

				var Pos = Position[Ds.Key];
				Pos.AveSpd = Ds.Value / Pos.Cnt;
			}

			float ASpeed = Tasker.MathRound(TSpeed / TCnt, 1);
			Position[ManPos.Staff] = new PosInfo { AveSpd = ASpeed, Cnt = (int)TCnt };
			Persons[ManName.PEER] = new Person { Name = ManName.PEER, Pos = ManPos.Staff, Spd = ASpeed };
		}

		void InitDevCnt()
		{
			foreach (var P in Persons)
			{
				if (P.Value.Pos.IsntDeveloper()) continue;

				if (Position.ContainsKey(P.Value.Pos))
				{
					Position[P.Value.Pos].Cnt += 1;
				}
				else
				{
					Position[P.Value.Pos] = new PosInfo { Cnt = 1};
				}
			}
		}

		void InitPersions()
		{
			Persons[ManName.HW] = new Person { Name = ManName.HW, Pos = ManPos.PM};

			Persons[ManName.GYS] = new Person { Name = ManName.GYS, StrName = "谷泳升", Pos = ManPos.DevUnity };
			Persons[ManName.HHL] = new Person { Name = ManName.HHL, StrName = "胡弘磊", Pos = ManPos.DevUnity };
			Persons[ManName.JZT] = new Person { Name = ManName.JZT, StrName = "贾子婷", Pos = ManPos.DevUnity };
			Persons[ManName.RBW] = new Person { Name = ManName.RBW, StrName = "任博文", Pos = ManPos.DevUnity };
			Persons[ManName.XG] = new Person { Name = ManName.XG, StrName = "薛刚", Pos = ManPos.DevUnity };

			Persons[ManName.ZSS] = new Person { Name = ManName.ZSS, StrName = "朱帅帅", Pos = ManPos.DevWeb };
			Persons[ManName.LPB] = new Person { Name = ManName.LPB, StrName = "李鹏斌", Pos = ManPos.DevWeb };

			Persons[ManName.SL] = new Person { Name = ManName.SL, StrName = "石磊", Pos = ManPos.DevTest };
			Persons[ManName.WZZ] = new Person { Name = ManName.WZZ, StrName = "王珍珍", Pos = ManPos.DevTest };

		}

	}
}
