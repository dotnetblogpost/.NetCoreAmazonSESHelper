using System;
 using System.Collections.Generic;
 using System.IO;
 using System.Linq;
 using System.Net;
 using System.Threading.Tasks;
 using Amazon.SimpleEmail;
 using Amazon.SimpleEmail.Model;
 using Microsoft.Extensions.Logging;
 using MimeKit;
 
 namespace CSharpAwsSesServiceHelper.EmailService
 {
      public class AwsEmailService : IAwsEmailService
     {
         private readonly ILogger<AwsEmailService> _logger;
         private readonly IAmazonSimpleEmailService _emailService;
         private readonly AwsEmailServiceOptions _emailOptions;
 
         public AwsEmailService(IAmazonSimpleEmailService emailService, AwsEmailServiceOptions emailOptions,
             ILogger<AwsEmailService> logger)
         {
             _logger = logger;
             _emailService = emailService;
             _emailOptions = emailOptions;
         }
 
         /// <summary>
         ///   /// <summary>
         /// sends email using AWS SES Api
         /// </summary>
         /// </summary>
         /// <param name="to"></param>
         /// <param name="subject"></param>
         /// <param name="body"></param>
         /// <param name="isHtmlBody"></param>
         /// <param name="cc"></param>
         /// <param name="bcc"></param>
         /// <returns></returns>
         public Task<HttpStatusCode> SendEmailAsync(IEnumerable<string> to, string subject, string body, bool isHtmlBody,List<string> cc = null, List<string> bcc = null)
         {
             var emailMessage = BuildEmailHeaders(_emailOptions.Sender, to, cc, bcc, subject);
             var emailBody = BuildEmailBody(body, isHtmlBody);
             emailMessage.Body = emailBody.ToMessageBody();
             return SendEmailAsync(emailMessage);
         }
 
         /// <summary>
         /// sends email using AWS SES Api with attachment support (file paths) 
         /// </summary>
         /// <param name="to">to address</param>
         /// <param name="cc">cc addresses</param>
         /// <param name="bcc"></param>
         /// <param name="subject"></param>
         /// <param name="body"></param>
         /// <param name="isHtmlBody"></param>
         /// <param name="fileAttachmentPath"></param>
         /// <returns></returns>
         public Task<HttpStatusCode> SendEmailWithAttachmentAsync(IEnumerable<string> to, string subject, string body, bool isHtmlBody, string fileAttachmentPath, List<string> cc, List<string> bcc)
         {
             var emailMessage = BuildEmailHeaders(_emailOptions.Sender, to, cc, bcc, subject);
             var emailBody = BuildEmailBody(body,isHtmlBody);
             if (!string.IsNullOrEmpty(fileAttachmentPath))
             {
                 emailBody.Attachments.Add(fileAttachmentPath);
             }
             emailMessage.Body = emailBody.ToMessageBody();
             return SendEmailAsync(emailMessage);
         }
         /// <summary>
         /// sends email using AWS SES Api with attachment support (file streams)
         /// </summary>
         /// <param name="to"></param>
         /// <param name="subject"></param>
         /// <param name="body"></param>
         /// <param name="isHtmlBody"></param>
         /// <param name="fileName"></param>
         /// <param name="fileAttachmentStream"></param>
         /// <param name="cc"></param>
         /// <param name="bcc"></param>
         /// <returns></returns>
         public Task<HttpStatusCode> SendEmailWithAttachmentAsync(IEnumerable<string> to, string subject, string body, bool isHtmlBody,string fileName, Stream fileAttachmentStream, List<string> cc,List<string> bcc)
         {
             var emailMessage = BuildEmailHeaders(_emailOptions.Sender, to, cc, bcc, subject);
             var emailBody = BuildEmailBody(body,isHtmlBody);
             if (fileAttachmentStream != null && !string.IsNullOrEmpty(fileName))
             {
                 emailBody.Attachments.Add(fileName, fileAttachmentStream);
             }
             emailMessage.Body = emailBody.ToMessageBody();
             return SendEmailAsync(emailMessage);
         }
 
         #region helpers
 
         /// <summary>
         /// builds email message body 
         /// </summary>
         /// <param name="body"></param>
         /// <param name="isHtmlBody"></param>
         /// <returns></returns>
         private static BodyBuilder BuildEmailBody(string body,  bool isHtmlBody = true)
         {
             var bodyBuilder = new BodyBuilder();
             if (isHtmlBody)
             {
                 bodyBuilder.HtmlBody = $@"<html>
                                     <head>
                                         <title>SES Email</title>
                                     </head>
                                     <body>
                                         {body}
                                     </body>
                                 </html>"; 
             }
             else
             {
                 bodyBuilder.TextBody = body;
             }
             return bodyBuilder;
         }
         /// <summary>
         /// builds email message headers 
         /// </summary>
         /// <param name="from"></param>
         /// <param name="to"></param>
         /// <param name="cc"></param>
         /// <param name="bcc"></param>
         /// <param name="subject"></param>
         /// <returns></returns>
         private static MimeMessage BuildEmailHeaders(string from, IEnumerable<string> to, IReadOnlyCollection<string> cc,IReadOnlyCollection<string> bcc, string subject)
         {
                 var message = new MimeMessage();
                 message.From.Add(new MailboxAddress(string.Empty, from));
                 foreach (var recipient in to)
                 {
                     message.To.Add(new MailboxAddress(string.Empty, recipient));
                 }
                 if (cc != null && cc.Any())
                 {
                     foreach (var recipient in cc)
                     {
                         message.Cc.Add(new MailboxAddress(string.Empty, recipient));
                     }
                 }
                 if (bcc != null && bcc.Any())
                 {
                     foreach (var recipient in bcc)
                     {
                         message.Bcc.Add(new MailboxAddress(string.Empty, recipient));
                     }
                 }
                 message.Subject = subject;
                 return message;
         }
         /// <summary>
         /// sends email using AWS SES Api - using  SendRawEmail method.
         /// </summary>
         /// <param name="message"></param>
         /// <returns></returns>
         private async Task<HttpStatusCode> SendEmailAsync(MimeMessage message)
         {
             using (var memoryStream = new MemoryStream())
             {
                 await message.WriteToAsync(memoryStream);
                 var sendRequest = new SendRawEmailRequest {RawMessage = new RawMessage(memoryStream)};
                
                     var response = await _emailService.SendRawEmailAsync(sendRequest);
                     if (response.HttpStatusCode == HttpStatusCode.OK)
                     {
                         _logger.LogInformation($"The email with message Id {response.MessageId} sent successfully to {message.To} on {DateTime.UtcNow:O}");
                     }
                     else
                     {
                         _logger.LogError($"Failed to send email with message Id {response.MessageId} to {message.To} on {DateTime.UtcNow:O} due to {response.HttpStatusCode}.");
                     }
                     return response.HttpStatusCode;
             }
         }
 #endregion
     }
 }