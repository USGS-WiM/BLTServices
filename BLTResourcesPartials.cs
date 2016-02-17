//------------------------------------------------------------------------------
//----- EntityObjectResource -----------------------------------------------------------
//------------------------------------------------------------------------------

//-------1---------2---------3---------4---------5---------6---------7---------8
//       01234567890123456789012345678901234567890123456789012345678901234567890
//-------+---------+---------+---------+---------+---------+---------+---------+

// copyright:   2012 WiM - USGS

//    authors:  Jeremy K. Newson USGS Wisconsin Internet Mapping
//              
//  
//   purpose:   EntityObject resources through the HTTP uniform interface.
//              Equivalent to the model in MVC.
//
//discussion:   Resources are plain-old CLR objects (POCO) the resources are POCO classes derived from the EF
//              SiteResource contains additional rederers of the derived EF POCO classes. 
//              https://github.com/openrasta/openrasta/wiki/Resources
//
//              EntityObjectResource contain the remaining entity object partial classes 
//              that where generated from the entity object model generator.
//              The partial classes were created in order to extend resource method to the 
//              generated objects
//
//              A partial class can implement more than one interface, 
//              but it cannot inherit from more than one base class. 
//              Therefore, all partial classes Inherits statements must specify the same base class.
//              http://msdn.microsoft.com/en-us/library/wa80x488.aspx
//              

#region Comments
// 08.02.12 - jkn - added method to add links to specified related objects
// 06.29.12 - jkn - added additional entities, changed base from EntityObject to ResourceBase to HypermediaEntity
// 06.28.12 - jkn - Created
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace BLTServices
{
    public partial class ACTIVE_INGREDIENT : HypermediaEntity
    {
        private String baseURI = "ActiveIngredients/{0}";
        #region Overrided Methods

        protected override string getRelativeURI(refType rType)
        {
            string uriString = "";
            switch (rType)
            {
                case refType.GET:
                    //Approvals/{ApprovalId}
                    uriString = string.Format(baseURI,this.ACTIVE_INGREDIENT_ID);
                    break;

                case refType.POST:
                    uriString = "Approvals";
                    break;
            }
            return uriString;
        }
        protected override void addRelatedLinks(string baseURI)
        {
            //ActiveIngredients/{entityID}/Version
            if (this.VERSION_ID > 0) this.LINKS.Add(GetLinkResource(baseURI, "Version", refType.GET, "/Version"));
            //ActiveIngredients/{entityID}/AIClass?ActiveDate={date}
            //if (this.AI_CLASS_ID > 0) this.LINKS.Add(GetLinkResource(baseURI, "AIClass", refType.GET, "/AIClass?ActiveDate="+DateTime.Now.Date));
            //ActiveIngredients/{entityID}/AILimitations
            this.LINKS.Add(GetLinkResource(baseURI, "AILimitations", refType.GET, "/AILimitations"));
        }

        #endregion

    }//end Class ACTIVE_INGREDIENT
    public partial class ACTIVE_INGREDIENT_PULA : HypermediaEntity
    {
        #region Methods
        public bool ShouldSerializeVERSION() { return false; }
        public bool ShouldSerializeEVENT() { return false; }

        //return a new aiPula
       public ACTIVE_INGREDIENT_PULA Clone()
        {
            ACTIVE_INGREDIENT_PULA newAIPULA = new ACTIVE_INGREDIENT_PULA();
            newAIPULA.PULA_ID = this.PULA_ID;
            newAIPULA.BASE_DATA = this.BASE_DATA;
            newAIPULA.BASE_DATA_MODIFIERS = this.BASE_DATA_MODIFIERS;
            newAIPULA.ADDITIONAL_INFORMATION = this.ADDITIONAL_INFORMATION;
            newAIPULA.IS_PUBLISHED = this.IS_PUBLISHED;
            newAIPULA.VERSION_ID = this.VERSION_ID;

            return newAIPULA;
        
        }//end Clone

        #endregion
        #region Overrided Methods

        protected override string getRelativeURI(refType rType)
        {
            string uriString = "";
            switch (rType)
            {
                case refType.GET:
                case refType.PUT:
                case refType.DELETE:
                    //Approvals/{ApprovalId}
                    uriString = string.Format("Approvals/{0}", this.PULA_ID);
                    break;

                case refType.POST:
                    uriString = "Approvals";
                    break;
            }
            return uriString;
        }
        protected override void addRelatedLinks(string baseURI)
        {
            //Approvals/{ApprovalId}/ApprovingOfficial
            if (this.VERSION_ID > 0) this.LINKS.Add(GetLinkResource(baseURI, "Approving Official", refType.GET, "/ApprovingOfficial"));
            //Approvals/{ApprovalId}/HWMs
            this.LINKS.Add(GetLinkResource(baseURI, "HWMs", refType.GET, "/HWMs"));
            //Approvals/{ApprovalId}/DataFiles
            this.LINKS.Add(GetLinkResource(baseURI, "Files", refType.GET, "/DataFiles"));
        }

        #endregion

    }//end Class ACTIVE_INGREDIENT_PULA
    public partial class AI_CLASS : HypermediaEntity
    {
        #region Overrided Methods

        protected override string getRelativeURI(refType rType)
        {
            string uriString = "";
            switch (rType)
            {
                case refType.GET:
                case refType.PUT:
                case refType.DELETE:
                    //Approvals/{ApprovalId}
                    uriString = string.Format("Approvals/{0}", this.AI_CLASS_ID);
                    break;

                case refType.POST:
                    uriString = "Approvals";
                    break;
            }
            return uriString;
        }
        protected override void addRelatedLinks(string baseURI)
        {
            //Approvals/{ApprovalId}/ApprovingOfficial
            if (this.VERSION_ID > 0) this.LINKS.Add(GetLinkResource(baseURI, "Approving Official", refType.GET, "/ApprovingOfficial"));
            //Approvals/{ApprovalId}/HWMs
            this.LINKS.Add(GetLinkResource(baseURI, "HWMs", refType.GET, "/HWMs"));
            //Approvals/{ApprovalId}/DataFiles
            this.LINKS.Add(GetLinkResource(baseURI, "Files", refType.GET, "/DataFiles"));
        }

        #endregion

    }//end Class AI_CLASS
    public partial class AI_LIMITATIONS : HypermediaEntity
    {
        #region Overrided Methods

        protected override string getRelativeURI(refType rType)
        {
            string uriString = "";
            switch (rType)
            {
                case refType.GET:
                case refType.PUT:
                case refType.DELETE:
                    //Approvals/{ApprovalId}
                    uriString = string.Format("Approvals/{0}", "");
                    break;

                case refType.POST:
                    uriString = "Approvals";
                    break;
            }
            return uriString;
        }
        protected override void addRelatedLinks(string baseURI)
        {
            //Approvals/{ApprovalId}/HWMs
            this.LINKS.Add(GetLinkResource(baseURI, "HWMs", refType.GET, "/HWMs"));
            //Approvals/{ApprovalId}/DataFiles
            this.LINKS.Add(GetLinkResource(baseURI, "Files", refType.GET, "/DataFiles"));
        }

        #endregion

    }//end Class AI_LIMITATIONS
    public partial class CROP_USE : HypermediaEntity
    {
        #region Overrided Methods

        protected override string getRelativeURI(refType rType)
        {
            string uriString = "";
            switch (rType)
            {
                case refType.GET:
                case refType.PUT:
                case refType.DELETE:
                    //Approvals/{ApprovalId}
                    uriString = string.Format("Approvals/{0}", this.CROP_USE_ID);
                    break;

                case refType.POST:
                    uriString = "Approvals";
                    break;
            }
            return uriString;
        }
        protected override void addRelatedLinks(string baseURI)
        {
            //Approvals/{ApprovalId}/ApprovingOfficial
            if (this.VERSION_ID > 0) this.LINKS.Add(GetLinkResource(baseURI, "Approving Official", refType.GET, "/ApprovingOfficial"));
            //Approvals/{ApprovalId}/HWMs
            this.LINKS.Add(GetLinkResource(baseURI, "HWMs", refType.GET, "/HWMs"));
            //Approvals/{ApprovalId}/DataFiles
            this.LINKS.Add(GetLinkResource(baseURI, "Files", refType.GET, "/DataFiles"));
        }

        #endregion

    }//end Class CROP_USE
    public partial class DIVISION : HypermediaEntity
    {
        #region Overrided Methods

        protected override string getRelativeURI(refType rType)
        {
            string uriString = "";
            switch (rType)
            {
                case refType.GET:
                case refType.PUT:
                case refType.DELETE:
                    //Approvals/{ApprovalId}
                    uriString = string.Format("Approvals/{0}", this.DIVISION_ID);
                    break;

                case refType.POST:
                    uriString = "Approvals";
                    break;
            }
            return uriString;
        }
        protected override void addRelatedLinks(string baseURI)
        {
            //Approvals/{ApprovalId}/ApprovingOfficial
           // if (this.VERSION_ID > 0) this.LINKS.Add(GetLinkResource(baseURI, "Approving Official", refType.GET, "/ApprovingOfficial"));
            //Approvals/{ApprovalId}/HWMs
            this.LINKS.Add(GetLinkResource(baseURI, "HWMs", refType.GET, "/HWMs"));
            //Approvals/{ApprovalId}/DataFiles
            this.LINKS.Add(GetLinkResource(baseURI, "Files", refType.GET, "/DataFiles"));
        }

        #endregion

    }//end Class DIVISIONS
    public partial class LIMITATION : HypermediaEntity
    {
        #region Overrided Methods

        protected override string getRelativeURI(refType rType)
        {
            string uriString = "";
            switch (rType)
            {
                case refType.GET:
                case refType.PUT:
                case refType.DELETE:
                    //Approvals/{ApprovalId}
                    uriString = string.Format("Approvals/{0}", this.LIMITATION_ID);
                    break;

                case refType.POST:
                    uriString = "Approvals";
                    break;
            }
            return uriString;
        }
        protected override void addRelatedLinks(string baseURI)
        {
            //Approvals/{ApprovalId}/ApprovingOfficial
            if (this.VERSION_ID > 0) this.LINKS.Add(GetLinkResource(baseURI, "Approving Official", refType.GET, "/ApprovingOfficial"));
            //Approvals/{ApprovalId}/HWMs
            this.LINKS.Add(GetLinkResource(baseURI, "HWMs", refType.GET, "/HWMs"));
            //Approvals/{ApprovalId}/DataFiles
            this.LINKS.Add(GetLinkResource(baseURI, "Files", refType.GET, "/DataFiles"));
        }

        #endregion

    }//end Class LIMITATIONS
    public partial class MODIFIER : HypermediaEntity
    {
        #region Overrided Methods

        protected override string getRelativeURI(refType rType)
        {
            string uriString = "";
            switch (rType)
            {
                case refType.GET:
                case refType.PUT:
                case refType.DELETE:
                    //Approvals/{ApprovalId}
                    //uriString = string.Format("Approvals/{0}", this.MODIFIER_ID);
                    break;

                case refType.POST:
                    uriString = "Approvals";
                    break;
            }
            return uriString;
        }
        protected override void addRelatedLinks(string baseURI)
        {
            //Approvals/{ApprovalId}/ApprovingOfficial
            //if (this.VERSION_ID > 0) this.LINKS.Add(GetLinkResource(baseURI, "Approving Official", refType.GET, "/ApprovingOfficial"));
            //Approvals/{ApprovalId}/HWMs
            this.LINKS.Add(GetLinkResource(baseURI, "HWMs", refType.GET, "/HWMs"));
            //Approvals/{ApprovalId}/DataFiles
            this.LINKS.Add(GetLinkResource(baseURI, "Files", refType.GET, "/DataFiles"));
        }

        #endregion

    }//end Class MODIFIER
    public partial class USER_ : HypermediaEntity
    {
        #region Overrided Methods

        protected override string getRelativeURI(refType rType)
        {
            string uriString = "";
            switch (rType)
            {
                case refType.GET:
                case refType.PUT:
                case refType.DELETE:
                    //Approvals/{ApprovalId}
                    uriString = string.Format("Approvals/{0}", this.USER_ID);
                    break;

                case refType.POST:
                    uriString = "Approvals";
                    break;
            }
            return uriString;
        }
        protected override void addRelatedLinks(string baseURI)
        {
            //Approvals/{ApprovalId}/ApprovingOfficial
          //  if (this.VERSION_ID > 0) this.LINKS.Add(GetLinkResource(baseURI, "Approving Official", refType.GET, "/ApprovingOfficial"));
            //Approvals/{ApprovalId}/HWMs
            this.LINKS.Add(GetLinkResource(baseURI, "HWMs", refType.GET, "/HWMs"));
            //Approvals/{ApprovalId}/DataFiles
            this.LINKS.Add(GetLinkResource(baseURI, "Files", refType.GET, "/DataFiles"));
        }

        #endregion

    }//end Class USER_
    public partial class VERSION : HypermediaEntity
    {
        #region Overrided Methods

        protected override string getRelativeURI(refType rType)
        {
            string uriString = "";
            switch (rType)
            {
                case refType.GET:
                case refType.PUT:
                case refType.DELETE:
                    //Approvals/{ApprovalId}
                    uriString = string.Format("Approvals/{0}", this.VERSION_ID);
                    break;

                case refType.POST:
                    uriString = "Approvals";
                    break;
            }
            return uriString;
        }
        protected override void addRelatedLinks(string baseURI)
        {
            //Approvals/{ApprovalId}/ApprovingOfficial
            if (this.VERSION_ID > 0) this.LINKS.Add(GetLinkResource(baseURI, "Approving Official", refType.GET, "/ApprovingOfficial"));
            //Approvals/{ApprovalId}/HWMs
            this.LINKS.Add(GetLinkResource(baseURI, "HWMs", refType.GET, "/HWMs"));
            //Approvals/{ApprovalId}/DataFiles
            this.LINKS.Add(GetLinkResource(baseURI, "Files", refType.GET, "/DataFiles"));
        }

        #endregion

    }//end Class DIVISIONS
}//end namespace