//------------------------------------------------------------------------------
//----- UserHandler ---------------------------------------------------------
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
    public class UserHandler:HandlerBase
    {
        #region Properties
        public override string entityName
        {
            get { return "USER_"; }
        }
        #endregion
        #region Routed Methods
        #region GetMethods
        // returns all USERS
        [WiMRequiresRole(new string[] { AdminRole, PublisherRole, CreatorRole })]
        [HttpOperation(HttpMethod.GET)]
        public OperationResult Get()
        {
            List<USER_> userList;
            try
            {
                using (EasySecureString securedPassword = GetSecuredPassword())
                {
                    using (BLTRDSEntities aBLTE = GetRDS(securedPassword))
                    {
                       userList  = GetEntities<USER_>(aBLTE).ToList();
                    }//end using
                }//end using
                activateLinks<USER_>(userList);

                return new OperationResult.OK { ResponseResource = userList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

        [WiMRequiresRole(new string[] { AdminRole, PublisherRole, CreatorRole, Public })]
        [HttpOperation(HttpMethod.GET)]
        public OperationResult Get(Int32 userID)
        {
            List<USER_> userList;
            try
            {
                if (userID <= 0)
                { return new OperationResult.BadRequest(); }
                using (EasySecureString securedPassword = GetSecuredPassword())
                {
                    using (BLTRDSEntities aBLTE = GetRDS(securedPassword))
                    {
                        userList = GetEntities<USER_>(aBLTE).Where(u => u.USER_ID== userID).ToList();
                    }//end using
                }//end using

                activateLinks<USER_>(userList);

                return new OperationResult.OK { ResponseResource = userList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

        [WiMRequiresRole(new string[] { AdminRole, PublisherRole })]
        [HttpOperation(HttpMethod.GET, ForUriName="GetVersionUsers")]
        public OperationResult GetVersionUsers(Int32 versionID)
        {
            List<USER_> userList;
            try
            {
                if (versionID <= 0)
                { return new OperationResult.BadRequest(); }
                using (EasySecureString securedPassword = GetSecuredPassword())
                {
                    using (BLTRDSEntities aBLTE = GetRDS(securedPassword))
                    {
                        userList = GetEntities<USER_>(aBLTE).Where(u => u.VERSIONs.Any(v => v.VERSION_ID == versionID) ||
                                                                                    u.VERSIONs_1.Any(v => v.VERSION_ID == versionID) ||
                                                                                    u.VERSIONs_2.Any(v => v.VERSION_ID == versionID)).ToList();
                    }//end using
                }//end using

                activateLinks<USER_>(userList);

                return new OperationResult.OK { ResponseResource = userList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

        [WiMRequiresRole(new string[] { AdminRole, PublisherRole, CreatorRole, Public })]
        [HttpOperation(HttpMethod.GET, ForUriName = "GetUserByUserName")]
        public OperationResult GetUserByUserName(string userName)
        {
            USER_ aUser; 
            try
            {
                if(string.IsNullOrEmpty(username))
                { return new OperationResult.BadRequest(); }
                using (EasySecureString securedPassword = GetSecuredPassword())
                {
                    using (BLTRDSEntities aBLTE = GetRDS(securedPassword))
                    {
                       aUser = GetEntities<USER_>(aBLTE).FirstOrDefault(u => string.Equals(u.USERNAME.ToUpper(), username.ToUpper()));
                    }//end using
                }//end using
                activateLinks<USER_>(aUser);

                return new OperationResult.OK { ResponseResource = aUser };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

        [WiMRequiresRole(new string[] { AdminRole, PublisherRole })]
        [HttpOperation(HttpMethod.GET, ForUriName = "ChangeUserPassword")]
        public OperationResult ChangeDataManagerPassword(string userName, string newPassword)
        {
            USER_ aUser;

            //Return BadRequest if missing required fields
            if ((string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(userName)))
            { return new OperationResult.BadRequest() { ResponseResource = "Invalid arguments" }; }

            if (!IsAuthorized(AdminRole) || Context.User.Identity.Name != username)
            { return new OperationResult.Forbidden(); }

            //Get basic authentication password
            using (EasySecureString securedPassword = GetSecuredPassword())
            {
                using (BLTRDSEntities aBLTRDS = GetRDS(securedPassword))
                {
                    //fetch the object to be updated (assuming that it exists)
                    aUser = aBLTRDS.USER_.SingleOrDefault(m => m.USERNAME == userName);

                    if (aUser == null)
                    { return new OperationResult.BadRequest() { ResponseResource = "no manager exists" }; }

                    // edit user profile using db stored procedure
                    // stored db throws errors internally but does not pass pass error
                    aBLTRDS.USER_PROFILE_EDITPASSWORD(aUser.USERNAME, newPassword);

                }//end using
            }//end using

            activateLinks<USER_>(aUser);

            return new OperationResult.OK { ResponseResource = aUser };

        }//end HttpMethod.Get
        #endregion
        
        #region POST Methods
        [RequiresRole(AdminRole)]
        [HttpOperation(HttpMethod.POST)]
        public OperationResult POST(USER_ anEntity)
        {
            try
            {
                //Return BadRequest if missing required fields
                if ((string.IsNullOrEmpty(anEntity.USERNAME) ||
                    anEntity.ROLE_ID <= 0))
                { return new OperationResult.BadRequest(); }

                //Get basic authenication password
                using (EasySecureString securedPassword = GetSecuredPassword())
                {
                    using (BLTRDSEntities aBLTE = GetRDS(securedPassword))
                    {
                        //Prior to running stored procedure, check if username exists
                        if (!Exists(aBLTE.USER_, ref anEntity))
                        {
                            //Check if USERNAME exists
                            if (aBLTE.USER_.FirstOrDefault(m => String.Equals(m.USERNAME.ToUpper().Trim(),
                                                                                        anEntity.USERNAME.ToUpper().Trim())) != null)
                                return new OperationResult.BadRequest { Description = "Username exists" };

                            // Create user profile using db stored procedure
                            // stored db throws errors internally but does not pass pass error
                           
                            aBLTE.USER_PROFILE_ADD(anEntity.USERNAME, buildDefaultPassword(anEntity));
                            aBLTE.USER_PROFILE_ADDROLE(anEntity.USERNAME, anEntity.ROLE_ID);
                            aBLTE.USER_.AddObject(anEntity);
                            aBLTE.SaveChanges();
                        }

                        activateLinks<USER_>(anEntity);

                    }//end using
                }//end using

                return new OperationResult.OK { ResponseResource = anEntity };
            }
            catch (Exception)
            { return new OperationResult.BadRequest(); }

        }//end HttpMethod.GET

        #endregion
        #region PUT Methods
        [RequiresRole(AdminRole)]
        [HttpOperation(HttpMethod.PUT, ForUriName = "Put")]
        public OperationResult Put(Int32 userID, USER_ aUser)
        {
             //Return BadRequest if missing required fields
            if ((string.IsNullOrEmpty(aUser.USERNAME) || aUser.ROLE_ID <= 0))
            { return new OperationResult.BadRequest();}
            
            //Get basic authentication password
            using (EasySecureString securedPassword = GetSecuredPassword())
            {
                using (BLTRDSEntities aBLTE = GetRDS(securedPassword))
                {
                    //fetch the object to be updated (assuming that it exists)
                    USER_ ObjectToBeUpdated = aBLTE.USER_.Single(m => m.USER_ID == userID);
                    
                    //FirstName
                    ObjectToBeUpdated.FNAME = (string.IsNullOrEmpty(aUser.FNAME) ?
                        ObjectToBeUpdated.FNAME : aUser.FNAME);
                    //Last Name
                    ObjectToBeUpdated.LNAME = (string.IsNullOrEmpty(aUser.LNAME) ?
                        ObjectToBeUpdated.LNAME : aUser.LNAME);
                    //OrganizationID
                    ObjectToBeUpdated.ORGANIZATION_ID = (Decimal.Equals(aUser.ORGANIZATION_ID, ObjectToBeUpdated.ORGANIZATION_ID) ?
                        ObjectToBeUpdated.ORGANIZATION_ID : aUser.ORGANIZATION_ID);
                    //DivisionID
                    ObjectToBeUpdated.DIVISION_ID = (Decimal.Equals(aUser.DIVISION_ID, ObjectToBeUpdated.DIVISION_ID) ?
                        ObjectToBeUpdated.DIVISION_ID : aUser.DIVISION_ID);
                    //Phone
                    ObjectToBeUpdated.PHONE = (string.IsNullOrEmpty(aUser.PHONE) ?
                        ObjectToBeUpdated.PHONE : aUser.PHONE);
                    //Email
                    ObjectToBeUpdated.EMAIL = (string.IsNullOrEmpty(aUser.EMAIL) ?
                        ObjectToBeUpdated.EMAIL : aUser.EMAIL);

                    aBLTE.SaveChanges();

                }//end using
            }//end using

            return new OperationResult.OK { ResponseResource = aUser };

        }//end HttpMethod.PUT
        #endregion

        #region DELETE Methods
        [RequiresRole(AdminRole)]
        [HttpOperation(HttpMethod.DELETE)]
        public OperationResult Delete(Int32 userID)
        {
            USER_ ObjectToBeDeleted = null;

            //Return BadRequest if missing required fields
            if (userID <= 0)
            {
                return new OperationResult.BadRequest();
            }


            //Get basic authentication password
            using (EasySecureString securedPassword = GetSecuredPassword())
            {
                using (BLTRDSEntities aBLTRDS = GetRDS(securedPassword))
                {

                    // create user profile using db stored proceedure
                    try
                    {
                        //fetch the object to be updated (assuming that it exists)
                        ObjectToBeDeleted = aBLTRDS.USER_.SingleOrDefault(
                                                m => m.USER_ID == userID);

                        //Try to remove user profile first
                        aBLTRDS.USER_PROFILE_REMOVE(ObjectToBeDeleted.USERNAME);

                        //delete it
                        aBLTRDS.USER_.DeleteObject(ObjectToBeDeleted);
                        aBLTRDS.SaveChanges();
                        //Return object to verify persisitance

                        return new OperationResult.OK { };

                    }
                    catch (Exception)
                    {
                        //TODO: relay failure type message 
                        // EX. if profile failed to be removed
                        return new OperationResult.BadRequest();
                    }

                }// end using
            } //end using
        }//end HTTP.DELETE
        #endregion
        #endregion

        #region Helper Methods
        private Boolean Exists(System.Data.Objects.IObjectSet<USER_> users, ref USER_ aUser)
        {
            USER_ existingDataManager;
            USER_ thisDataManager = aUser;
            //check if it exists
            try
            {
                existingDataManager = users.FirstOrDefault(m => m.USERNAME == thisDataManager.USERNAME);

                if (existingDataManager == null)
                    return false;

                //if exists then update ref contact
                aUser = existingDataManager;
                return true;

            }
            catch (Exception)
            {
                return false;
            }
        }

        private string buildDefaultPassword(USER_ dm)
        {
            //BLTDefau1t+numbercharInlastname+first2letterFirstName
            if (dm.USERNAME == "guest")
            {
                return "BLTDefau1t";
            }
            else
            {
                return "BLTDefau1t" + dm.LNAME.Count() + dm.FNAME.Substring(0, 2);
            }
        }//end buildDefaultPassword
        #endregion

        protected override void ExpireEntities(BLTRDSEntities aBLTE, decimal userId, decimal Id, DateTime dt)
        {
            throw new NotImplementedException();
        }
    }//end class UserHandler
}//end namespace