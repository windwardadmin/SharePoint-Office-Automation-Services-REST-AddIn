using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using OASModels;

namespace OASClient
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length < 2 )
            {
                Console.WriteLine("Please specify file and web-service url to convert...");
                return;
            }
            
            // read file content
            string filename = args[0];
            Stream fs = File.OpenRead(filename);

            string url = args[1]; 
            string domain = "";
            string username = "";
            string password = "";

            if (args.Length >= 5)
            {
                // read auth details
                domain = args[2];
                username = args[3];
                password = args[4];
            }

            // init client
            OASClient client = new OASClient(url, domain, username, password);

            // set options for conversion
            ConversionOptions options = new ConversionOptions();
            options.IncludeDocumentProperties = true;
            options.UsePDFA = true;

            // get immediately converted file

            Stream result = null;
            string ext = Path.GetExtension(filename);

            if(ext.Equals(".docx", StringComparison.InvariantCultureIgnoreCase))
                result = client.Convert(OASModels.DocType.DOCX, fs, options).Result;

            if (ext.Equals(".pptx", StringComparison.InvariantCultureIgnoreCase))
                result = client.Convert(OASModels.DocType.PPTX, fs, options).Result;

            //save result file
            string result_file = Path.GetDirectoryName(filename) + "\\" + Path.GetFileNameWithoutExtension(filename) + ".pdf";
            using (FileStream fs1 = new FileStream(result_file, FileMode.Create))
            { 
                result.CopyTo(fs1);
                fs1.Flush();
            }
            fs.Close();


            // start conversion job
            fs = File.OpenRead(filename);
            Stream result1 = null;
            string FileId = "";
            if (ext.Equals(".docx", StringComparison.InvariantCultureIgnoreCase))
                FileId = client.StartConversionJob(OASModels.DocType.DOCX, fs, options).Result;

            if (ext.Equals(".pptx", StringComparison.InvariantCultureIgnoreCase))
                FileId = client.StartConversionJob(OASModels.DocType.PPTX, fs, options).Result;

            // wait until file will be processed
            do
            {
                System.Threading.Thread.Sleep(1000);
                result1 = client.GetConvertedFile(FileId).Result;
            }
            while (result1 == null);

            //save result file
            string result_file1 = Path.GetDirectoryName(filename) + "\\" + FileId + ".pdf";
            using (FileStream fs1 = new FileStream(result_file1, FileMode.Create))
            {
                result1.CopyTo(fs1);
                fs1.Flush();
            }

        }
    }
}
