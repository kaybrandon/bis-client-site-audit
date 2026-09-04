using System.Text;
using BisAudit.Api.Data.Entities;

namespace BisAudit.Api.Services;

public static class ContactExport
{
    public static string ToCsv(ClientAudit audit)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Role,Name,Title,Email,Phone,Department,Company");

        void Row(string role, string? name, string? title, string? email, string? phone, string? dept)
        {
            sb.AppendLine(string.Join(",",
                Csv(role), Csv(name), Csv(title), Csv(email), Csv(phone), Csv(dept), Csv(audit.CompanyName)));
        }

        foreach (var c in audit.Contacts.OrderBy(c => c.Role))
            Row(c.Role.ToString(), c.Name, c.Title, c.Email, c.Phone, null);

        foreach (var w in audit.Workstations.Where(w =>
                     !string.IsNullOrWhiteSpace(w.UserName) || !string.IsNullOrWhiteSpace(w.Email) || !string.IsNullOrWhiteSpace(w.Phone)))
            Row("Workstation user", w.UserName, w.DeviceName, w.Email, w.Phone, w.Department);

        return sb.ToString();
    }

    private static string Csv(string? value)
    {
        var v = value ?? "";
        if (v.Contains(',') || v.Contains('"') || v.Contains('\n'))
            return "\"" + v.Replace("\"", "\"\"") + "\"";
        return v;
    }
}
