using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MailSender.Models
{
    public class CertificateSettings
    {
        public int X { get; set; } = 500;

        public int Y { get; set; } = 400;

        public int FontSize { get; set; } = 48;

        public string FontFamily { get; set; } = "Arial";

        public string Color { get; set; } = "#000000";

        public string Alignment { get; set; } = "Center";

    }
}