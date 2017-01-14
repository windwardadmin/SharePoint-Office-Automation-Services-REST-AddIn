using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

using OASModels;

namespace OASClient
{
    class OASClient
    {
        public string Url { get; set; }
        public string Domain { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public OASClient(string url, string domain, string username, string password)
        {
            Url = url;
            Domain = domain;
            Username = username;
            Password = password;
        }

        /*
         * Immediately convert file on a server
         * In case of success returned converted file as Stream
         * In case of any error exception will be throw
         */
        public async Task<Stream> Convert(DocType doctype, Stream file, ConversionOptions options)
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(Url);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            ConversionSettings settings = new ConversionSettings(Domain, Username, Password);
            settings.Options = options;

            // get file content
            using (MemoryStream ms = new MemoryStream())
            {
                file.CopyTo(ms);
                byte[] bytes = ms.ToArray();
                settings.Content = System.Convert.ToBase64String(bytes);
            }

            HttpResponseMessage response = await client.PostAsJsonAsync("api/convert/file/" + doctype.ToString(), settings);
            
            OASResponse resp = null;

            if (response.IsSuccessStatusCode)
            {
                resp = await response.Content.ReadAsAsync<OASResponse>();

                if (resp.ErrorCode == OASErrorCodes.Success)
                {
                    MemoryStream ms = new MemoryStream(System.Convert.FromBase64String(resp.Content));
                    return ms;
                }
                else
                {
                    OASConversionException ex = new OASConversionException(resp.Message);
                    ex.Source = resp.ErrorCode.ToString();
                    throw ex;
                }
                
            }
            else
            {
                OASWebServiceException ex = new OASWebServiceException(response.ReasonPhrase);
                ex.Source = response.StatusCode.ToString();
                throw ex;
            }
        }
        

        /*
         * Start the convertion job on a server
         * and return FileId of the processing file
         * In case of any error exception will be throw
         */
        public async Task<string> StartConversionJob(DocType doctype, Stream file, ConversionOptions options)
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(Url);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            ConversionSettings settings = new ConversionSettings(Domain, Username, Password);
            settings.Options = options;

            // get file content
            using (MemoryStream ms = new MemoryStream())
            {
                file.CopyTo(ms);
                byte[] bytes = ms.ToArray();
                settings.Content = System.Convert.ToBase64String(bytes);
            }

            HttpResponseMessage response = await client.PostAsJsonAsync("api/convert/job/" + doctype.ToString(), settings);

            OASResponse resp = null;

            if (response.IsSuccessStatusCode)
            {
                resp = await response.Content.ReadAsAsync<OASResponse>();

                if (resp.ErrorCode == OASErrorCodes.Success)
                {
                    return resp.FileId;
                }
                else
                {
                    OASConversionException ex = new OASConversionException(resp.Message);
                    ex.Source = resp.ErrorCode.ToString();
                    throw ex;
                }

            }
            else
            {
                OASWebServiceException ex = new OASWebServiceException(response.ReasonPhrase);
                ex.Source = response.StatusCode.ToString();
                throw ex;
            }
        }

        /*
         * Get converted file as result of convertion job
         * In case of not ready yet null value will be returned
         * In case of any error exception will the fired
         */
        public  async Task<Stream> GetConvertedFile(string FileId)
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(Url);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            ConversionSettings settings = new ConversionSettings(Domain, Username, Password);

            HttpResponseMessage response = await client.PostAsJsonAsync("api/convert/getfile/" + FileId, settings);

            OASResponse resp = null;

            if (response.IsSuccessStatusCode)
            {
                resp = await response.Content.ReadAsAsync<OASResponse>();

                if (resp.ErrorCode == OASErrorCodes.Success)
                {
                    MemoryStream ms = null;
                    if (resp.Content != null)
                        ms = new MemoryStream(System.Convert.FromBase64String(resp.Content));
                    return ms;
                }
                else
                {
                    OASConversionException ex = new OASConversionException(resp.Message);
                    ex.Source = resp.ErrorCode.ToString();
                    throw ex;
                }

            }
            else
            {
                OASWebServiceException ex = new OASWebServiceException(response.ReasonPhrase);
                ex.Source = response.StatusCode.ToString();
                throw ex;
            }
        }
    }
}
