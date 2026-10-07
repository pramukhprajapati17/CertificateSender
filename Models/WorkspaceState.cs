using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MailSender.Models
{
    public class WorkspaceState
    {
        public string TemplatePath { get; set; } = string.Empty;

        public string ExcelPath { get; set; } = string.Empty;

        public string ResultExcelPath { get; set; } = string.Empty;

        public CertificateSettings CertificateSettings { get; set; } = new CertificateSettings();

    }
}