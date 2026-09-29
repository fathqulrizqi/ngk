using System;
using System.Drawing;

namespace NGKBusi.Areas.SCM.Controllers
{
    internal class QRCodeGenerator
    {
        public static object ECCLevel { get; internal set; }

        public QRCodeGenerator()
        {
        }

        internal class QRCode
        {
            internal Bitmap GetGraphic(int v)
            {
                throw new NotImplementedException();
            }
        }

        internal QRCode CreateQrCode(string qrcode, object q)
        {
            throw new NotImplementedException();
        }
    }
}