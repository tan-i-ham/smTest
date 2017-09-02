using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace smTest
{
    public struct Resume
    {
        public string Date { get; set; }
        public string Type { get; set; }
        public string Content { get; set; }
        public string Ref { get; set; }
    }

    public struct Product
    {
        public string CompanyShort;
        public string Farmer;
        public string ProductName;
        public string Origin;
        public string PackedDate;
        public string VarifiedCompany;
    }

    public struct Recipe
    {
        public string dishName;
        public string dishPhoto;
        public string dishUrl;
    }
}