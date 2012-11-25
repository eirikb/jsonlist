using System.Collections.Generic;

namespace Eirikb.SharePoint.JSONList
{
    public class List
    {
        public string Name;
        public List<string> Fields { get; set; }
        public List<List<object>> Items { get; set; }
    }
}