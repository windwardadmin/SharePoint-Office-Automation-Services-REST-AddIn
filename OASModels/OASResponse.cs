using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OASModels
{
    public class OASResponse
    {
        public OASErrorCodes ErrorCode { get; set; }
        public string FileId { get; set; }
        public string Message { get; set; }
        public string Source { get; set; }
        public string Destination { get; set; }
        public string Content { get; set; }
    }

    public enum OASErrorCodes
    {
        Success = 0,
        ErrWrongCredentials = 1,
        ErrFailedConvert = 2,
        ErrUnknown = 3,
        ErrFileNotExists = 4
    };
}