using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSD_Assignment___Banking_Application
{
    public class DataIntegrityException : Exception
    {
        public DataIntegrityException() { }

        public DataIntegrityException(string message) : base(message) { }

        public DataIntegrityException(string message, Exception innerException) : base(message, innerException) { }
    }
}
