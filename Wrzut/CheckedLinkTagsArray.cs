using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Wrzut
{
    class CheckedLinksListCollection: List<object>
    {
        private Dictionary<string, string> dict = new Dictionary<string, string>();
        
        public void Add(object link)
        {
            UpdateDict((string)link);   
        }

        private void UpdateDict(string xmlLink)
        {
            if (dict.ContainsKey(xmlLink))
            {
                dict.Remove(xmlLink);
            }

            dict.Add(xmlLink, null);
        }

        public Dictionary<string, string> Dict
        {
            get { return dict; }
            set { dict = value; }
        }
    }
}
