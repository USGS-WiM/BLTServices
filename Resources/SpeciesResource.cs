//------------------------------------------------------------------------------
//-----  SpeciesResource -------------------------------------------------------
//------------------------------------------------------------------------------

//-------1---------2---------3---------4---------5---------6---------7---------8
//       01234567890123456789012345678901234567890123456789012345678901234567890
//-------+---------+---------+---------+---------+---------+---------+---------+

// copyright:   2013 WiM - USGS

//    authors:  Jeremy K. Newson USGS Wisconsin Internet Mapping
//              
//  
//   purpose:   Species resources.
//              Equivalent to the model in MVC.
//
//discussion:   Resources are plain-old CLR objects (POCO) the resources are POCO classes derived from the EF
//              PULAResource contains additional rederers of the derived EF POCO classes. 
//              https://github.com/openrasta/openrasta/wiki/Resources
//
//     

#region Comments
// 07.08.13 - jkn - Created
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Xml.Serialization;
using System.Runtime.Serialization;

namespace BLTServices.Resources
{
    [XmlRoot("ArrayOfSpecies")]
    public class SpeciesList
    {
        [DataMember]
        [XmlElement(typeof(SimpleSpecies),
        ElementName = "SPECIES")]
        public List<SimpleSpecies> SPECIES { get; set; }
    }//end PULAList

    public class SimpleSpecies : HypermediaEntity
    {
        [DataMember]
        [XmlElement(typeof(Int32),
            ElementName = "ENTITY_ID")]
        public Int32 ENTITY_ID { get; set; }

        [DataMember]
        [XmlElement(typeof(String),
            ElementName = "COMNAME")]
        public string COMNAME { get; set; }

        [DataMember]
        [XmlElement(typeof(String),
            ElementName = "SCINAME")]
        public string SCINAME { get; set; }

        [DataMember]
        [XmlElement(typeof(String),
            ElementName = "SPCODE")]
        public string SPCODE { get; set; }

    }//end class SimpleSpecies
}