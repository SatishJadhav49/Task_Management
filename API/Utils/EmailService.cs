using System.Net;
using System.Net.Mail;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Taskmanagement_API.Models;

namespace Taskmanagement_API.Utils
{
    public interface IEmailService
    {
        /// <summary>
        /// Queues an email for background delivery so the API response is never
        /// blocked by a slow/unreachable SMTP server. Every attempt (sent or
        /// failed) is logged to MM_Mail_Tracking; failures get a [FAILED] prefix.
        /// </summary>
        void SendInBackground(List<string> to, List<string> cc, string subject, string htmlBody);
    }

    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _smtp;
        private readonly string _connectionString;
        private readonly ILogger<EmailService> _logger;

        // Registered as singleton, so it keeps its own connection string instead
        // of depending on the scoped IDbConnectionFactory.
        public EmailService(
            IOptions<SmtpSettings> smtpOptions,
            IConfiguration configuration,
            ILogger<EmailService> logger)
        {
            _smtp = smtpOptions.Value;
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
            _logger = logger;
        }

        public void SendInBackground(List<string> to, List<string> cc, string subject, string htmlBody)
        {
            var toList = Clean(to);
            var ccList = Clean(cc)
                .Where(address => !toList.Contains(address, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (toList.Count == 0)
            {
                _logger.LogWarning("Mail '{Subject}' skipped: no recipient address available.", subject);
                return;
            }

            _ = Task.Run(() => SendAndTrackAsync(toList, ccList, subject, htmlBody));
        }

        private static List<string> Clean(List<string>? addresses) =>
            (addresses ?? new List<string>())
                .Where(address => !string.IsNullOrWhiteSpace(address) && address.Contains('@'))
                .Select(address => address.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

        private async Task SendAndTrackAsync(List<string> to, List<string> cc, string subject, string htmlBody)
        {
            var sent = false;
            try
            {
                using var message = new MailMessage
                {
                    From = new MailAddress(_smtp.FromEmail, _smtp.FromName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true,
                };
                foreach (var address in to)
                {
                    message.To.Add(address);
                }
                foreach (var address in cc)
                {
                    message.CC.Add(address);
                }

                using var client = new SmtpClient(_smtp.Server, _smtp.Port)
                {
                    EnableSsl = _smtp.EnableSsl,
                };
                if (!string.IsNullOrWhiteSpace(_smtp.Password))
                {
                    client.Credentials = new NetworkCredential(_smtp.Username, _smtp.Password);
                }

                // Hard cap so an unreachable SMTP server can't hold the task forever.
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                await client.SendMailAsync(message, cts.Token);
                sent = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send mail '{Subject}' to {To}", subject, string.Join(";", to));
            }

            await TrackAsync(sent ? subject : "[FAILED] " + subject, htmlBody, to, cc);
        }

        private async Task TrackAsync(string subject, string body, List<string> to, List<string> cc)
        {
            try
            {
                const string query = @"
                    INSERT INTO MM_Mail_Tracking ([Subject], [Body], [To], [CC], Inserted_Date)
                    VALUES (@Subject, @Body, @To, @CC, GETDATE())";

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Subject", subject);
                command.Parameters.AddWithValue("@Body", body);
                command.Parameters.AddWithValue("@To", string.Join("; ", to));
                command.Parameters.AddWithValue("@CC", cc.Count > 0 ? string.Join("; ", cc) : (object)DBNull.Value);
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write MM_Mail_Tracking row for '{Subject}'", subject);
            }
        }
    }
}
