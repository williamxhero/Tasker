using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Excel = Microsoft.Office.Interop.Excel;

namespace Tasker
{
	public class XlsxApp
	{
		Excel.Application Xls;

		void NewXls()
		{
			if (Xls == null)
			{
				Xls = new Excel.Application();
			}
		}

		~XlsxApp()
		{
			Xls?.Quit();
			Release(Xls);
		}

		public static void Release<T>(T obj) where T:class
		{
			if (obj == null) return;
			Marshal.FinalReleaseComObject(obj);
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}

		public Xlsx Open(string f)
		{
			return Open(f, 1);
		}

		public Xlsx Open(string f, int s)
		{
			NewXls();
			var X = new Xlsx(Xls);
			X.Load(f, s);
			return X;
		}

		public Xlsx Open(string f, string s)
		{
			NewXls();
			var X = new Xlsx(Xls);
			X.Load(f, s);
			return X;
		}

		public Xlsx New(string f, string s)
		{
			NewXls();
			var X = new Xlsx(Xls);
			X.Create(f, s);
			return X;
		}

		public Xlsx New(string f)
		{
			NewXls();
			var X = new Xlsx(Xls);
			X.Create(f);
			return X;
		}
	}

	public class Xlsx : IDisposable
	{
		public Xlsx(Excel.Application xls)
		{
			Xls = xls;
		}

		Excel.Worksheet Ws = null;
		Excel.Workbook Wb = null;
		Excel.Application Xls = null;
		string FilePath = "Temp";
		
		public void SetSheetName(string name)
		{
			Ws.Name = name;
		}

		public void EmptySheet()
		{
			Ws.UsedRange.Clear();
		}

		public void UseSheet(string name)
		{
			try
			{
				Ws = Wb.Worksheets[name];
				return;
			}
			catch
			{
				Ws = Wb.Worksheets.Add();
				Ws.Name = name;
			}
		}

		public void UseSheet(int idx)
		{
			if (Wb.Worksheets.Count < idx)
			{
				Wb.Worksheets.Add();
			}

			Ws = Wb.Worksheets[idx];
		}

		public void Load(string filePath, int sheet)
		{
			FilePath = filePath;
			Wb = Xls.Workbooks.Open(filePath);
			UseSheet(sheet);
		}

		public void Load(string filePath, string sheet)
		{
			FilePath = filePath;
			Wb = Xls.Workbooks.Open(filePath);
			UseSheet(sheet);
		}

		public void Create(string filePath)
		{
			FilePath = filePath;
			Wb = Xls.Workbooks.Add();
			UseSheet(1);
		}

		public void Create(string filePath, string sheet)
		{
			FilePath = filePath;
			Wb = Xls.Workbooks.Add();
			UseSheet(sheet);
		}

		public void Save()
		{
			Ws.UsedRange.Columns.AutoFit();
			Ws.Columns.AutoFit();
			Ws.SaveAs(FilePath);
		}

		public int SetRow(int row, List<string> Lines = null)
		{
			if(Lines != null)
			{
				for (int CIdx = 1; CIdx <= Lines.Count; CIdx++)
				{
					Cell(row, CIdx, Lines[CIdx - 1]);
				}
			}
			return row+1;
		}

		public void Cell(int row, int col, object val)
		{
			Ws.Cells[row, col].Value2 = val.ToString();
		}

		public void AlignLeft(int row)
		{
			int CC = Ws.UsedRange.Cells.Columns.Count;
			for (int CIdx = 1; CIdx <= CC; CIdx++)
			{
				Ws.Cells[row, CIdx].HorizontalAlignment =Excel.XlHAlign.xlHAlignLeft;
			}
		}

		public void Merge(int col, int rowFrom, int rowTo)
		{
			Excel.Range Rng = Ws.Range[Ws.Cells[rowFrom, col], Ws.Cells[rowTo, col]];
			Rng.Merge();
		}

		public void FontMsYH(int row)
		{
			int CC = Ws.UsedRange.Cells.Columns.Count;
			for (int CIdx = 1; CIdx <= CC; CIdx++)
			{
				Ws.Cells[row, CIdx].Font.Name = "Microsoft YaHei Light";
			}
		}

		public void Bold(int row, int col = 0, bool bold = true)
		{
			if(col > 0)
			{
				Ws.Cells[row, col].Font.Bold = bold;
			}
			else
			{
				int CC = Ws.UsedRange.Cells.Columns.Count;
				for (int CIdx = 1; CIdx <= CC; CIdx++)
				{
					Ws.Cells[row, CIdx].Font.Bold = bold;
				}
			}
		}

		/// <summary>
		/// ForEach Row Until True
		/// </summary>
		public void FeRoUT(Func<int, List<string>, bool> cell)
		{
			int RC = Ws.UsedRange.Cells.Rows.Count;
			int CC = Ws.UsedRange.Cells.Columns.Count;
			for (var Line = 1; Line <= RC; Line++)
			{
				Excel.Range R = Ws.Rows[Line];
				List<string> Objs = new List<string>();
				for (var Col = 1; Col <= CC; Col++)
				{
					Objs.Add(R.Columns[Col].Value2.ToString());
				}
				if (cell(Line, Objs)) return;
			}
		}

		void IDisposable.Dispose()
		{
			XlsxApp.Release(Ws);
			Wb?.Close();
			XlsxApp.Release(Wb);
		}
	}
}
