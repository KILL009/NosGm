using System;
using System.Net.Mail;
using System.Net;
using System.Threading.Tasks;
using NosTale.ServiceManager.MailService.Configuration;

namespace NosTale.ServiceManager.MailService
{
    public class MailService
    {
        public async static Task GenerateMail(string email, string subject, string header, string text)
        {
            using (MailMessage mail = new MailMessage())
            {
                mail.From = new MailAddress("nosgx.server@gmail.com");
                mail.To.Add($"{email}");
                mail.Subject = subject;
                mail.Body = $"<h1>{header}</h1>\n\n" + text;
                mail.IsBodyHtml = true;

                using (SmtpClient smtp = new SmtpClient())
                {
                    smtp.Credentials = new NetworkCredential("nosgx.server@gmail.com", "NosGXServer2023Admin");
                    smtp.EnableSsl = true;
                    smtp.Send(mail);
                    Console.WriteLine($"Mail has been sent to {email}");
                }
            }
        }

        public async static Task SendMail(string email, string subject, string header, string text)
        {

        }

        public void GenerateStartMail(string email, string subject, string body, string name)
        {
            MailMessage message = new MailMessage();
            message.From = new MailAddress(MailConfiguration.Email);
            message.Subject = $"{subject}";
            message.To.Add(new MailAddress($"{email}"));
            message.Body = 
                "<html><body>" +
                $"<h2>Hello {name},</h2>" +
                $"<br>" +
                $"<br>" +
                $"<h1>{body}</h1>\n\n" +
                $"<br>" +
                $"<br>" +
                $"<br>" +
                $"<br>" +
                $"<br>" +
                "<h1>Greetings,</h1>" +
                "<h1>OneTale Administration Team</h1>" +
                "</body></html>";
            message.IsBodyHtml = true;

            var smtpclient = new SmtpClient(MailConfiguration.SMTPClient)
            {
                Port = 587,
                Credentials = new NetworkCredential(MailConfiguration.Email, MailConfiguration.EmailPassword2),
                EnableSsl = true,
            };
            smtpclient.Send(message);

        }
    }
}