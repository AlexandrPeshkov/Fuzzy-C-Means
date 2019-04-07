using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fuzzy_C_Means
{
    public static class ExcelReport
    {
        private static List<KeyValuePair<string, List<List<double>>>> Reports { get; set; } = new List<KeyValuePair<string, List<List<double>>>>();

        private static KeyValuePair<string, Dictionary<int, List<string>>> TotalReport;

        /// <summary>
        /// Записать все отчеты в Excel файл
        /// </summary>
        public static void WriteReports(string fileName = "fuzzy c-menas report.xlsx")
        {
            using (ExcelPackage excel = new ExcelPackage())
            {
                ExcelWorksheet worksheet = excel.Workbook.Worksheets.Add("Fuzzy c-means отчёт");
                worksheet.Cells.AutoFilter = true;

                int verticalOffset = 0;

                foreach (var report in Reports)
                {
                    worksheet.Cells[1 + verticalOffset, 1].Value = report.Key;
                    worksheet.Cells[1 + verticalOffset, 1].Style.Font.Bold = true;

                    if (report.Value != null)
                    {
                        for (var i = 0; i < report.Value.Count; i++)
                        {
                            for (var j = 0; j < report.Value[i].Count; j++)
                            {
                                worksheet.Cells[i + 2 + verticalOffset, j + 1].Value = report.Value[i][j];
                            }
                        }

                        verticalOffset += 2 + (report.Value != null ? report.Value.Count : 0);
                    }
                }

                WriteTotals(worksheet, verticalOffset);

                FileInfo excelFile = new FileInfo(fileName);
                try
                {
                    excel.SaveAs(excelFile);
                }
                catch
                {

                }
            }
        }

        /// <summary>
        /// Добавить отчет на запись в Excel
        /// </summary>
        /// <param name="matrix">Данные</param>
        /// <param name="caption">Описание</param>
        public static void AddReport(List<List<double>> matrix, string caption)
        {
            Reports.Add(new KeyValuePair<string, List<List<double>>>(caption, matrix));
        }

        public static void AddReport(double value, string caption)
        {
            Reports.Add(new KeyValuePair<string, List<List<double>>>(caption, new List<List<double>>() { new List<double>() { value } }));
        }

        public static void AddReport(Dictionary<int, List<string>> clusterObjects, string caption)
        {
            TotalReport = new KeyValuePair<string, Dictionary<int, List<string>>>(caption, clusterObjects);
        }

        public static void WriteTotals(ExcelWorksheet worksheet, int offset)
        {
            worksheet.Cells[1 + offset, 1].Value = TotalReport.Key;
            worksheet.Cells[1 + offset, 1].Style.Font.Bold = true;

            for (var i = 0; i < TotalReport.Value.Count; i++)
            {
                for (var j = 0; j < TotalReport.Value[i].Count; j++)
                {
                    worksheet.Cells[j + 2 + offset, 1+ i].Value = TotalReport.Value[i][j];
                }
            }
        }
    }
}
