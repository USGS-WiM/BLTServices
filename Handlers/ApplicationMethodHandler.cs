//------------------------------------------------------------------------------
//----- Application Method Handler -----------------------------------------------------------
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
// 07.22.13 - TR - Renamed from ModifiersHandler to ApplicationMethodHandler
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
    public class ApplicationMethodHandler: HandlerBase
    {

        #region Properties
        public override string entityName
        {
            get { return "APPLICATION_METHOD"; }
        }
        #endregion
        #region Routed Methods
        #region GetMethods
        //---------------------Returns List of objects---------------------
        // returns all APPLICATION_METHOD
       // [WiMRequiresRole(new string[] { AdminRole, PublisherRole, CreatorRole })]
        [HttpOperation(HttpMethod.GET)]
        public OperationResult Get()
        {
            List<APPLICATION_METHOD> anApplicationMethod;
            try
            {
                //using (EasySecureString securedPassword = GetSecuredPassword())
                //{
                    using (BLTRDSEntities aBLTE = GetRDS())
                    {
                        anApplicationMethod = GetEntities<APPLICATION_METHOD>(aBLTE).ToList();
                    }//end using
              //  }//end using
                activateLinks<APPLICATION_METHOD>(anApplicationMethod);

                return new OperationResult.OK { ResponseResource = anApplicationMethod };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

        // returns all PULAs
      //  [RequiresAuthentication]
        [HttpOperation(HttpMethod.GET, ForUriName = "GetVersionedApplicationMethods")]
        public OperationResult GetVersionedApplicationMethods(string status, string date)
        {
            ObjectQuery<APPLICATION_METHOD> appMethQuery;
            List<APPLICATION_METHOD> appMethods;
            try
            {
                StatusType statustype = getStatusType(status);
                DateTime? thisDate = ValidDate(date);
                if (!thisDate.HasValue)
                    thisDate = DateTime.Now.Date;

                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    appMethQuery = GetEntities<APPLICATION_METHOD>(aBLTE);
                    switch (statustype)
                    {
                        case (StatusType.Published):
                            appMethQuery.Where(ai => ai.VERSION.PUBLISHED_TIME_STAMP != null);
                            break;
                        case (StatusType.Reviewed):
                            appMethQuery.Where(ai => ai.VERSION.REVIEWED_TIME_STAMP != null &&
                                            ai.VERSION.PUBLISHED_TIME_STAMP == null);
                            break;
                        //created
                        default:
                            appMethQuery.Where(ai => ai.VERSION.REVIEWED_TIME_STAMP == null &&
                                            ai.VERSION.PUBLISHED_TIME_STAMP == null);
                            break;
                    }//end switch

                    appMethQuery.Where(ai => !ai.VERSION.EXPIRED_TIME_STAMP.HasValue ||
                                        ai.VERSION.EXPIRED_TIME_STAMP < thisDate.Value.Date);

                    appMethods = appMethQuery.ToList();

                }//end using
                

                activateLinks<APPLICATION_METHOD>(appMethods);

                return new OperationResult.OK { ResponseResource = appMethods };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

        // returns all active APPLICATION_METHOD
        [HttpOperation(HttpMethod.GET)]
        public OperationResult Get(string date)
        {
            List<APPLICATION_METHOD> anApplicationMethodList = null;
            try
            {
                DateTime? thisDate = ValidDate(date);
                if (!thisDate.HasValue)
                    thisDate = DateTime.Now.Date; 

                //set the date to current date if request is not authorized
                if (DateTime.Compare(thisDate.Value.Date, DateTime.Now.Date) < 0 && !CanManage()) thisDate = DateTime.Now.Date;
               
                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    anApplicationMethodList = GetActive(GetEntities<APPLICATION_METHOD>(aBLTE), thisDate.Value.Date).OrderBy(a=>a.METHOD).ToList();
                }//end using

                activateLinks<APPLICATION_METHOD>(anApplicationMethodList);

                return new OperationResult.OK { ResponseResource = anApplicationMethodList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }

        }//end HttpMethod.GET

  //      [RequiresAuthentication]
        [HttpOperation(HttpMethod.GET, ForUriName = "GetApplicationMethods")]
        public OperationResult GetApplicationMethods(Int32 applicationMethodID, [Optional] string date)
        {
            try
            {
                List<APPLICATION_METHOD> applicationMethodList;
                DateTime? thisDate = ValidDate(date);
                if (!thisDate.HasValue)
                    thisDate = DateTime.Now.Date;

                //set the date to current date if request is not authorized
                if (DateTime.Compare(thisDate.Value.Date, DateTime.Now.Date) < 0 && !CanManage()) thisDate = DateTime.Now.Date;

                if (applicationMethodID < 0)
                { return new OperationResult.BadRequest(); }

                
                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    IQueryable<APPLICATION_METHOD> query;
                    query = GetEntities<APPLICATION_METHOD>(aBLTE).Where(am => am.APPLICATION_METHOD_ID == applicationMethodID);

                    if (thisDate.HasValue)
                        query = GetActive(query, thisDate.Value.Date);

                    //process
                    applicationMethodList = query.ToList();
                }//end using
                

                activateLinks<APPLICATION_METHOD>(applicationMethodList);

                return new OperationResult.OK { ResponseResource = applicationMethodList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

        [HttpOperation(HttpMethod.GET, ForUriName = "GetPULALimitationsApplicationMethod")]
        public OperationResult GetPULALimitationsApplicationMethod(Int32 pulaLimitatationsID, [Optional]string date)
        {
            try
            {
                DateTime? thisDate = ValidDate(date);
                if (!thisDate.HasValue)
                    thisDate = DateTime.Now.Date;

                //set the date to current date if request is not authorized
                if (DateTime.Compare(thisDate.Value.Date, DateTime.Now.Date) < 0 && !CanManage()) thisDate = DateTime.Now.Date;

                List<APPLICATION_METHOD> applicationMethodList;
                using (BLTRDSEntities aBLTE = GetRDS())
                {

                    IQueryable<APPLICATION_METHOD> query1 =
                            (from PULALimit in GetActive(aBLTE.PULA_LIMITATIONS.Where(p => p.PULA_LIMITATION_ID == pulaLimitatationsID), thisDate.Value.Date)
                             join am in aBLTE.APPLICATION_METHOD
                             on PULALimit.APPLICATION_METHOD_ID equals am.APPLICATION_METHOD_ID
                             select am).Distinct();

                    applicationMethodList = GetActive(query1, thisDate.Value.Date).ToList();

                    activateLinks<APPLICATION_METHOD>(applicationMethodList);

                }//end using

                return new OperationResult.OK { ResponseResource = applicationMethodList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET
       
        //---------------------Returns individual objects---------------------
        //[WiMRequiresRole(new string[] { AdminRole, PublisherRole, CreatorRole })]
        [HttpOperation(HttpMethod.GET, ForUriName = "GetEntity")]
        public OperationResult GetEntity(Int32 entityID)
        {
            APPLICATION_METHOD anApplicationMethod;
            try
            {                
                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    anApplicationMethod = GetEntities<APPLICATION_METHOD>(aBLTE).SingleOrDefault(c => c.ID == entityID);

                }//end using                

                activateLinks<APPLICATION_METHOD>(anApplicationMethod);

                return new OperationResult.OK { ResponseResource = anApplicationMethod };
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
        public OperationResult POST(APPLICATION_METHOD anEntity)
        {
            try
            {
                using (EasySecureString securedPassword = GetSecuredPassword())
                {
                    using (BLTRDSEntities aBLTE = GetRDS(securedPassword))
                    {
                        if (!Exists(aBLTE, ref anEntity))
                        {
                            //create version
                            anEntity.VERSION_ID = SetVersion(aBLTE, anEntity.VERSION_ID, LoggedInUser(aBLTE).USER_ID, StatusType.Published, DateTime.Now.Date).VERSION_ID;

                            anEntity.APPLICATION_METHOD_ID = GetNextID(aBLTE);

                            aBLTE.APPLICATION_METHOD.AddObject(anEntity);

                            aBLTE.SaveChanges();
                        }
                        else
                        {//it exists, check if expired
                            if (anEntity.VERSION.EXPIRED_TIME_STAMP.HasValue)
                            {
                                APPLICATION_METHOD newAM = new APPLICATION_METHOD();
                                newAM.METHOD = anEntity.METHOD;
                                newAM.VERSION_ID = SetVersion(aBLTE, newAM.VERSION_ID, LoggedInUser(aBLTE).USER_ID, StatusType.Published, DateTime.Now.Date).VERSION_ID;
                                newAM.APPLICATION_METHOD_ID = anEntity.APPLICATION_METHOD_ID;
                                //anEntity.ID = 0;
                                aBLTE.APPLICATION_METHOD.AddObject(newAM);
                                aBLTE.SaveChanges();
                            }//end if
                        }
                        activateLinks<APPLICATION_METHOD>(anEntity);
                    }//end using
                }//end using

                return new OperationResult.OK { ResponseResource = anEntity };
            }
            catch (Exception)
            { return new OperationResult.BadRequest(); }

        }//end HttpMethod.GET

        #endregion

        #region PUT/EDIT Methods

        [WiMRequiresRole(new string[] { AdminRole, PublisherRole })]
        [HttpOperation(HttpMethod.PUT)]
        public OperationResult Put(Int32 entityID, APPLICATION_METHOD anEntity)
        {
            //No editing of tables are allowed. An edit call will create and activate a new entity, and expire the old one
            USER_ loggedInUser;
            APPLICATION_METHOD anApplicationMethod;
            try
            {
                if (entityID <= 0)
                { return new OperationResult.BadRequest(); }

                using (EasySecureString securedPassword = GetSecuredPassword())
                {
                    using (BLTRDSEntities aBLTE = GetRDS(securedPassword))
                    {
                        //version user
                        loggedInUser = LoggedInUser(aBLTE);

                        anApplicationMethod = aBLTE.APPLICATION_METHOD.FirstOrDefault(c => c.ID == entityID);
                        if (anApplicationMethod == null)
                        { return new OperationResult.BadRequest(); }

                        if (anApplicationMethod.VERSION.PUBLISHED_TIME_STAMP.HasValue)
                        {
                            //can not edit a published entity. Create new
                            //assign next pulaID
                            anEntity.APPLICATION_METHOD_ID = anApplicationMethod.APPLICATION_METHOD_ID;
                            //assign version
                            anEntity.VERSION_ID = SetVersion(aBLTE, anEntity.VERSION_ID, loggedInUser.USER_ID, StatusType.Published, DateTime.Now.Date).VERSION_ID;
                            aBLTE.APPLICATION_METHOD.AddObject(anEntity);
                            //expire originals
                            ExpireOtherEntities(aBLTE, LoggedInUser(aBLTE).USER_ID, anEntity, DateTime.Now.Date);
                            anApplicationMethod = anEntity;
                        }
                        else
                        {
                            anApplicationMethod.METHOD = anEntity.METHOD;
                        }//end if

                        aBLTE.SaveChanges();

                        activateLinks<APPLICATION_METHOD>(anApplicationMethod);
                    }//end using
                }//end using

                return new OperationResult.OK { ResponseResource = anApplicationMethod };
            }
            catch (Exception)
            { return new OperationResult.BadRequest(); }

        }//end HttpMethod.GET

        #endregion

        #region DELETE Methods
        [WiMRequiresRole(new string[] { AdminRole })]
        [HttpOperation(HttpMethod.DELETE)]
        public OperationResult Delete(Int32 entityID)
        {
            //Return BadRequest if missing required fields
            if (entityID <= 0)
                return new OperationResult.BadRequest();
            //Get basic authentication password
            using (EasySecureString securedPassword = GetSecuredPassword())
            {
                using (BLTRDSEntities aBLTE = GetRDS(securedPassword))
                {
                    APPLICATION_METHOD ObjectToBeDelete = aBLTE.APPLICATION_METHOD.FirstOrDefault(am => am.ID == entityID);

                    if (ObjectToBeDelete == null)
                    { return new OperationResult.BadRequest(); }

                    //NOTE: ShapeID can not be changed
                    if (ObjectToBeDelete.VERSION.PUBLISHED_TIME_STAMP.HasValue)
                    {
                        //set the date to be first of following month 
                        //int nextMo = DateTime.Now.Month + 1;
                        //DateTime nextMonth = Convert.ToDateTime(nextMo + "/01/" + DateTime.Now.Year);
                        ObjectToBeDelete.VERSION_ID = SetVersion(ObjectToBeDelete.VERSION_ID, LoggedInUser(aBLTE).USER_ID, StatusType.Expired, DateTime.Now.Date).VERSION_ID;
                    }
                    else
                    {
                        aBLTE.APPLICATION_METHOD.DeleteObject(ObjectToBeDelete);
                    }//end if

                    aBLTE.SaveChanges();
                }//end using
            }//end using

            //Return object to verify persisitance
            return new OperationResult.OK { };
        }//end HttpMethod.DELETE
        #endregion

        #endregion
        #region Helper Methods
        private bool Exists(BLTRDSEntities aBLTE, ref APPLICATION_METHOD anEntity)
        {
            APPLICATION_METHOD existingEntity;
            APPLICATION_METHOD thisEntity = anEntity;
            //check if it exists
            try
            {
                existingEntity = aBLTE.APPLICATION_METHOD.FirstOrDefault(mt => string.Equals(mt.METHOD.ToUpper(), thisEntity.METHOD.ToUpper()));
           

                if (existingEntity == null)
                    return false;

                //if exists then update ref contact
                anEntity = existingEntity;
                return true;

            }
            catch (Exception)
            {
                return false;
            }
        }//end Exists
        private decimal GetNextID(BLTRDSEntities aBLTE)
        {
            //create pulaID
            Decimal nextID = 1;
            if (aBLTE.APPLICATION_METHOD.Count() > 0)
                nextID = aBLTE.APPLICATION_METHOD.OrderByDescending(p => p.APPLICATION_METHOD_ID).First().APPLICATION_METHOD_ID + 1;

            return nextID;
        }
        private void ExpireOtherEntities(BLTRDSEntities aBLTE, decimal userId, APPLICATION_METHOD applicationMethod, DateTime dt)
        {
            //get all published, should only be 1
            List<APPLICATION_METHOD> aiList = aBLTE.APPLICATION_METHOD.Where(p => p.APPLICATION_METHOD_ID == applicationMethod.APPLICATION_METHOD_ID &&
                                                                                p.VERSION.PUBLISHED_TIME_STAMP <= dt.Date).ToList();
            if (aiList == null) return;

            foreach (var p in aiList)
            {
                if (!p.Equals(applicationMethod))
                    p.VERSION = SetVersion(aBLTE, p.VERSION_ID, userId, StatusType.Expired, dt.Date);
            }//next
        }//end ExpireOtherEntities
        protected override void ExpireEntities(BLTRDSEntities aBLTE, decimal userId, decimal Id, DateTime dt)
        {
            //get all published, should only be 1
            List<APPLICATION_METHOD> aiList = aBLTE.APPLICATION_METHOD.Where(p => p.APPLICATION_METHOD_ID == Id &&
                                                               p.VERSION.PUBLISHED_TIME_STAMP <= dt.Date).ToList();
            if (aiList == null) return;

            foreach (var p in aiList)
            {
                p.VERSION = SetVersion(aBLTE, p.VERSION_ID, userId, StatusType.Expired, dt.Date);
            }//next
        }
        #endregion
    }//end class ApplicationMethodHandler

}//end namespace