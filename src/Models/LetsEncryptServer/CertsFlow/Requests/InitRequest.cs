using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.LetsEncryptServer.CertsFlow.Requests
{
    public class InitRequest
    {
        public string[] Contacts { get; set; }
    }
}
