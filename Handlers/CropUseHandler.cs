//------------------------------------------------------------------------------
//----- CropUseHandler ---------------------------------------------------------
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
    public class CropUseHandler:HandlerBase
    {
        #region Properties
        public override string entityName
        {
            get { return "CROP_USE"; }
        }
        #endregion
        #region Routed Methods
        #region GetMethods

        //---------------------Returns List of objects---------------------
        // returns all CROP_USE
       // [WiMRequiresRole(new string[] { AdminRole, PublisherRole, CreatorRole })]
        [HttpOperation(HttpMethod.GET)]
        public OperationResult Get()
        {
            List<CROP_USE> cropUseList;
            try
            {               
                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    cropUseList = GetEntities<CROP_USE>(aBLTE).ToList();
                }//end using
                
                activateLinks<CROP_USE>(cropUseList);

                return new OperationResult.OK { ResponseResource = cropUseList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

        // returns versioned CropUses
       // [RequiresAuthentication]
        [HttpOperation(HttpMethod.GET, ForUriName = "GetVersionedCropUses")]
        public OperationResult GetVersionedCropUses(string status, string date)
        {
            ObjectQuery<CROP_USE> cropUseQuery;
            List<CROP_USE> cropUses;
            try
            {
                StatusType statustype = getStatusType(status);
                DateTime? thisDate = ValidDate(date);
                if (!thisDate.HasValue)
                    thisDate = DateTime.Now.Date;

                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    cropUseQuery = GetEntities<CROP_USE>(aBLTE);
                    switch (statustype)
                    {
                        case (StatusType.Published):
                            cropUseQuery.Where(ai => ai.VERSION.PUBLISHED_TIME_STAMP != null);
                            break;
                        case (StatusType.Reviewed):
                            cropUseQuery.Where(ai => ai.VERSION.REVIEWED_TIME_STAMP != null &&
                                            ai.VERSION.PUBLISHED_TIME_STAMP == null);
                            break;
                        //created
                        default:
                            cropUseQuery.Where(ai => ai.VERSION.REVIEWED_TIME_STAMP == null &&
                                            ai.VERSION.PUBLISHED_TIME_STAMP == null);
                            break;
                    }//end switch

                    cropUseQuery.Where(ai => !ai.VERSION.EXPIRED_TIME_STAMP.HasValue ||
                                        ai.VERSION.EXPIRED_TIME_STAMP < thisDate.Value.Date);

                    cropUses = cropUseQuery.ToList();

                }//end using
               

                activateLinks<CROP_USE>(cropUses);

                return new OperationResult.OK { ResponseResource = cropUses };
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
            List<CROP_USE> aCropUseList = null;
            try
            {
                DateTime? thisDate = ValidDate(date);
                if (!thisDate.HasValue)
                    thisDate = DateTime.Now.Date; 

                //set the date to current date if request is not authorized
                if (DateTime.Compare(thisDate.Value.Date, DateTime.Now.Date) < 0 && !CanManage()) thisDate = DateTime.Now.Date;
                
                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    aCropUseList = GetActive(GetEntities<CROP_USE>(aBLTE), thisDate.Value.Date).OrderBy(a=>a.USE).ToList();
                   
                }//end using

                activateLinks<CROP_USE>(aCropUseList);

                return new OperationResult.OK { ResponseResource = aCropUseList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }

        }//end HttpMethod.GET

    //    [RequiresAuthentication]
        [HttpOperation(HttpMethod.GET, ForUriName="GetCropUses")]
        public OperationResult Get(Int32 cropUseID, [Optional] string date)
        {
            try
            {
                List<CROP_USE> cropUseList;
                DateTime? thisDate = ValidDate(date);
                if (!thisDate.HasValue)
                    thisDate = DateTime.Now.Date;

                //set the date to current date if request is not authorized
                if (DateTime.Compare(thisDate.Value.Date, DateTime.Now.Date) < 0 && !CanManage()) thisDate = DateTime.Now.Date;

                if (cropUseID < 0)
                { return new OperationResult.BadRequest(); }

                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    IQueryable<CROP_USE> query;
                    query = GetEntities<CROP_USE>(aBLTE).Where(ai => ai.CROP_USE_ID == cropUseID);

                    if (thisDate.HasValue)
                        query = GetActive(query, thisDate.Value.Date);

                    //process
                    cropUseList = query.ToList();
                }//end using               

                activateLinks<CROP_USE>(cropUseList);

                return new OperationResult.OK { ResponseResource = cropUseList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

        // returns active Active Ingredients for the given pulaID
        [HttpOperation(HttpMethod.GET, ForUriName = "GetPULALimitationsCropUse")]
        public OperationResult GetPULALimitationsCropUse(Int32 pulaLimitatationsID, [Optional]string date)
        {
            try
            {
                DateTime? thisDate = ValidDate(date);
                if (!thisDate.HasValue)
                    thisDate = DateTime.Now.Date;

                //set the date to current date if request is not authorized
                if (DateTime.Compare(thisDate.Value.Date, DateTime.Now.Date) < 0 && !CanManage()) thisDate = DateTime.Now.Date;

                List<CROP_USE> cropUseList;
                using (BLTRDSEntities aBLTE = GetRDS())
                {

                    IQueryable<CROP_USE> query1 =
                            (from PULALimit in GetActive(aBLTE.PULA_LIMITATIONS.Where(p => p.PULA_LIMITATION_ID == pulaLimitatationsID), thisDate.Value.Date)
                             join cu in aBLTE.CROP_USE
                             on PULALimit.CROP_USE_ID equals cu.CROP_USE_ID
                             select cu).Distinct();

                    cropUseList = GetActive(query1, thisDate.Value.Date).ToList();

                    activateLinks<CROP_USE>(cropUseList);

                }//end using

                return new OperationResult.OK { ResponseResource = cropUseList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

        //---------------------Returns individual objects---------------------
      //  [WiMRequiresRole(new string[] { AdminRole, PublisherRole, CreatorRole })]
        [HttpOperation(HttpMethod.GET, ForUriName="GetEntity")]
        public OperationResult GetEntity(Int32 entityID)
        {
            CROP_USE anCropUse;
            try
            {
               using (BLTRDSEntities aBLTE = GetRDS())
                {
                    anCropUse = GetEntities<CROP_USE>(aBLTE).SingleOrDefault(c => c.ID == entityID);

                }//end using
                

                activateLinks<CROP_USE>(anCropUse);

                return new OperationResult.OK { ResponseResource = anCropUse };
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
        public OperationResult POST(CROP_USE anEntity)
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
                           
                            anEntity.CROP_USE_ID = GetNextID(aBLTE);

                            aBLTE.CROP_USE.AddObject(anEntity);

                            aBLTE.SaveChanges();
                        }
                        else
                        {//it exists, check if expired
                            if (anEntity.VERSION.EXPIRED_TIME_STAMP.HasValue)
                            {
                                CROP_USE newCU = new CROP_USE();
                                newCU.USE = anEntity.USE;
                                newCU.VERSION_ID = SetVersion(aBLTE, newCU.VERSION_ID, LoggedInUser(aBLTE).USER_ID, StatusType.Published, DateTime.Now.Date).VERSION_ID;
                                newCU.CROP_USE_ID = anEntity.CROP_USE_ID;
                                //anEntity.ID = 0;
                                aBLTE.CROP_USE.AddObject(newCU);
                                aBLTE.SaveChanges();
                            }//end if
                        }//end if

                        activateLinks<CROP_USE>(anEntity); 
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
        public OperationResult Put(Int32 entityID, CROP_USE anEntity)
        {
            //No editing of tables are allowed. An edit call will create and activate a new entity, and expire the old one
            USER_ loggedInUser;
            CROP_USE aCropUse;
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

                        aCropUse = aBLTE.CROP_USE.FirstOrDefault(c => c.ID == entityID);

                        if (aCropUse == null)
                        { return new OperationResult.BadRequest(); }

                        if (aCropUse.VERSION.PUBLISHED_TIME_STAMP.HasValue)
                        {
                            //can not edit a published entity. Create new
                            //assign next pulaID
                            anEntity.CROP_USE_ID = aCropUse.CROP_USE_ID;
                            //assign version
                            anEntity.VERSION_ID = SetVersion(aBLTE, anEntity.VERSION_ID, loggedInUser.USER_ID, StatusType.Published, DateTime.Now.Date).VERSION_ID;
                            aBLTE.CROP_USE.AddObject(anEntity);
                            //expire originals
                            ExpireOtherEntities(aBLTE, LoggedInUser(aBLTE).USER_ID, anEntity, DateTime.Now.Date);
                            aCropUse = anEntity;
                        }
                        else
                        {
                            aCropUse.USE = anEntity.USE;
                        }//end if

                        aBLTE.SaveChanges();

                        activateLinks<CROP_USE>(anEntity);
                    }//end using
                }//end using

                return new OperationResult.OK { ResponseResource = anEntity };
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
                    CROP_USE ObjectToBeDelete = aBLTE.CROP_USE.FirstOrDefault(am => am.ID == entityID);

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
                        aBLTE.CROP_USE.DeleteObject(ObjectToBeDelete);
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

        private bool Exists(BLTRDSEntities aBLTE, ref CROP_USE anEntity)
        {
            CROP_USE existingEntity;
            CROP_USE thisEntity = anEntity;
            //check if it exists
            try
            {
                existingEntity = aBLTE.CROP_USE.FirstOrDefault(mt => string.Equals(mt.USE.ToUpper(), thisEntity.USE.ToUpper()));
        

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
            if (aBLTE.CROP_USE.Count() > 0)
                nextID = aBLTE.CROP_USE.OrderByDescending(p => p.CROP_USE_ID).First().CROP_USE_ID + 1;

            return nextID;
        }

        private void ExpireOtherEntities(BLTRDSEntities aBLTE, decimal userId, CROP_USE cropUse, DateTime dt)
        {
            //get all published, should only be 1
            List<CROP_USE> aiList = aBLTE.CROP_USE.Where(p => p.CROP_USE_ID == cropUse.CROP_USE_ID &&
                                                        p.VERSION.PUBLISHED_TIME_STAMP <= dt.Date).ToList();
            if (aiList == null) return;

            foreach (var p in aiList)
            {
                if (!p.Equals(cropUse))
                    p.VERSION = SetVersion(aBLTE, p.VERSION_ID, userId, StatusType.Expired, dt.Date);
            }//next
        }//end ExpireOtherEntities

        protected override void ExpireEntities(BLTRDSEntities aBLTE, decimal userId, decimal Id, DateTime dt)
        {
            //get all published, should only be 1
            List<CROP_USE> aiList = aBLTE.CROP_USE.Where(p => p.CROP_USE_ID == Id &&
                                                        p.VERSION.PUBLISHED_TIME_STAMP <= dt.Date).ToList();
            if (aiList == null) return;

            foreach (var p in aiList)
            {
                p.VERSION = SetVersion(aBLTE, p.VERSION_ID, userId, StatusType.Expired, dt.Date);
            }//next
        }
        #endregion


    }//end class CropUseHandler
}// end namespace