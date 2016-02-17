//------------------------------------------------------------------------------
//-----  PULAResource ----------------------------------------------------------
//------------------------------------------------------------------------------

//-------1---------2---------3---------4---------5---------6---------7---------8
//       01234567890123456789012345678901234567890123456789012345678901234567890
//-------+---------+---------+---------+---------+---------+---------+---------+

// copyright:   2013 WiM - USGS

//    authors:  Jeremy K. Newson USGS Wisconsin Internet Mapping
//              
//  
//   purpose:   PULA resources.
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
    [XmlRoot("ArrayOfPULA")]
    public class PULAList
    {
        [XmlElement(typeof(SimplePULA),
        ElementName = "PULA")]
        public List<SimplePULA> PULA { get; set; }
    }//end PULAList

    public class SimplePULA:PULABase
    {
        [XmlElement(typeof(DateTime),
            ElementName = "CreatedDate")]
        public DateTime? Created { get; set; }
        public bool ShouldSerializeCreated()
        { return Created.HasValue; }

        [XmlElement(typeof(DateTime),
            ElementName = "PublishedDate")]
        public DateTime? Published { get; set; }
        public bool ShouldSerializePublished()
        { return Published.HasValue; }

        [XmlElement(typeof(DateTime),
            ElementName = "EffectiveDate")]
        public DateTime? Effective { get; set; }
        public bool ShouldSerializeEffective()
        { return Effective.HasValue; }

        [XmlElement(typeof(DateTime),
            ElementName = "ExpiredDate")]
        public DateTime? Expired { get; set; }
        public bool ShouldSerializeExpired()
        { return Expired.HasValue; }

    }//end class simplePULA

    public class PULABase//:HypermediaEntity
    {
        [XmlElement(DataType = "decimal",
        ElementName = "ID")]
        public decimal entityID { get; set; }

        [XmlElement(DataType = "decimal",
        ElementName = "ShapeID")]
        public decimal ShapeID { get; set; }

        [XmlElement(DataType = "decimal",
        ElementName = "isPublished")]
        public decimal isPublished { get; set; }

    }//end class simplePULA

    public class MapperLimitations
    {
        [XmlElement(typeof(MapperLimit),
        ElementName = "PULA")]
        public List<MapperLimit> MapperLimits { get; set; }
    }//end MapperLimitations

    public class MapperLimit
    {
        [XmlElement(typeof(Int32),
            ElementName = "PULAID")]
        public Int32 PULAID { get; set; }

        [XmlElement(typeof(Int32),
            ElementName = "PULASHPID")]
        public Int32 PULASHPID { get; set; }

        [XmlElement(typeof(String),
            ElementName = "NAME")]
        public string NAME { get; set; }

        [XmlElement(typeof(String),
            ElementName = "USE")]
        public string USE { get; set; }

        [XmlElement(typeof(String),
            ElementName = "APPMETHOD")]
        public string APPMETHOD { get; set; }

        [XmlElement(typeof(String),
            ElementName = "FORM")]
        public string FORM { get; set; }

        [XmlElement(typeof(LIMITATION),
            ElementName = "LIMIT")]
        public LIMITATION LIMIT { get; set; }

    }//end class simplePULA

}//end namespace