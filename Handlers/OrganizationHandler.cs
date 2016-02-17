//------------------------------------------------------------------------------
//----- OrganizationHandler ---------------------------------------------------------
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
// 05.15.13 - JKN - Created
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
    public class OrganizationHandler: HandlerBase
    {
        #region Properties
        public override string entityName
        {
            get { return "ORGANIZATION"; }
        }
        #endregion
        #region Routed Methods
        #region GetMethods
        // returns all USERS
       // [WiMRequiresRole(new string[] { AdminRole, PublisherRole, CreatorRole })]
        [HttpOperation(HttpMethod.GET)]
        public OperationResult Get()
        {
            List<ORGANIZATION> orgList;
            try
            {
                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    orgList  = GetEntities<ORGANIZATION>(aBLTE).OrderBy(a=>a.NAME).ToList();
                }//end using

                activateLinks<ORGANIZATION>(orgList);

                return new OperationResult.OK { ResponseResource = orgList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

       // [WiMRequiresRole(new string[] { AdminRole, PublisherRole, CreatorRole })]
        [HttpOperation(HttpMethod.GET)]
        public OperationResult Get(Int32 organizationID)
        {
            ORGANIZATION anOrg;
            try
            {
                if (organizationID <= 0)
                { return new OperationResult.BadRequest(); }
                
                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    anOrg = GetEntities<ORGANIZATION>(aBLTE).SingleOrDefault(o=>o.ORGANIZATION_ID==organizationID);
                }//end using
                
                activateLinks<ORGANIZATION>(anOrg);

                return new OperationResult.OK { ResponseResource = anOrg };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET
        #endregion

        #region POST Methods
        [WiMRequiresRole(new string[] { AdminRole, PublisherRole, CreatorRole })]
        [HttpOperation(HttpMethod.POST)]
        public OperationResult POST(ORGANIZATION anEntity)
        {
            try
            {
                //Return BadRequest if missing required fields
                if (string.IsNullOrEmpty(anEntity.NAME) )
                {return new OperationResult.BadRequest();}

                using (EasySecureString securedPassword = GetSecuredPassword())
                {
                    using (BLTRDSEntities aBLTE = GetRDS(securedPassword))
                    {
                        //Prior to running stored procedure, check if username exists
                        if (!Exists(aBLTE.ORGANIZATION, ref anEntity))
                        {
                           aBLTE.ORGANIZATION.AddObject(anEntity);
                            aBLTE.SaveChanges();
                        }

                        activateLinks<ORGANIZATION>(anEntity);

                    }//end using
                }//end using

                return new OperationResult.OK { ResponseResource = anEntity };
            }
            catch (Exception)
            { return new OperationResult.BadRequest(); }

        }//end HttpMethod.GET

        #endregion
        #region PUT Methods
        [WiMRequiresRole(new string[] { AdminRole, PublisherRole })]
        [HttpOperation(HttpMethod.PUT)]
        public OperationResult PUT(Int32 organizationID, ORGANIZATION instance)
        {
            try
            {
                if (organizationID <= 0)
                { return new OperationResult.BadRequest(); }

                 using (EasySecureString securedPassword = GetSecuredPassword())
                {
                    using (BLTRDSEntities aBLTE = GetRDS(securedPassword))
                    {
                        ORGANIZATION ObjectToBeUpdated = aBLTE.ORGANIZATION.SingleOrDefault(o=>o.ORGANIZATION_ID == organizationID);

                        if (ObjectToBeUpdated == null) { return new OperationResult.BadRequest(); }

                        //Name
                        ObjectToBeUpdated.NAME = (string.IsNullOrEmpty(instance.NAME) ?
                            ObjectToBeUpdated.NAME : instance.NAME);

                        aBLTE.SaveChanges();
                    }// end using
                }// end using

                activateLinks<ORGANIZATION>(instance);

                return new OperationResult.OK { ResponseResource = instance };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET
        #endregion
        //#region DELETE Methods
        //[RequiresRole(AdminRole)]
        //[HttpOperation(HttpMethod.DELETE)]
        //public OperationResult Delete(Int32 organizationID)
        //{
        //    ORGANIZATION ObjectToBeDeleted = null;

        //    //Return BadRequest if missing required fields
        //    if (organizationID <= 0)
        //    {return new OperationResult.BadRequest(); }


        //    //Get basic authentication password
        //    using (EasySecureString securedPassword = GetSecuredPassword())
        //    {
        //        using (BLTRDSEntities aBLTRDS = GetRDS(securedPassword))
        //        {
        //            try
        //            {
        //                //fetch the object to be updated (assuming that it exists)
        //                ObjectToBeDeleted = aBLTRDS.ORGANIZATION.SingleOrDefault(
        //                                        m => m.ORGANIZATION_ID == organizationID);

        //                //delete it
        //                aBLTRDS.ORGANIZATION.DeleteObject(ObjectToBeDeleted);
        //                aBLTRDS.SaveChanges();
        //                //Return object to verify persisitance

        //                return new OperationResult.OK { };

        //            }
        //            catch (Exception)
        //            {
        //                //TODO: relay failure type message 
        //                // EX. if profile failed to be removed
        //                return new OperationResult.BadRequest();
        //            }

        //        }// end using
        //    } //end using
        //}//end HTTP.DELETE
        //#endregion
        #endregion

        #region Helper Methods
        private Boolean Exists(System.Data.Objects.IObjectSet<ORGANIZATION> organizations, ref ORGANIZATION anOrganization)
        {
            ORGANIZATION existingOrg;
            ORGANIZATION thisOrg = anOrganization;
            //check if it exists
            try
            {
                existingOrg = organizations.FirstOrDefault(o => o.NAME == thisOrg.NAME);

                if (existingOrg == null)
                    return false;

                //if exists then update ref contact
                anOrganization = existingOrg;
                return true;

            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        protected override void ExpireEntities(BLTRDSEntities aBLTE, decimal userId, decimal Id, DateTime dt)
        {
            throw new NotImplementedException();
        }
    }//end class OrganizationHandler
}//end namespace