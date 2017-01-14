using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OASModels
{
    public class OASConversionException : Exception
    {
        public OASConversionException()
        {
        }

        public OASConversionException(string message)
            : base(message)
        {
        }

        public OASConversionException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    public class OASWebServiceException : Exception
    {
        public OASWebServiceException()
        {
        }

        public OASWebServiceException(string message)
            : base(message)
        {
        }

        public OASWebServiceException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
