//------------------------------------------------------------------------------
//----- ActiveIngredientResource -----------------------------------------------------------
//------------------------------------------------------------------------------

//-------1---------2---------3---------4---------5---------6---------7---------8
//       01234567890123456789012345678901234567890123456789012345678901234567890
//-------+---------+---------+---------+---------+---------+---------+---------+

// copyright:   2013 WiM - USGS

//    authors:  Jon Baier USGS Wisconsin Internet Mapping
//              Jeremy K. Newson USGS Wisconsin Internet Mapping
//              Tonia Roddick USGS Wisconsin Internet Mapping
//  
//   purpose:   Active Ingredient resources.
//              Equivalent to the model in MVC.
//
//discussion:   Resources are plain-old CLR objects (POCO) the resources are POCO classes derived from the EF
//              SiteResource contains additional rederers of the derived EF POCO classes. 
//              https://github.com/openrasta/openrasta/wiki/Resources
//,
//     

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Xml.Serialization;

namespace BLTServices.Resources
{
    public class AIBase : HypermediaEntity
    {
        [XmlElement(DataType = "int",
        ElementName = "ACTIVE_INGREDIENT_ID")]
        public Int32 ACTIVE_INGREDIENT_ID { get; set; }

        [XmlElement(DataType = "string",
        ElementName = "INGREDIENT_NAME")]
        public String INGREDIENT_NAME { get; set; }
    }


    /* Light version of ActiveIngredient Object, only contains ID and ActiveIngredientName */
    public class SimpleAI : AIBase
    {
        public SimpleAI()
        {
            ACTIVE_INGREDIENT_ID = -1;
            INGREDIENT_NAME = "";
        }
    }


}