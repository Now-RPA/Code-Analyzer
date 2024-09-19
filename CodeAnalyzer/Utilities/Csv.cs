using System.Data;
using System.Text;
using CodeAnalyzer.Models.Rule;

namespace CodeAnalyzer.Utilities;

public static class Csv
{
    private static DataTable ConvertToDataTable(List<RuleCheckResult> ruleCheckResults)
    {
        DataTable dataTable = new();
        dataTable.Columns.Add("Category", typeof(string));
        dataTable.Columns.Add("Name", typeof(string));
        dataTable.Columns.Add("Status", typeof(string));
        dataTable.Columns.Add("Source", typeof(string));
        dataTable.Columns.Add("Comments", typeof(string));
        dataTable.Columns.Add("Description", typeof(string));

        foreach (var ruleCheckResult in ruleCheckResults)
        {
            var row = dataTable.NewRow();
            row["Category"] = ruleCheckResult.Rule.Category;
            row["Source"] = ruleCheckResult.Source;
            row["Name"] = ruleCheckResult.Rule.Name;
            row["Status"] = ruleCheckResult.Status;
            row["Comments"] = ruleCheckResult.Comments;
            row["Description"] = ruleCheckResult.Rule.Description;
            dataTable.Rows.Add(row);
        }

        return dataTable;
    }

    public static void WriteDataTableToCsv(List<RuleCheckResult> ruleCheckResults, string filePath)
    {
        StringBuilder sb = new();
        var dataTable = ConvertToDataTable(ruleCheckResults);

        var columnNames = dataTable.Columns.Cast<DataColumn>().Select(column => EscapeCsvField(column.ColumnName));
        sb.AppendLine(string.Join(",", columnNames));

        foreach (DataRow row in dataTable.Rows)
        {
            var fields = row.ItemArray.Select(field => EscapeCsvField(field?.ToString() ?? string.Empty));
            sb.AppendLine(string.Join(",", fields));
        }

        File.WriteAllText(filePath, sb.ToString());
    }

    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field)) return string.Empty;

        if (field.Contains('"')) field = field.Replace("\"", "\"\"");

        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
            field = $"\"{field}\"";

        return field;
    }
}