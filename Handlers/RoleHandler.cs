//------------------------------------------------------------------------------
//----- RoleHandler ------------------------------------------------------------
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
    public class RoleHandler:HandlerBase
    {
        #region Properties
        public override string entityName
        {
            get { return "ROLE"; }
        }
        #endregion
        #region Routed Methods
        #region GetMethods

        // returns all VERSION
    //    [WiMRequiresRole(new string[] { AdminRole, PublisherRole, CreatorRole })]
        [HttpOperation(HttpMethod.GET)]
        public OperationResult Get()
        {
            List<ROLE> roleList;
            try
            {   
                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    roleList = GetEntities<ROLE>(aBLTE).ToList();
                }//end using
             
                activateLinks<ROLE>(roleList);

                return new OperationResult.OK { ResponseResource = roleList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

        // returns all active VERSION
      //  [WiMRequiresRole(new string[] { AdminRole, PublisherRole, CreatorRole })]
        [HttpOperation(HttpMethod.GET)]
        public OperationResult Get(Int32 roleID)
        {
            ROLE aRole = null;
            try
            {
                if (roleID <=0)
                { return new OperationResult.BadRequest {}; }
                
                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    aRole = GetEntities<ROLE>(aBLTE).FirstOrDefault(r => r.ROLE_ID == roleID);
                }//end using                

                activateLinks<ROLE>(aRole);

                return new OperationResult.OK { ResponseResource = aRole };
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
    }//end class RoleHandler
}//end namespace