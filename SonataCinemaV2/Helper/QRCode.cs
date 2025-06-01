using QRCoder;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace SonataCinemaV2.Helper
{
    public static class QRCodeHelper
    {
        public static string GenerateQRCode(string data, string fileName)
        {
            try
            {
                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
                QRCode qrCode = new QRCode(qrCodeData);

                using (Bitmap qrCodeImage = qrCode.GetGraphic(20))
                {
                    string uploadsFolder = Path.Combine(System.Web.Hosting.HostingEnvironment.MapPath("~/Content/QRCodes"));

                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string filePath = Path.Combine(uploadsFolder, fileName);
                    qrCodeImage.Save(filePath, ImageFormat.Png);

                    return $"/Content/QRCodes/{fileName}";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"QR Code Generation Error: {ex.Message}");
                return null;
            }
        }

        public static string GenerateTicketQRData(int ticketId, string customerName, string movieName, string showTime, string seats)
        {
            return $"SONATA_CINEMA|TICKET_ID:{ticketId}|CUSTOMER:{customerName}|MOVIE:{movieName}|SHOWTIME:{showTime}|SEATS:{seats}|VALID:TRUE";
        }
    }
}