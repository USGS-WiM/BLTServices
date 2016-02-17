//------------------------------------------------------------------------------
//----- LimitationHandler ------------------------------------------------------
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
    public class LimitationHandler:HandlerBase
    {
        #region Properties
        public override string entityName
        {
            get { return "LIMITATIONS"; }
        }
        #endregion
        #region Routed Methods
        #region GetMethods
        //---------------------Returns List of objects---------------------
        // returns all CROP_USE
    //    [WiMRequiresRole(new string[] { AdminRole, PublisherRole, CreatorRole })]
        [HttpOperation(HttpMethod.GET)]
        public OperationResult Get()
        {
            List<LIMITATION> limitationList;
            try
            {
                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    limitationList = GetEntities<LIMITATION>(aBLTE).ToList();
                }//end using

                activateLinks<LIMITATION>(limitationList);

                return new OperationResult.OK { ResponseResource = limitationList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

        // returns all PULAs
        //[RequiresAuthentication]
        [HttpOperation(HttpMethod.GET, ForUriName = "GetVersionedLimitations")]
        public OperationResult GetVersionedLimitations(string status, string date)
        {
            ObjectQuery<LIMITATION> limitQuery;
            List<LIMITATION> limitation;
            try
            {
                StatusType statustype = getStatusType(status);
                DateTime? thisDate = ValidDate(date);
                if (!thisDate.HasValue)
                    thisDate = DateTime.Now.Date;

                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    limitQuery = GetEntities<LIMITATION>(aBLTE);
                    switch (statustype)
                    {
                        case (StatusType.Published):
                            limitQuery.Where(ai => ai.VERSION.PUBLISHED_TIME_STAMP != null);
                            break;
                        case (StatusType.Reviewed):
                            limitQuery.Where(ai => ai.VERSION.REVIEWED_TIME_STAMP != null &&
                                            ai.VERSION.PUBLISHED_TIME_STAMP == null);
                            break;
                        //created
                        default:
                            limitQuery.Where(ai => ai.VERSION.REVIEWED_TIME_STAMP == null &&
                                            ai.VERSION.PUBLISHED_TIME_STAMP == null);
                            break;
                    }//end switch

                    limitQuery.Where(ai => !ai.VERSION.EXPIRED_TIME_STAMP.HasValue ||
                                        ai.VERSION.EXPIRED_TIME_STAMP < thisDate.Value.Date);

                    limitation = limitQuery.ToList();

                }//end using

                activateLinks<LIMITATION>(limitation);

                return new OperationResult.OK { ResponseResource = limitation };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

        // returns all active CROP_USE
        [HttpOperation(HttpMethod.GET)]
        public OperationResult Get(string date)
        {
            List<LIMITATION> limitationList = null;
            try
            {
                DateTime? thisDate = ValidDate(date);
                if (!thisDate.HasValue)
                    thisDate = DateTime.Now;

                //set the date to current date if request is not authorized
                if (DateTime.Compare(thisDate.Value.Date, DateTime.Now.Date) < 0 && !CanManage()) thisDate = DateTime.Now.Date;

                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    limitationList = GetActive(GetEntities<LIMITATION>(aBLTE), thisDate.Value.Date).OrderBy(a=>a.CODE).ToList();
                }//end using
                

                activateLinks<LIMITATION>(limitationList);

                return new OperationResult.OK { ResponseResource = limitationList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }

        }//end HttpMethod.GET

    //    [RequiresAuthentication]
        [HttpOperation(HttpMethod.GET, ForUriName="GetLimitations")]
        public OperationResult Get(Int32 limitationID, [Optional] string date)
        {
            try
            {
                List<LIMITATION> limitationList;
                DateTime? thisDate = ValidDate(date);

                if (limitationID < 0)
                { return new OperationResult.BadRequest(); }

                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    IQueryable<LIMITATION> query;
                    query = GetEntities<LIMITATION>(aBLTE).Where(l => l.LIMITATION_ID == limitationID);

                    if (thisDate.HasValue)
                        query = GetActive(query, thisDate.Value.Date);

                    //process
                    limitationList = query.ToList();
                }//end using

                activateLinks<LIMITATION>(limitationList);

                return new OperationResult.OK { ResponseResource = limitationList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

        // returns active Active Ingredients for the given pulaID
        [HttpOperation(HttpMethod.GET, ForUriName = "GetPULALimitationsLimitation")]
        public OperationResult GetPULALimitationsLimitation(Int32 pulaLimitatationsID, [Optional]string date)
        {
            try
            {
                DateTime? thisDate = ValidDate(date);
                if (!thisDate.HasValue)
                    thisDate = DateTime.Now.Date;

                //set the date to current date if request is not authorized
                if (DateTime.Compare(thisDate.Value.Date, DateTime.Now.Date) < 0 && !CanManage()) thisDate = DateTime.Now.Date;

                List<LIMITATION> limitationsList;
                using (BLTRDSEntities aBLTE = GetRDS())
                {

                    IQueryable<LIMITATION> query1 =
                            (from PULALimit in GetActive(aBLTE.PULA_LIMITATIONS.Where(p => p.PULA_LIMITATION_ID == pulaLimitatationsID), thisDate.Value.Date)
                             join l in aBLTE.LIMITATIONS
                             on PULALimit.LIMITATION_ID equals l.LIMITATION_ID
                             select l).Distinct();

                    limitationsList = GetActive(query1, thisDate.Value).ToList();

                    activateLinks<LIMITATION>(limitationsList);

                }//end using

                return new OperationResult.OK { ResponseResource = limitationsList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

        //---------------------Returns individual objects---------------------
       // [WiMRequiresRole(new string[] { AdminRole, PublisherRole, CreatorRole })]
        [HttpOperation(HttpMethod.GET, ForUriName = "GetEntity")]
        public OperationResult GetEntity(Int32 entityID)
        {
            LIMITATION aLimitation;
            try
            {
                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    aLimitation = GetEntities<LIMITATION>(aBLTE).SingleOrDefault(l => l.ID == entityID);

                }//end using
                
                activateLinks<LIMITATION>(aLimitation);

                return new OperationResult.OK { ResponseResource = aLimitation };
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
        public OperationResult POST(LIMITATION anEntity)
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

                            anEntity.LIMITATION_ID = GetNextID(aBLTE);

                            aBLTE.LIMITATIONS.AddObject(anEntity);

                            aBLTE.SaveChanges();
                        }//end if

                        activateLinks<LIMITATION>(anEntity);
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
        public OperationResult Put(Int32 entityID, LIMITATION anEntity)
        {
            //No editing of tables are allowed. An edit call will create and activate a new entity, and expire the old one
            USER_ loggedInUser;
            LIMITATION aLimitation;
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

                        aLimitation = aBLTE.LIMITATIONS.FirstOrDefault(c => c.ID == entityID);
                        if (aLimitation == null)
                        { return new OperationResult.BadRequest(); }

                        if (aLimitation.VERSION.PUBLISHED_TIME_STAMP.HasValue)
                        {
                            //can not edit a published entity. Create new
                            //assign next pulaID
                            anEntity.LIMITATION_ID = aLimitation.LIMITATION_ID;
                            //assign version
                            anEntity.VERSION_ID = SetVersion(aBLTE, anEntity.VERSION_ID, loggedInUser.USER_ID, StatusType.Published, DateTime.Now.Date).VERSION_ID;
                            aBLTE.LIMITATIONS.AddObject(anEntity);
                            //expire originals
                            ExpireOtherEntities(aBLTE, LoggedInUser(aBLTE).USER_ID, anEntity, DateTime.Now.Date);
                            aLimitation = anEntity;
                        }
                        else
                        {
                            aLimitation.LIMITATION1 = anEntity.LIMITATION1;
                        }//end if

                        aBLTE.SaveChanges();

                        activateLinks<LIMITATION>(aLimitation);
                    }//end using
                }//end using

                return new OperationResult.OK { ResponseResource = aLimitation };
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

                    LIMITATION ObjectToBeDelete = aBLTE.LIMITATIONS.FirstOrDefault(l => l.ID == entityID);

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
                        aBLTE.LIMITATIONS.DeleteObject(ObjectToBeDelete);
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
        private bool Exists(BLTRDSEntities aBLTE, ref LIMITATION anEntity)
        {
            LIMITATION existingEntity;
            LIMITATION thisEntity = anEntity;
            //check if it exists
            try
            {
                existingEntity = aBLTE.LIMITATIONS.FirstOrDefault(mt => string.Equals(mt.CODE.ToUpper(), thisEntity.CODE.ToUpper()) &&
                                                                         string.Equals(mt.LIMITATION1.ToUpper(), thisEntity.LIMITATION1.ToUpper()));

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
            if (aBLTE.LIMITATIONS.Count() > 0)
                nextID = aBLTE.LIMITATIONS.OrderByDescending(p => p.LIMITATION_ID).First().LIMITATION_ID + 1;

            return nextID;
        }
        private void ExpireOtherEntities(BLTRDSEntities aBLTE, decimal userId, LIMITATION limitation, DateTime dt)
        {
            //get all published, should only be 1
            List<LIMITATION> limitList = aBLTE.LIMITATIONS.Where(p => p.LIMITATION_ID == limitation.LIMITATION_ID &&
                                                                                p.VERSION.PUBLISHED_TIME_STAMP <= dt.Date).ToList();
            if (limitList == null) return;

            foreach (var p in limitList)
            {
                if (!p.Equals(limitation))
                    p.VERSION = SetVersion(aBLTE, p.VERSION_ID, userId, StatusType.Expired, dt.Date);
            }//next
        }//end ExpireOtherEntities
        protected override void ExpireEntities(BLTRDSEntities aBLTE, decimal userId, decimal Id, DateTime dt)
        {           
            //get all published, should only be 1
            List<LIMITATION> aiList = aBLTE.LIMITATIONS.Where(p => p.LIMITATION_ID == Id &&
                                                              p.VERSION.PUBLISHED_TIME_STAMP <= dt.Date).ToList();
            if (aiList == null) return;

            foreach (var p in aiList)
            {
                p.VERSION = SetVersion(aBLTE, p.VERSION_ID, userId, StatusType.Expired, dt.Date);
            }//next
        }
        #endregion



    }//end LimitationHandler
}//end namespace