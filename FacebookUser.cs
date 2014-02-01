using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace facebookXMPP
{
    public class FacebookUser
    {
        public string Jid { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }
        public string Gender { get; set; }
        public string Locale { get; set; }
        public bool IsTyping = false;
    }
}
