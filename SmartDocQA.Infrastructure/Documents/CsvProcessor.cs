using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using SmartDocQA.Core.Interfaces;
using SmartDocQA.Core.Models;

namespace SmartDocQA.Infrastructure.Documents;

public class CsvProcessor : ICsvProcessor
{
    public async Task<DocumentResult> ProcessAsync(Stream stream)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        return await Task.Run(() =>
        {
            var result = new DocumentResult();
            var rows = new List<DataRow>();
            var textBuilder = new StringBuilder();

            using var reader = new StreamReader(memoryStream, Encoding.UTF8);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                HeaderValidated = null,
                BadDataFound = null
            };

            using var csv = new CsvReader(reader, config);

            if (csv.Read())
            {
                csv.ReadHeader();
                var headers = csv.HeaderRecord;

                if (headers != null)
                {
                    textBuilder.AppendLine(string.Join("\t", headers));

                    while (csv.Read())
                    {
                        var dataRow = new DataRow();
                        var rowValues = new List<string>();

                        foreach (var header in headers)
                        {
                            var value = csv.GetField<string>(header) ?? string.Empty;
                            dataRow.Columns[header] = value;
                            rowValues.Add(value);
                        }

                        rows.Add(dataRow);
                        textBuilder.AppendLine(string.Join("\t", rowValues));
                    }
                }
            }

            result.Text = textBuilder.ToString();
            result.Rows = rows;
            result.PageCount = rows.Count;

            return result;
        });
    }
}
