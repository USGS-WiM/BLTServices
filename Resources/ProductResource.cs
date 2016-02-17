//------------------------------------------------------------------------------
//-----  ProductResource ----------------------------------------------------------
//------------------------------------------------------------------------------

//-------1---------2---------3---------4---------5---------6---------7---------8
//       01234567890123456789012345678901234567890123456789012345678901234567890
//-------+---------+---------+---------+---------+---------+---------+---------+

// copyright:   2013 WiM - USGS

//    authors:  Jeremy K. Newson USGS Wisconsin Internet Mapping
//              Tonia Roddick USGS Wisconsin Internet Mapping
//  
//   purpose:   Product resources.
//              Equivalent to the model in MVC.
//
//discussion:   Resources are plain-old CLR objects (POCO) the resources are POCO classes derived from the EF
//              PULAResource contains additional rederers of the derived EF POCO classes. 
//              https://github.com/openrasta/openrasta/wiki/Resources
//
//     

#region Comments
// 05.20.13 - jkn - Created
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Xml.Serialization;

namespace BLTServices.Resources
{
    [XmlRoot("ArrayOfPRODUCT")]
    public class ProductList
    {
        [XmlElement(typeof(SimpleProduct),
        ElementName = "PROD")]
        public List<SimpleProduct> PROD { get; set; }
    }//end ProductList

    public class SimpleProduct : ProductBase
    {
        [XmlElement(typeof(String),
            ElementName = "PRODUCT_NAME")]
        public string PRODUCT_NAME { get; set; }

    }//end class SimpleProduct

    public class ProductBase : HypermediaEntity
    {
        [XmlElement(DataType = "int",
        ElementName = "ID")]
        public Int32 entityID { get; set; }

    }//end class ProductBase
}//end namespace