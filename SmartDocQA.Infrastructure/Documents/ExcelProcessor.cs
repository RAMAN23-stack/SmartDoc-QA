using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeOpenXml;
using SmartDocQA.Core.Interfaces;
using SmartDocQA.Core.Models;

namespace SmartDocQA.Infrastructure.Documents;

public class ExcelProcessor : IExcelProcessor
{
    public async Task<DocumentResult> ProcessAsync(Stream stream, string extension)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        return await Task.Run(() =>
        {
            var result = new DocumentResult();
            
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            
            using var package = new ExcelPackage(memoryStream);
            var rows = new List<DataRow>();
            var textBuilder = new StringBuilder();

            foreach (var worksheet in package.Workbook.Worksheets)
            {
                if (worksheet.Dimension == null)
                {
                    continue;
                }

                textBuilder.AppendLine($"[Sheet: {worksheet.Name}]");
                
                var headers = new List<string>();
                // Extract headers (Row 1)
                for (int col = 1; col <= worksheet.Dimension.Columns; col++)
                {
                    var headerVal = worksheet.Cells[1, col].Text;
                    headers.Add(string.IsNullOrWhiteSpace(headerVal) ? $"Column_{col}" : headerVal.Trim());
                }

                textBuilder.AppendLine(string.Join("\t", headers));

                // Extract data rows (Row 2 onwards)
                for (int row = 2; row <= worksheet.Dimension.Rows; row++)
                {
                    var dataRow = new DataRow();
                    var rowText = new List<string>();

                    // Prepend SheetName metadata to row
                    dataRow.Columns["SheetName"] = worksheet.Name;

                    for (int col = 1; col <= worksheet.Dimension.Columns; col++)
                    {
                        var value = worksheet.Cells[row, col].Text;
                        var headerKey = (col - 1 < headers.Count) ? headers[col - 1] : $"Column_{col}";
                        dataRow.Columns[headerKey] = value;
                        rowText.Add(value);
                    }

                    rows.Add(dataRow);
                    textBuilder.AppendLine(string.Join("\t", rowText));
                }

                textBuilder.AppendLine(); // Empty line between sheets
            }

            result.Text = textBuilder.ToString();
            result.Rows = rows;
            result.PageCount = rows.Count;
            
            return result;
        });
    }
}