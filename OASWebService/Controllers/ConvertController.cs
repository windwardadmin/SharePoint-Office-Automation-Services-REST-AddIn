using System;
using System.Security.Principal;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Client;
using Microsoft.Office.Word.Server.Conversions;
using System.DirectoryServices.AccountManagement;
using System.Threading.Tasks;
using System.Configuration;
using Microsoft.Office.Server.PowerPoint.Conversion;
using System.IO;

using OASModels;

namespace OASWebService.Controllers
{
    public class PPTJob
    {
        public SPWeb web;
        public string jobId;
        public string source;
        public string dest;
        public MemoryStream output;
        public PdfRequest request;

        public PPTJob()
        {
            jobId = Guid.NewGuid().ToString();
        }
    }

    public class ConvertController : ApiController
    {
        static string OAS_LIST = ConfigurationManager.AppSettings["OAS_LIST"];
        static string OAS_LIBRARY = ConfigurationManager.AppSettings["OAS_LIBRARY"];
        static int TIME_LIMIT = Int32.Parse(ConfigurationManager.AppSettings["TimeLimit"]);

        const string EXTENSION = ".pdf";

        [System.Runtime.InteropServices.DllImport("advapi32.dll")]
        public static extern bool LogonUser(string userName, string domainName, string password, int LogonType, int LogonProvider, ref IntPtr phToken);

        // closes open handes returned by LogonUser
        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public extern static bool CloseHandle(IntPtr handle);

        [HttpGet]
        [Route("api")]
        public string Get()
        {
            return "OASWebservice works!";
        }

        [HttpPost]
        [Route("api/convert/file/{doctype}")]
        public async Task<OASResponse> ConvertFile(DocType doctype, OASModels.ConversionSettings settings)
        {
            OASResponse oasResponse = new OASResponse();

            try
            {
                SPUserToken userToken = GetUserToken(settings, ref oasResponse);
                if (oasResponse.ErrorCode != OASErrorCodes.Success)
                    return oasResponse;
                
                switch (doctype)
                {
                    case DocType.DOCX:

                        // process using provided user token
                        oasResponse = ConvertWordImmediately(settings, userToken);

                        break;
                    
                    case DocType.PPTX:
                        
                        // process using provided user token
                        oasResponse = ConvertPPTImmediately(settings, userToken);

                        break;
                }

            }
            catch(Exception ex)
            {
                oasResponse.ErrorCode = OASErrorCodes.ErrUnknown;
                oasResponse.Message = ex.Message;
            }
            
            return oasResponse;
        }

        [HttpPost]
        [Route("api/convert/job/{doctype}")]
        public async Task<OASResponse> StartConvertJob(DocType doctype, OASModels.ConversionSettings settings)
        {
            OASResponse oasResponse = new OASResponse();

            try
            {
                SPUserToken userToken = GetUserToken(settings, ref oasResponse);
                if (oasResponse.ErrorCode != OASErrorCodes.Success)
                    return oasResponse;

                switch (doctype)
                {
                    case DocType.DOCX:

                       // process using provided user token
                        oasResponse = StartWordConversion(settings, userToken);
                        
                        break;

                    case DocType.PPTX:

                        // process using provided user token
                        oasResponse = StartPPTConversion(settings, userToken);

                        break;
                }

            }
            catch (Exception ex)
            {
                oasResponse.ErrorCode = OASErrorCodes.ErrUnknown;
                oasResponse.Message = ex.Message;
            }

            return oasResponse;
        }

        [HttpPost]
        [Route("api/convert/getfile/{fileid}")]
        public async Task<OASResponse> GetFile(string fileid, OASModels.ConversionSettings settings)
        {
            OASResponse oasResponse = new OASResponse();

            try
            {
                SPUserToken userToken = GetUserToken(settings, ref oasResponse);
                if (oasResponse.ErrorCode != OASErrorCodes.Success)
                    return oasResponse;

                // process using provided user token
                oasResponse = GetConvertedFile(fileid, settings, userToken);
                              
            }
            catch (Exception ex)
            {
                oasResponse.ErrorCode = OASErrorCodes.ErrUnknown;
                oasResponse.Message = ex.Message;
            }

            return oasResponse;
        }


        private OASResponse ConvertWordImmediately(OASModels.ConversionSettings settings, SPUserToken userToken)
        {
            OASResponse oasResponse = new OASResponse();

            ConversionJobSettings set = FillWordConversionOptions(settings.Options);
            set.OutputFormat = SaveFormat.PDF;

            SyncConverter syncConv = new SyncConverter(ConfigurationManager.AppSettings["WASName"], set);
            if(userToken != null)
                syncConv.UserToken = userToken;

            byte[] input = Convert.FromBase64String(settings.Content);
            byte[] output;
            ConversionItemInfo convInfo = syncConv.Convert(input, out output);
            if (convInfo.Succeeded)
            {
                oasResponse.Content = Convert.ToBase64String(output);
                oasResponse.ErrorCode = OASErrorCodes.Success;
            }
            else
            {
                oasResponse.ErrorCode = OASErrorCodes.ErrFailedConvert;
                oasResponse.Message = convInfo.ErrorMessage;
            }

            return oasResponse;
        }

        private OASResponse ConvertPPTImmediately(OASModels.ConversionSettings settings, SPUserToken userToken)
        {
            OASResponse oasResponse = new OASResponse();

            using (SPSite site = (userToken == null ? new SPSite(ConfigurationManager.AppSettings["SiteUrl"]) : new SPSite(ConfigurationManager.AppSettings["SiteUrl"], userToken)))
            {
                using (SPWeb web = site.OpenWeb())
                {
                    byte[] input = Convert.FromBase64String(settings.Content);
                    MemoryStream outstream = new MemoryStream();

                    try
                    {
                        Microsoft.Office.Server.PowerPoint.Conversion.FixedFormatSettings ppsettings = FillPowerPointConversionOptions(settings.Options);

                        PdfRequest request = new PdfRequest(new MemoryStream(input), ".pptx", ppsettings, outstream);

                        IAsyncResult result = request.BeginConvert(SPServiceContext.GetContext(site), null, null);

                        // Use the EndConvert method to get the result. 
                        request.EndConvert(result);

                        oasResponse.Content = Convert.ToBase64String(outstream.ToArray());
                        oasResponse.ErrorCode = OASErrorCodes.Success;
                    }
                    catch (Exception ex)
                    {
                        oasResponse.ErrorCode = OASErrorCodes.ErrFailedConvert;
                        oasResponse.Message = ex.Message;
                    }
                }
            }

            return oasResponse;
        }

        private OASResponse StartWordConversion(OASModels.ConversionSettings settings, SPUserToken userToken)
        {
            OASResponse oasResponse = new OASResponse();

            using (SPSite site = (userToken == null ? new SPSite(ConfigurationManager.AppSettings["SiteUrl"]) : new SPSite(ConfigurationManager.AppSettings["SiteUrl"], userToken)))
            {
                using (SPWeb web = site.OpenWeb())
                {
                    try
                    {
                        web.AllowUnsafeUpdates = true;

                        byte[] input = Convert.FromBase64String(settings.Content);

                        SPFolder lib = GetOASLibrary(web);

                        //add source file to library
                        string source = Guid.NewGuid().ToString();
                        SPFile srcfile = lib.Files.Add(source, input, true);

                        string dest = source + EXTENSION;


                        //Set up the job
                        ConversionJobSettings set = FillWordConversionOptions(settings.Options);
                        set.OutputFormat = SaveFormat.PDF;

                        ConversionJob syncConv = new ConversionJob(ConfigurationManager.AppSettings["WASName"], set);
                        if (userToken != null)
                            syncConv.UserToken = userToken;

                        syncConv.AddFile(web.Url + "/" + lib.Url + "/" + source, web.Url + "/" + lib.Url + "/" + dest);
                        syncConv.Start();

                        // put file to the processing list
                        AddFileToList(web, syncConv.JobId.ToString(), dest, DocType.DOCX);

                        oasResponse.FileId = syncConv.JobId.ToString();
                        oasResponse.ErrorCode = OASErrorCodes.Success;
                    }
                    catch (Exception ex)
                    {
                        oasResponse.ErrorCode = OASErrorCodes.ErrFailedConvert;
                        oasResponse.Message = ex.Message;
                    }
                }
            }

            return oasResponse;
        }


        private OASResponse StartPPTConversion(OASModels.ConversionSettings settings, SPUserToken userToken)
        {
            OASResponse oasResponse = new OASResponse();

            using (SPSite site = (userToken == null ? new SPSite(ConfigurationManager.AppSettings["SiteUrl"]) : new SPSite(ConfigurationManager.AppSettings["SiteUrl"], userToken)))
            {
                using (SPWeb web = site.OpenWeb())
                {
                    try
                    {
                        web.AllowUnsafeUpdates = true;

                        byte[] input = Convert.FromBase64String(settings.Content);

                        SPFolder lib = GetOASLibrary(web);

                        //add source file to library
                        string source = Guid.NewGuid().ToString();
                        SPFile srcfile = lib.Files.Add(source, input, true);

                        string dest = source + EXTENSION;


                        //Set up the job
                        MemoryStream ms = new MemoryStream();
                        Microsoft.Office.Server.PowerPoint.Conversion.FixedFormatSettings ppsettings = FillPowerPointConversionOptions(settings.Options);

                        PdfRequest request = new PdfRequest(new MemoryStream(input), ".pptx", ppsettings, ms);

                        PPTJob job = new PPTJob();
                        job.source = source;
                        job.dest = dest;
                        job.web = web;
                        job.output = ms;
                        job.request = request;

                        IAsyncResult result = request.BeginConvert(SPServiceContext.GetContext(web.Site), PPTConversionFinished, job);

                        // put file to the processing list
                        AddFileToList(web, job.jobId.ToString(), dest, DocType.PPTX);

                        oasResponse.FileId = job.jobId.ToString();
                        oasResponse.ErrorCode = OASErrorCodes.Success;
                    }
                    catch (Exception ex)
                    {
                        oasResponse.ErrorCode = OASErrorCodes.ErrFailedConvert;
                        oasResponse.Message = ex.Message;
                    }
                }
            }

            return oasResponse;
        }

        private OASResponse GetConvertedFile(string fileid, OASModels.ConversionSettings settings, SPUserToken userToken)
        {
            OASResponse oasResponse = new OASResponse();

            using (SPSite site = (userToken == null ? new SPSite(ConfigurationManager.AppSettings["SiteUrl"]) : new SPSite(ConfigurationManager.AppSettings["SiteUrl"], userToken)))
            {
                using (SPWeb web = site.OpenWeb())
                {
                    try
                    {
                        // Get list of "old" file and delete them
                        RemoveOldFiles(web);

                        //check conversion type
                        DocType dtype = GetJobType(web, fileid);
                        switch (dtype)
                        {
                            case DocType.DOCX:

                                // check if the current file conversion is finished
                                ConversionJobStatus status = new ConversionJobStatus(ConfigurationManager.AppSettings["WASName"], Guid.Parse(fileid), null);
                                if (status.Succeeded == 1)
                                {
                                    // return finished document
                                    SPFile dest = GetFinishedFile(web, fileid);
                                    if (dest != null)
                                    {
                                        oasResponse.Content = Convert.ToBase64String(dest.OpenBinary());
                                        oasResponse.ErrorCode = OASErrorCodes.Success;

                                        // remove converted file
                                        RemoveConversionFiles(web, fileid);
                                    }
                                    else
                                    {
                                        oasResponse.ErrorCode = OASErrorCodes.ErrFileNotExists;
                                        oasResponse.Message = "Converted file not exists";
                                    }

                                }
                                else if (status.InProgress == 1 || status.NotStarted == 1)
                                {
                                    //file not converted yet
                                    oasResponse.ErrorCode = OASErrorCodes.Success;
                                }
                                else
                                {
                                    // something went wrong and file was not converted
                                    oasResponse.ErrorCode = OASErrorCodes.ErrFailedConvert;
                                    oasResponse.Message = "File conversion job error";

                                    RemoveConversionFiles(web, fileid);
                                }

                                break;

                            case DocType.PPTX:

                                // check if the current file conversion is finished

                                if (IsJobFinished(web, fileid))
                                {
                                    // return finished document
                                    SPFile dest = GetFinishedFile(web, fileid);
                                    if (dest != null)
                                    {
                                        oasResponse.Content = Convert.ToBase64String(dest.OpenBinary());
                                        oasResponse.ErrorCode = OASErrorCodes.Success;

                                        // remove converted file
                                        RemoveConversionFiles(web, fileid);
                                    }
                                    else
                                    {
                                        oasResponse.ErrorCode = OASErrorCodes.ErrFileNotExists;
                                        oasResponse.Message = "Converted file not exists";
                                    }

                                }

                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        oasResponse.ErrorCode = OASErrorCodes.ErrFailedConvert;
                        oasResponse.Message = ex.Message;
                    }
                }
            }

            return oasResponse;
        }

        static void PPTConversionFinished(IAsyncResult result)
        {
            // Get the state object associated with this request.
            PPTJob request = (PPTJob)result.AsyncState;
            try
            {
                //finish converrsion
                request.request.EndConvert(result);

                // Save result to the file
                SPFolder lib = GetOASLibrary(request.web);
                SPFile srcfile = lib.Files.Add(request.dest, request.output.ToArray(), true);
                srcfile.Update();

                // Set conversion record to finished state
                SetFileFinished(request.web, request.jobId);


            }
            catch (Exception ex)
            {
                // In case of any error need cleanup file records
                RemoveConversionFiles(request.web, request.jobId);
            }
        }


        private static SPFolder GetOASLibrary(SPWeb web)
        {
            SPListTemplateType genericList = new SPListTemplateType();
            genericList = SPListTemplateType.DocumentLibrary;

            SPFolder res = (SPFolder)web.GetFolder(OAS_LIBRARY);

            if (!res.Exists)
            {
                web.AllowUnsafeUpdates = true;
                Guid listGuid = web.Lists.Add(OAS_LIBRARY, "", genericList);
                SPDocumentLibrary oaslist = (SPDocumentLibrary)web.Lists[listGuid];
                oaslist.Hidden = true;

                oaslist.Update();
            }

            return (SPFolder)web.Folders[OAS_LIBRARY];
        }

        private static SPList GetOASList(SPWeb web)
        {
            SPListTemplateType genericList = new SPListTemplateType();

            genericList = SPListTemplateType.GenericList;

            //Check if the list exist
            SPList oaslist = web.Lists.TryGetList(OAS_LIST);

            if (oaslist == null)
            {
                //Create a custom list
                web.AllowUnsafeUpdates = true;
                Guid listGuid = web.Lists.Add(OAS_LIST, "", genericList);
                      oaslist = web.Lists[listGuid];
                oaslist.Hidden = true;

                //Add columns
                SPFieldCollection collFields = oaslist.Fields;

                string field1 = collFields.Add("FileId", SPFieldType.Text, false);
                SPField column1 = collFields.GetFieldByInternalName(field1);

                string field4 = collFields.Add("JobId", SPFieldType.Text, false);
                SPField column4 = collFields.GetFieldByInternalName(field4);

                string field2 = collFields.Add("Started", SPFieldType.DateTime, false);
                SPField column2 = collFields.GetFieldByInternalName(field2);

                string field3 = collFields.Add("Finished", SPFieldType.DateTime, false);
                SPField column3 = collFields.GetFieldByInternalName(field3);

                string field5 = collFields.Add("Type", SPFieldType.Integer, false);
                SPField column5 = collFields.GetFieldByInternalName(field5);

                SPView view = oaslist.DefaultView;

                SPViewFieldCollection collViewFields = view.ViewFields;

                collViewFields.Add(column1);
                collViewFields.Add(column2);
                collViewFields.Add(column3);
                collViewFields.Add(column4);
                collViewFields.Add(column5);

                view.Update();
            }

            return oaslist;
        }

        private void AddFileToList(SPWeb web, string JobId, string FileId, DocType type)
        {
            SPList list = GetOASList(web);
            SPListItem item = list.Items.Add();

            item["FileId"] = FileId;
            item["JobId"] = JobId;
            item["Started"] = DateTime.Now;
            item["Type"] = (int)type;

            item.Update();
        }

        private static SPFile GetFile(SPFolder folder, string fileid)
        {
            SPFile res = null;
            try
            {
                res = folder.Files[fileid];
            }
            catch (ArgumentException) { }

            return res;
        }
        private static void RemoveOldFiles(SPWeb web)
        {
            SPList list = GetOASList(web);
            

            DateTime limit_date = DateTime.Now.Subtract(new TimeSpan(TIME_LIMIT, 0, 0));

            web.AllowUnsafeUpdates = true;
            bool removed = false;
            do
            {
                removed = false;
                SPListItemCollection listItems = list.Items;
                int itemCount = listItems.Count;

                for (int k = 0; k < itemCount; k++)
                {
                    SPListItem item = listItems[k];

                    if ((DateTime)item["Started"] < limit_date)
                    {
                        //source file
                        string fileid = item["FileId"].ToString();
                        //conveted file
                        string source = Path.GetFileNameWithoutExtension(fileid);

                        SPFolder lib = GetOASLibrary(web);
                        //delete result file
                        SPFile res = GetFile(lib, fileid);
                        if (res != null)
                        {
                            res.Delete();
                            lib.Update();
                        }

                        //delete source file
                        res = GetFile(lib, source);
                        if (res != null)
                        {
                            res.Delete();
                            lib.Update();
                        }

                        listItems.Delete(k);
                        removed = true;
                        break;
                    }
                }
            }
            while (removed);
            web.AllowUnsafeUpdates = false;
        }

        private static void RemoveConversionFiles(SPWeb web, string JobId)
        {
            SPList list = GetOASList(web);
            SPListItemCollection listItems = list.Items;
            int itemCount = listItems.Count;

            web.AllowUnsafeUpdates = true;
            for (int k = 0; k < itemCount; k++)
            {
                SPListItem item = listItems[k];

                if (JobId == item["JobId"].ToString())
                {
                    //source file
                    string fileid = item["FileId"].ToString();
                    //conveted file
                    string source = Path.GetFileNameWithoutExtension(fileid);

                    SPFolder lib = GetOASLibrary(web);
                    //delete result file
                    SPFile res = GetFile(lib, fileid);
                    if (res != null)
                    {
                        res.Delete();
                        lib.Update();
                    }

                    //delete source file
                    res = GetFile(lib, source);
                    if (res != null)
                    {
                        res.Delete();
                        lib.Update();
                    }

                    listItems.Delete(k);
                    break;
                }
            }
            web.AllowUnsafeUpdates = false;
        }

        private static void SetFileFinished(SPWeb web, string JobId)
        {
            SPList list = GetOASList(web);
            SPListItemCollection listItems = list.Items;
            int itemCount = listItems.Count;

            for (int k = 0; k < itemCount; k++)
            {
                SPListItem item = listItems[k];

                if (JobId == item["JobId"].ToString())
                {
                    item["Finished"] = DateTime.Now;
                    item.Update();
                }
            }
        }

        private bool IsJobFinished(SPWeb web, string JobId)
        {
            SPList list = GetOASList(web);
            SPListItemCollection listItems = list.Items;
            int itemCount = listItems.Count;
            bool res = false;
            bool found = false;

            for (int k = 0; k < itemCount; k++)
            {
                SPListItem item = listItems[k];

                if (JobId == item["JobId"].ToString())
                {
                    if (item["Finished"] != null)
                    { 
                        res = true;
                    }
                    found = true;
                    break;
                }
            }

            // if there no job found with jobid we assume it was finished
            if (!found)
                res = true;

            return res;
        }

        private DocType GetJobType(SPWeb web, string JobId)
        {
            SPList list = GetOASList(web);
            SPListItemCollection listItems = list.Items;
            int itemCount = listItems.Count;
            DocType res = DocType.DOCX;

            for (int k = 0; k < itemCount; k++)
            {
                SPListItem item = listItems[k];

                if (JobId == item["JobId"].ToString())
                {
                    res = (DocType)item["Type"];
                    break;
                }
            }

            return res;
        }

        private SPFile GetFinishedFile(SPWeb web, string JobId)
        {
            SPFile res = null;
            SPList list = GetOASList(web);
            SPListItemCollection listItems = list.Items;
            int itemCount = listItems.Count;

            for (int k = 0; k < itemCount; k++)
            {
                SPListItem item = listItems[k];

                if (JobId == item["JobId"].ToString())
                {
                    string fileid = item["FileId"].ToString();
                    SPFolder lib = GetOASLibrary(web);
                    res = lib.Files[fileid];
                    break;
                }
            }

            return res;
        }

        private ConversionJobSettings FillWordConversionOptions(OASModels.ConversionOptions co)
        {
            ConversionJobSettings res = new ConversionJobSettings();
            res.FixedFormatSettings.BalloonState = (Microsoft.Office.Word.Server.Conversions.BalloonState)co.BalloonState;
            res.FixedFormatSettings.BitmapEmbeddedFonts = co.BitmapEmbeddedFonts;
            res.FixedFormatSettings.Bookmarks = (Microsoft.Office.Word.Server.Conversions.FixedFormatBookmark)co.Bookmarks;
            res.FixedFormatSettings.IncludeDocumentProperties = co.IncludeDocumentProperties;
            res.FixedFormatSettings.IncludeDocumentStructure = co.IncludeDocumentStructure;
            res.FixedFormatSettings.OutputQuality = (Microsoft.Office.Word.Server.Conversions.FixedFormatQuality)co.OutputQuality;
            res.FixedFormatSettings.UsePDFA = co.UsePDFA;

            return res;
        }

        private Microsoft.Office.Server.PowerPoint.Conversion.FixedFormatSettings FillPowerPointConversionOptions(OASModels.ConversionOptions co)
        {
            Microsoft.Office.Server.PowerPoint.Conversion.FixedFormatSettings res = new Microsoft.Office.Server.PowerPoint.Conversion.FixedFormatSettings(); 

            
            res.BitmapUnembeddableFonts = co.BitmapEmbeddedFonts;
            res.FrameSlides = co.FrameSlides;
            res.IncludeDocumentProperties = co.IncludeDocumentProperties;
            res.IncludeDocumentStructureTags = co.IncludeDocumentStructure;
            res.IncludeHiddenSlides = co.IncludeHiddenSlides;
            res.OptimizeForMinimumSize = (co.OutputQuality == OASModels.FixedFormatQuality.Minimum ? true : false);
            res.UsePdfA = co.UsePDFA;
            res.UseVerticalOrder = co.UseVerticalOrder;

            return res;
        }

        private SPUserToken GetUserToken(OASModels.ConversionSettings settings, ref OASModels.OASResponse oasResponse)
        {
            SPUserToken userToken = null;
            if (settings.Username != string.Empty && settings.Domain != string.Empty)
            {
                OASResponse resp = new OASResponse();

                // check for valid user credentials
                bool isValid = false;
                /*
                using (PrincipalContext pc = new PrincipalContext(ContextType.Domain, settings.Domain))
                {
                    // validate the credentials
                    isValid = pc.ValidateCredentials(settings.Username, settings.Password);
                }
                if (!isValid)
                {
                    oasResponse.ErrorCode = OASErrorCodes.ErrWrongCredentials;
                    oasResponse.Message = "Wrong credentials were passed.";
                }
                */

                IntPtr tokenHandler = IntPtr.Zero;
                isValid = LogonUser(settings.Username, settings.Domain, settings.Password, 2, 0, ref tokenHandler);
                if(!isValid)
                {
                    oasResponse.ErrorCode = OASErrorCodes.ErrWrongCredentials;
                    oasResponse.Message = "Wrong credentials were passed.";
                    return userToken;
                }

                WindowsImpersonationContext impersonationContext = WindowsIdentity.Impersonate(tokenHandler);
                
                SPSecurity.RunWithElevatedPrivileges(delegate ()
                {
                    try
                    {
                        SPSite site1 = new SPSite(ConfigurationManager.AppSettings["SiteUrl"]);
                        userToken = site1.RootWeb.AllUsers["i:0#.w|" + settings.Domain + "\\" + settings.Username].UserToken;
                    }
                    catch (Exception ex)
                    {
                        resp.ErrorCode = OASErrorCodes.ErrWrongCredentials;
                        resp.Message = ex.Message;
                    }
                });

                if (impersonationContext != null)
                {
                    impersonationContext.Undo();
                }

                if (tokenHandler != IntPtr.Zero)
                {
                    CloseHandle(tokenHandler);
                }

                oasResponse = resp;
            }
            else
            {
                //get current user for AppPool and try get his token
                string usename = System.Security.Principal.WindowsIdentity.GetCurrent().Name;

                OASResponse resp = new OASResponse();
                SPSecurity.RunWithElevatedPrivileges(delegate ()
                {
                    try
                    {
                        SPSite site1 = new SPSite(ConfigurationManager.AppSettings["SiteUrl"]);
                        userToken = site1.RootWeb.AllUsers["i:0#.w|" + usename].UserToken;
                    }
                    catch (Exception ex)
                    {
                        resp.ErrorCode = OASErrorCodes.ErrWrongCredentials;
                        resp.Message = ex.Message;
                    }
                });

                oasResponse = resp;
            }

            return userToken;
        }
    }
}
