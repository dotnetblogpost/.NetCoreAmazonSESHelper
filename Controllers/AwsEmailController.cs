using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CSharpAwsSesServiceHelper.EmailService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace CSharpAwsSesServiceHelper.Controllers
{
    /// <summary>
    /// email controller - responsible for sending emails
    /// </summary>
    [Route("api/[controller]")]
    public class EmailController : ControllerBase
    {
        private readonly IAwsEmailService _awsEmailService;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="awsEmailService"></param>
        public EmailController(IAwsEmailService awsEmailService)
                 {
            _awsEmailService = awsEmailService;
        }
        /// <summary>
        /// sends test email
        /// </summary>
        /// <param name="recipient"></param>
        /// <returns></returns>
        [Route(""), HttpGet]
        [AllowAnonymous]
        public async Task<bool> TestEmail(string recipient)
        {
            var to = recipient.Split(";".ToCharArray()).ToList();
            var responseCode = await _awsEmailService.SendEmailAsync(to, "Test Email", "Test Email from SES");
            return responseCode == HttpStatusCode.OK;
        }
        /// <summary>
        /// sends test email with attachment
        /// </summary>
        /// <returns></returns>
        [Route("attachment"), HttpPost]
        [AllowAnonymous]
        [Consumes("multipart/form-data")]
        public async Task<bool> TestEmailWithAttachment(IFormFile file)
        {
            var fileName = ContentDispositionHeaderValue
                .Parse(file.ContentDisposition)
                .FileName
                .TrimStart().ToString();
            var toAddress = Request.Form.ContainsKey("to") ? Request.Form["to"].ToString() : null;
            if (string.IsNullOrEmpty(toAddress))
            {
                return false;
            }
            HttpStatusCode responseCode;
            var to = toAddress?.Split(";".ToCharArray()).ToList();
            using (var fileStream = file.OpenReadStream())
            using (var ms = new MemoryStream())
            {
                await fileStream.CopyToAsync(ms);
                responseCode = await _awsEmailService.SendEmailWithAttachmentAsync(to, "Test Email", "Test Email from SES",true,fileName:fileName, fileAttachmentStream:ms);
            }
            return responseCode == HttpStatusCode.OK;
        }
        /// <summary>
        /// sends test email with attachment
        /// </summary>
        /// <returns></returns>
        [Route("attachment/filepath"), HttpPost]
        [AllowAnonymous]
        public async Task<bool> TestEmailWithFilePathAttachment()
        {
            var toAddress = Request.Form.ContainsKey("to") ? Request.Form["to"].ToString() : null;
            var filePath = Request.Form.ContainsKey("filePath") ? Request.Form["filePath"].ToString() : null;
            if (string.IsNullOrEmpty(toAddress))
            {
                return false;
            }
            var to = toAddress?.Split(";".ToCharArray()).ToList();
            var responseCode = await _awsEmailService.SendEmailWithAttachmentAsync(to, "Test Email", "Test Email from SES", true,fileAttachmentPath:filePath);
            return responseCode == HttpStatusCode.OK;
        }
    }
}