using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace smTest
{
    public struct Resume
    {
        public string Date;
        public string Type;
        public string Content;
        public string Ref;
    }

    public struct Product
    {
        public string UriName;
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