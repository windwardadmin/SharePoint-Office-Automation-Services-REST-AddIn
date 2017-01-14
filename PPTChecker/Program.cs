using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Office.Server.PowerPoint.Conversion;
using Microsoft.SharePoint;

namespace PPTChecker
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Please specify file and web-service url to convert...");
                return;
            }

            // read file content
            string filename = args[0];
            Stream fs = File.OpenRead(filename);

            string url = args[1];

            MemoryStream outstream = new MemoryStream();
            PdfRequest request = new PdfRequest(fs, ".pptx", outstream);

            SPSite site = new SPSite(url);

            IAsyncResult result = request.BeginConvert(SPServiceContext.GetContext(site), null, null);

            // Use the EndConvert method to get the result. 
            request.EndConvert(result);

            string result_file = Path.GetDirectoryName(filename) + "\\" + Path.GetFileNameWithoutExtension(filename) + ".pdf";
            using (FileStream fs1 = new FileStream(result_file, FileMode.Create))
            {
                outstream.Position = 0;
                outstream.CopyTo(fs1);
                fs1.Flush();
            }
            fs.Close();
        }
    }
}
