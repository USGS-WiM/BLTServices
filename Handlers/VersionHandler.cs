//------------------------------------------------------------------------------
//----- VersionHandler ---------------------------------------------------------
//------------------------------------------------------------------------------

//-------1---------2---------3---------4---------5---------6---------7---------8
//       01234567890123456789012345678901234567890123456789012345678901234567890
//-------+---------+---------+---------+---------+---------+---------+---------+

// copyright:   2013 WiM - USGS

//    authors:  Jeremy K. Newson USGS Wisconsin Internet Mapping
//              
//  
//   purpose:   Handles Site resources through the HTTP uniform interface.
//              Equivalent to the controller in MVC.
//
//discussion:   Handlers are objects which handle all interaction with resources in 
//              this case the resources are POCO classes derived from the EF. 
//              https://github.com/openrasta/openrasta/wiki/Handlers
//
//     

#region Comments
// 05.14.13 - JKN - Created
#endregion

using BLTServices.Authentication;

using OpenRasta.Web;
using OpenRasta.Security;

using System;
using System.Data;
using System.Data.EntityClient;
using System.Data.Metadata.Edm;
using System.Data.Objects;
using System.Collections.Generic;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Runtime.InteropServices;

namespace BLTServices.Handlers
{
    public class VersionHandler: HandlerBase
    {
        #region Properties
        public override string entityName
        {
            get { return "VERSIONs"; }
        }
        #endregion
        #region Routed Methods
        #region GetMethods

        // returns all VERSION
      //  [WiMRequiresRole(new string[] { AdminRole, PublisherRole, CreatorRole })]
        [HttpOperation(HttpMethod.GET)]
        public OperationResult Get()
        {
            List<VERSION> versionList;
            try
            {
                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    versionList = GetEntities<VERSION>(aBLTE).ToList();
                }//end using
                
                activateLinks<VERSION>(versionList);

                return new OperationResult.OK { ResponseResource = versionList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

        // returns all active VERSION
       // [WiMRequiresRole(new string[] { AdminRole, PublisherRole, CreatorRole })]
        [HttpOperation(HttpMethod.GET)]
        public OperationResult Get(string date)
        {
            List<VERSION> versionList = null;
            try
            {
                DateTime? thisDate = ValidDate(date);
                if (!thisDate.HasValue)
                { return new OperationResult.BadRequest { ResponseResource = date + " is not a valid date" }; }

                //set the date to current date if request is not authorized
                if (DateTime.Compare(thisDate.Value,DateTime.Now) < 0 && !CanManage()) thisDate = DateTime.Now;
                
                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    versionList = GetActive(GetEntities<VERSION>(aBLTE), thisDate.Value).ToList();
                }//end using
                
                activateLinks<VERSION>(versionList);

                return new OperationResult.OK { ResponseResource = versionList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }

        }//end HttpMethod.GET

        // returns all active VERSION
      //  [WiMRequiresRole(new string[] { AdminRole, PublisherRole, CreatorRole })]
        [HttpOperation(HttpMethod.GET, ForUriName="GetActiveIngredientVersion")]
        public OperationResult GetActiveIngredientVersion(Int32 aiEntityID)
        {
            VERSION aVersion = null;
            try
            {
                if (aiEntityID <= 0)
                { return new OperationResult.BadRequest(); }
                                
                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    aVersion = GetEntities<ACTIVE_INGREDIENT>(aBLTE).FirstOrDefault(ai => ai.ID == aiEntityID).VERSION;

                }//end using
                
                activateLinks<VERSION>(aVersion);

                return new OperationResult.OK { ResponseResource = aVersion };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }

        }//end HttpMethod.GET

        // returns all active VERSION
     //   [WiMRequiresRole(new string[] { AdminRole, PublisherRole, CreatorRole })]
        [HttpOperation(HttpMethod.GET, ForUriName = "GetVersion")]
        public OperationResult GetVersion(Int32 entityID)
        {
            VERSION aVersion = null;
            try
            {
                if (entityID <= 0)
                { return new OperationResult.BadRequest(); }
                                
                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    aVersion = GetEntities<VERSION>(aBLTE).FirstOrDefault(v => v.VERSION_ID == entityID);

                }//end using

                activateLinks<VERSION>(aVersion);

                return new OperationResult.OK { ResponseResource = aVersion };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }

        }//end HttpMethod.GET

        #endregion
        #endregion
        #region Helper Methods
        
        #endregion

        protected override void ExpireEntities(BLTRDSEntities aBLTE, decimal userId, decimal Id, DateTime dt)
        {
            throw new NotImplementedException();
        }
    }//end VersionHandler
}//end namespace