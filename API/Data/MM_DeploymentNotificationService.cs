using System.Data;
using System.Net;
using Microsoft.Data.SqlClient;
using Taskmanagement_API.Utils;

namespace Taskmanagement_API.Data
{
    /// <summary>
    /// Composes and queues the deployment-approval notification mails.
    /// New request  -> To: approver manager, CC: team lead + requester.
    /// Decision     -> To: requester, CC: team lead + deciding manager.
    /// Mail failures never break the API flow (EmailService sends in background).
    /// </summary>
    public class MM_DeploymentNotificationService
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IEmailService _emailService;
        private readonly string _applicationUrl;

        public MM_DeploymentNotificationService(
            IDbConnectionFactory connectionFactory,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _connectionFactory = connectionFactory;
            _emailService = emailService;
            _applicationUrl = (configuration["SystemSettings:ApplicationUrl"] ?? string.Empty).TrimEnd('/');
        }

        private sealed class RequestMailData
        {
            public decimal Request_ID;
            public decimal Requested_By;
            public string Feature_Module = string.Empty;
            public string Changes_Description = string.Empty;
            public string? Risk_Challenge;
            public string Change_Type = string.Empty;
            public string Status = string.Empty;
            public string? Manager_Remark;
            public DateTime Requested_Date;
            public DateTime? Approved_Date;
            public string Requester_Name = string.Empty;
            public string Requester_Email = string.Empty;
            public string Requester_Designation = string.Empty;
            public string Manager_Name = string.Empty;
            public string Manager_Email = string.Empty;
            public string Lead_Name = string.Empty;
            public string Lead_Email = string.Empty;
            public string Decider_Name = string.Empty;
            public string Decider_Email = string.Empty;
        }

        public async Task NotifyNewRequestAsync(decimal requestId)
        {
            var data = await LoadRequestAsync(requestId);
            if (data == null)
            {
                return;
            }

            var subject = $"Deployment Approval Required | {data.Feature_Module} ({data.Change_Type}) | Request #{data.Request_ID}";

            var intro =
                $"<p style=\"margin:0 0 6px;\">Dear <strong>{Enc(FirstName(data.Manager_Name))}</strong>,</p>" +
                $"<p style=\"margin:0 0 18px;\"><strong>{Enc(data.Requester_Name)}</strong>" +
                (string.IsNullOrEmpty(data.Requester_Designation) ? "" : $" ({Enc(data.Requester_Designation)})") +
                " has submitted a deployment request that requires your approval.</p>";

            var body = BuildMailShell(
                bannerHtml: BuildBanner("#1d4ed8", "ACTION REQUIRED", "Deployment approval requested"),
                contentHtml: intro + BuildDetailsTable(data) + BuildCta("Review &amp; Approve"),
                note: "You can approve or reject this request from the Deployment Approval section in TaskFlow.");

            // CC: every developer & team lead of the requester's team (includes the
            // requester & their lead). No other managers. To: the approving manager.
            var cc = await GetTeamRecipientEmailsAsync(data.Requested_By);
            cc.Add(data.Requester_Email);
            cc.Add(data.Lead_Email);

            _emailService.SendInBackground(
                to: new List<string> { data.Manager_Email },
                cc: cc,
                subject: subject,
                htmlBody: body);
        }

        public async Task NotifyDecisionAsync(decimal requestId)
        {
            var data = await LoadRequestAsync(requestId);
            if (data == null || (data.Status != "Approved" && data.Status != "Rejected"))
            {
                return;
            }

            var approved = data.Status == "Approved";
            var color = approved ? "#059669" : "#dc2626";
            var deciderName = string.IsNullOrEmpty(data.Decider_Name) ? data.Manager_Name : data.Decider_Name;
            var deciderEmail = string.IsNullOrEmpty(data.Decider_Email) ? data.Manager_Email : data.Decider_Email;
            var decidedOn = data.Approved_Date.HasValue ? FormatDate(data.Approved_Date.Value) : FormatDate(DateTime.Now);

            var subject = $"Deployment Request {data.Status} | {data.Feature_Module} | Request #{data.Request_ID}";

            var intro =
                $"<p style=\"margin:0 0 6px;\">Dear <strong>{Enc(FirstName(data.Requester_Name))}</strong>,</p>" +
                $"<p style=\"margin:0 0 18px;\">Your deployment request has been " +
                $"<strong style=\"color:{color};\">{data.Status.ToUpper()}</strong> by " +
                $"<strong>{Enc(deciderName)}</strong> on {decidedOn}.</p>";

            var remarkHtml = string.IsNullOrWhiteSpace(data.Manager_Remark)
                ? string.Empty
                : $@"
                <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0""
                       style=""margin:0 0 18px;border-left:4px solid {color};background-color:{(approved ? "#ecfdf5" : "#fef2f2")};border-radius:0 6px 6px 0;"">
                  <tr>
                    <td style=""padding:12px 16px;"">
                      <div style=""font-size:11px;font-weight:bold;color:{color};text-transform:uppercase;letter-spacing:0.5px;margin-bottom:4px;"">Manager's Remark</div>
                      <div style=""font-size:14px;color:#374151;line-height:1.6;"">{Enc(data.Manager_Remark).Replace("\n", "<br/>")}</div>
                    </td>
                  </tr>
                </table>";

            var body = BuildMailShell(
                bannerHtml: BuildBanner(color, data.Status.ToUpper(), $"Deployment Request {data.Status}"),
                contentHtml: intro + remarkHtml + BuildDetailsTable(data) + BuildCta("View in TaskFlow"),
                note: approved
                    ? "You may proceed with the deployment as per your team's release process."
                    : "Please review the manager's remark, make the required changes and raise a fresh request if needed.");

            // CC: every developer & team lead of the requester's team, plus the
            // deciding manager. To: the requester (removed from CC by EmailService).
            var cc = await GetTeamRecipientEmailsAsync(data.Requested_By);
            cc.Add(data.Lead_Email);
            cc.Add(deciderEmail);

            _emailService.SendInBackground(
                to: new List<string> { data.Requester_Email },
                cc: cc,
                subject: subject,
                htmlBody: body);
        }

        // ── Data ──────────────────────────────────────────────────────────────

        private async Task<RequestMailData?> LoadRequestAsync(decimal requestId)
        {
            const string query = @"
                SELECT
                    r.Request_ID, r.Requested_By, r.Feature_Module, r.Changes_Description, r.Risk_Challenge,
                    r.Change_Type, r.Status, r.Manager_Remark, r.Inserted_Date, r.Approved_Date,
                    req.Employee_Name AS Requester_Name,
                    ISNULL(req.Email_Address, '') AS Requester_Email,
                    ISNULL(d.Designation_Name, '') AS Requester_Designation,
                    ISNULL(mgr.Employee_Name, '') AS Manager_Name,
                    ISNULL(mgr.Email_Address, '') AS Manager_Email,
                    ISNULL(tl.Employee_Name, '') AS Lead_Name,
                    ISNULL(tl.Email_Address, '') AS Lead_Email,
                    ISNULL(app.Employee_Name, '') AS Decider_Name,
                    ISNULL(app.Email_Address, '') AS Decider_Email
                FROM MM_Deployment_Request r
                INNER JOIN MM_Employee req ON req.Employee_ID = r.Requested_By
                LEFT JOIN MM_Designation d ON d.Designation_ID = req.Designation_ID
                LEFT JOIN MM_Employee mgr ON mgr.Employee_ID = r.Approver_Manager_ID
                LEFT JOIN MM_Employee tl ON tl.Employee_ID = req.Team_Lead_ID
                LEFT JOIN MM_Employee app ON app.Employee_ID = r.Approved_By
                WHERE r.Request_ID = @Request_ID";

            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Request_ID", requestId);

            using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            return new RequestMailData
            {
                Request_ID = reader.GetDecimal("Request_ID"),
                Requested_By = reader.GetDecimal("Requested_By"),
                Feature_Module = reader.GetString("Feature_Module"),
                Changes_Description = reader.GetString("Changes_Description"),
                Risk_Challenge = reader.IsDBNull("Risk_Challenge") ? null : reader.GetString("Risk_Challenge"),
                Change_Type = reader.GetString("Change_Type"),
                Status = reader.GetString("Status"),
                Manager_Remark = reader.IsDBNull("Manager_Remark") ? null : reader.GetString("Manager_Remark"),
                Requested_Date = reader.GetDateTime("Inserted_Date"),
                Approved_Date = reader.IsDBNull("Approved_Date") ? null : reader.GetDateTime("Approved_Date"),
                Requester_Name = reader.GetString("Requester_Name"),
                Requester_Email = reader.GetString("Requester_Email"),
                Requester_Designation = reader.GetString("Requester_Designation"),
                Manager_Name = reader.GetString("Manager_Name"),
                Manager_Email = reader.GetString("Manager_Email"),
                Lead_Name = reader.GetString("Lead_Name"),
                Lead_Email = reader.GetString("Lead_Email"),
                Decider_Name = reader.GetString("Decider_Name"),
                Decider_Email = reader.GetString("Decider_Email"),
            };
        }

        // All developers and team leads (designation 2 & 3) that share a team with
        // the requester, via MM_Employee_Team. Managers are intentionally excluded
        // so only the routed/approving manager (added separately) ever gets a mail.
        private async Task<List<string>> GetTeamRecipientEmailsAsync(decimal requesterId)
        {
            var emails = new List<string>();

            const string query = @"
                SELECT DISTINCT ISNULL(e.Email_Address, '') AS Email
                FROM MM_Employee_Team et1
                INNER JOIN MM_Employee_Team et2 ON et2.Team_ID = et1.Team_ID
                INNER JOIN MM_Employee e ON e.Employee_ID = et2.Employee_ID
                WHERE et1.Employee_ID = @Requester_ID
                  AND e.Designation_ID IN (2, 3)
                  AND (e.Is_Deleted IS NULL OR e.Is_Deleted = 0)
                  AND e.Email_Address IS NOT NULL AND e.Email_Address <> ''";

            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Requester_ID", requesterId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var email = reader.GetString("Email");
                if (!string.IsNullOrWhiteSpace(email))
                {
                    emails.Add(email);
                }
            }

            return emails;
        }

        // ── Mail building blocks (inline styles for mail-client compatibility) ─

        private static string Enc(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

        private static string FirstName(string name) =>
            string.IsNullOrWhiteSpace(name) ? "Sir/Madam" : name.Trim().Split(' ')[0];

        private static string FormatDate(DateTime date) => date.ToString("dd MMM yyyy, HH:mm");

        private static string ChangeTypeColor(string type) => type switch
        {
            "Critical" => "#dc2626",
            "Major" => "#d97706",
            _ => "#059669",
        };

        private static string BuildBanner(string color, string tag, string title) => $@"
            <tr>
              <td style=""background-color:{color};padding:22px 32px;"">
                <div style=""font-size:11px;font-weight:bold;color:rgba(255,255,255,0.85);letter-spacing:2px;text-transform:uppercase;margin-bottom:4px;"">{tag}</div>
                <div style=""font-size:20px;font-weight:bold;color:#ffffff;"">{title}</div>
              </td>
            </tr>";

        private string BuildCta(string label)
        {
            if (string.IsNullOrEmpty(_applicationUrl))
            {
                return string.Empty;
            }

            var link = $"{_applicationUrl}/deployment-approval";
            return $@"
                <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""margin:6px 0 8px;"">
                  <tr>
                    <td style=""background-color:#1d4ed8;border-radius:8px;"">
                      <a href=""{link}"" target=""_blank""
                         style=""display:inline-block;padding:12px 28px;font-size:14px;font-weight:bold;color:#ffffff;text-decoration:none;"">{label}</a>
                    </td>
                  </tr>
                </table>
                <p style=""margin:0 0 4px;font-size:12px;color:#9ca3af;"">
                  Or open this link: <a href=""{link}"" style=""color:#1d4ed8;"">{link}</a>
                </p>";
        }

        private static string DetailRow(string label, string valueHtml, bool shade) => $@"
            <tr style=""background-color:{(shade ? "#f9fafb" : "#ffffff")};"">
              <td style=""padding:10px 16px;font-size:11px;font-weight:bold;color:#6b7280;text-transform:uppercase;letter-spacing:0.5px;width:170px;border-bottom:1px solid #f3f4f6;vertical-align:top;white-space:nowrap;"">{label}</td>
              <td style=""padding:10px 16px;font-size:14px;color:#111827;border-bottom:1px solid #f3f4f6;line-height:1.6;"">{valueHtml}</td>
            </tr>";

        private static string BuildDetailsTable(RequestMailData data)
        {
            var rows =
                DetailRow("Request ID", $"#{data.Request_ID}", true) +
                DetailRow("Feature / Module", $"<strong>{Enc(data.Feature_Module)}</strong>", false) +
                DetailRow("Change Category",
                    $"<span style=\"display:inline-block;padding:2px 12px;border-radius:12px;font-size:12px;font-weight:bold;color:#ffffff;background-color:{ChangeTypeColor(data.Change_Type)};\">{Enc(data.Change_Type)}</span>",
                    true) +
                DetailRow("Requested By",
                    Enc(data.Requester_Name) + (string.IsNullOrEmpty(data.Requester_Designation) ? "" : $" <span style=\"color:#9ca3af;\">({Enc(data.Requester_Designation)})</span>"),
                    false) +
                DetailRow("Requested On", FormatDate(data.Requested_Date), true) +
                DetailRow("Changes Made", Enc(data.Changes_Description).Replace("\n", "<br/>"), false);

            if (!string.IsNullOrWhiteSpace(data.Risk_Challenge))
            {
                rows += DetailRow(
                    "<span style=\"color:#d97706;\">&#9888; Risk / Challenges</span>",
                    $"<span style=\"color:#92400e;\">{Enc(data.Risk_Challenge).Replace("\n", "<br/>")}</span>",
                    true);
            }

            return $@"
                <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0""
                       style=""border:1px solid #e5e7eb;border-radius:8px;border-collapse:separate;overflow:hidden;margin:0 0 22px;"">
                  {rows}
                </table>";
        }

        private static string BuildMailShell(string bannerHtml, string contentHtml, string note) => $@"<!DOCTYPE html>
<html>
<body style=""margin:0;padding:0;background-color:#f3f4f6;font-family:Segoe UI,Arial,Helvetica,sans-serif;"">
  <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#f3f4f6;padding:24px 12px;"">
    <tr>
      <td align=""center"">
        <table role=""presentation"" width=""620"" cellpadding=""0"" cellspacing=""0""
               style=""max-width:620px;width:100%;background-color:#ffffff;border-radius:12px;overflow:hidden;border:1px solid #e5e7eb;"">
          {bannerHtml}
          <tr>
            <td style=""padding:28px 32px;font-size:14px;color:#374151;line-height:1.6;"">
              {contentHtml}
              <p style=""margin:14px 0 0;font-size:12px;color:#6b7280;"">{note}</p>
            </td>
          </tr>
          <tr>
            <td style=""background-color:#f9fafb;padding:16px 32px;border-top:1px solid #f3f4f6;"">
              <div style=""font-size:12px;font-weight:bold;color:#4b5563;"">Drona &middot; Deployment Approval</div>
              <div style=""font-size:11px;color:#9ca3af;margin-top:2px;"">This is an automated notification. Please do not reply to this email.</div>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
    }
}
