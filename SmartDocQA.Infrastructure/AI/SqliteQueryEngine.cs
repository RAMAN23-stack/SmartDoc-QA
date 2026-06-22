using System;
using System.Data;
using System.Text;
using Microsoft.Data.Sqlite;

namespace SmartDocQA.Infrastructure.AI;

public class SqliteQueryEngine
{
    private SqliteConnection? _connection;

    public void LoadDataTable(DataTable dt)
    {
        // Close existing connection if any
        Clear();

        // Guard: nothing to do if table has no columns
        if (dt.Columns.Count == 0)
            return;

        // Open a persistent in-memory connection
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        // Build CREATE TABLE query — sanitise each column name so the SQL is always valid
        var createTableBuilder = new StringBuilder();
        createTableBuilder.Append("CREATE TABLE df (");

        for (int i = 0; i < dt.Columns.Count; i++)
        {
            var rawName = dt.Columns[i].ColumnName;
            // Replace empty/whitespace-only names with a positional fallback
            var colName = string.IsNullOrWhiteSpace(rawName) ? $"col_{i}" : rawName.Trim();
            // Escape any double-quotes inside the name
            createTableBuilder.Append($"\"{colName.Replace("\"", "\"\"")}\" TEXT");
            if (i < dt.Columns.Count - 1)
                createTableBuilder.Append(", ");
        }
        createTableBuilder.Append(");");

        using (var cmd = _connection.CreateCommand())
        {
            cmd.CommandText = createTableBuilder.ToString();
            cmd.ExecuteNonQuery();
        }

        // Insert rows using parameterised queries (safe against any cell value)
        if (dt.Rows.Count > 0)
        {
            var insertBuilder = new StringBuilder();
            insertBuilder.Append("INSERT INTO df VALUES (");
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                insertBuilder.Append($"$val{i}");
                if (i < dt.Columns.Count - 1)
                    insertBuilder.Append(", ");
            }
            insertBuilder.Append(");");

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = insertBuilder.ToString();

                // Pre-create parameters once, reuse for every row
                var parameters = new SqliteParameter[dt.Columns.Count];
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    parameters[i] = cmd.CreateParameter();
                    parameters[i].ParameterName = $"$val{i}";
                    cmd.Parameters.Add(parameters[i]);
                }

                foreach (DataRow row in dt.Rows)
                {
                    for (int i = 0; i < dt.Columns.Count; i++)
                    {
                        parameters[i].Value = row[i] ?? DBNull.Value;
                    }
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }

    public string ExecuteQuery(string sqlQuery)
    {
        if (_connection == null)
        {
            return "Error: No data loaded in database.";
        }

        // Clean query of markdown markers
        sqlQuery = sqlQuery.Trim();
        if (sqlQuery.StartsWith("```sql"))
        {
            sqlQuery = sqlQuery[6..];
        }
        if (sqlQuery.StartsWith("```"))
        {
            sqlQuery = sqlQuery[3..];
        }
        if (sqlQuery.EndsWith("```"))
        {
            sqlQuery = sqlQuery[..^3];
        }
        sqlQuery = sqlQuery.Trim();

        try
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = sqlQuery;

            using var reader = cmd.ExecuteReader();
            var output = new StringBuilder();

            // Print headers
            for (int i = 0; i < reader.FieldCount; i++)
            {
                output.Append(reader.GetName(i));
                if (i < reader.FieldCount - 1)
                    output.Append("\t");
            }
            output.AppendLine();

            int rowCount = 0;
            while (reader.Read())
            {
                rowCount++;
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    output.Append(reader.GetValue(i)?.ToString() ?? "NULL");
                    if (i < reader.FieldCount - 1)
                        output.Append("\t");
                }
                output.AppendLine();
            }

            if (rowCount == 0)
            {
                return "Query executed successfully, but returned 0 rows.";
            }

            return output.ToString();
        }
        catch (Exception ex)
        {
            throw new Exception($"SQL execution error: {ex.Message}\nQuery: {sqlQuery}");
        }
    }

    public void Clear()
    {
        if (_connection != null)
        {
            try
            {
                _connection.Close();
                _connection.Dispose();
            }
            catch {}
            _connection = null;
        }
    }
}
