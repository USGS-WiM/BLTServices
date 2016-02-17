//------------------------------------------------------------------------------
//----- PULALimitationsHandler ---------------------------------------------------
//------------------------------------------------------------------------------

//-------1---------2---------3---------4---------5---------6---------7---------8
//       01234567890123456789012345678901234567890123456789012345678901234567890
//-------+---------+---------+---------+---------+---------+---------+---------+

// copyright:   2012 WiM - USGS

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
// 11.09.12 - TR - Changed class name from SiteHandler to PULALimitationsHandler and removed ForUriName "Sites" references
// 10.19.12 - JB - Created from STN
#endregion


using BLTServices.Authentication;
using BLTServices.Resources;

using OpenRasta.Web;
using OpenRasta.Security;

using System;
using System.Data;
using System.Data.EntityClient;
using System.Data.Metadata.Edm;
using System.Data.Objects;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Runtime.InteropServices;


namespace BLTServices.Handlers
{
    public class PULALimitationsHandler: HandlerBase
    {

        #region Properties
        public override string entityName
        {
            get { return "PULA_LIMITATIONS"; }
        }
        #endregion
        #region Routed Methods
        #region GetMethods
        //---------------------Returns List of objects---------------------
        // returns all PULA_LIMITATIONS
      //  [WiMRequiresRole(new string[] { AdminRole, PublisherRole, CreatorRole })]
        [HttpOperation(HttpMethod.GET)]
        public OperationResult Get()
        {
            List<PULA_LIMITATIONS> pulaLimit;
            try
            {                
                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    pulaLimit = GetEntities<PULA_LIMITATIONS>(aBLTE).ToList();
                }//end using

                activateLinks<PULA_LIMITATIONS>(pulaLimit);

                return new OperationResult.OK { ResponseResource = pulaLimit };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

        // returns all PULAs
       // [RequiresAuthentication]
        [HttpOperation(HttpMethod.GET, ForUriName = "GetVersionedPULALimitations")]
        public OperationResult GetVersionedPULALimitations(string status, string date)
        {
            ObjectQuery<PULA_LIMITATIONS> pulaLimitQuery;
            List<PULA_LIMITATIONS> pulaLimitList;
            try
            {
                StatusType statustype = getStatusType(status);
                DateTime? thisDate = ValidDate(date);
                if (!thisDate.HasValue)
                    thisDate = DateTime.Now.Date;
                                
                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    pulaLimitQuery = GetEntities<PULA_LIMITATIONS>(aBLTE);
                    switch (statustype)
                    {
                        case (StatusType.Published):
                            pulaLimitQuery.Where(ai => ai.VERSION.PUBLISHED_TIME_STAMP != null);
                            break;
                        case (StatusType.Reviewed):
                            pulaLimitQuery.Where(ai => ai.VERSION.REVIEWED_TIME_STAMP != null &&
                                            ai.VERSION.PUBLISHED_TIME_STAMP == null);
                            break;
                        //created
                        default:
                            pulaLimitQuery.Where(ai => ai.VERSION.REVIEWED_TIME_STAMP == null &&
                                            ai.VERSION.PUBLISHED_TIME_STAMP == null);
                            break;
                    }//end switch

                    pulaLimitQuery.Where(ai => !ai.VERSION.EXPIRED_TIME_STAMP.HasValue ||
                                        ai.VERSION.EXPIRED_TIME_STAMP < thisDate.Value.Date);

                    pulaLimitList = pulaLimitQuery.ToList();

                }//end using

                activateLinks<PULA_LIMITATIONS>(pulaLimitList);

                return new OperationResult.OK { ResponseResource = pulaLimitList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

        // returns all active PULA_LIMITATIONS
        [HttpOperation(HttpMethod.GET)]
        public OperationResult Get(string date)
        {
            List<PULA_LIMITATIONS> pulaLimit = null;
            try
            {
                DateTime? thisDate = ValidDate(date);
                if (!thisDate.HasValue)
                    thisDate = DateTime.Now.Date; 

                //set the date to current date if request is not authorized
                if (DateTime.Compare(thisDate.Value.Date, DateTime.Now.Date) < 0 && !CanManage()) thisDate = DateTime.Now.Date;
              
                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    pulaLimit = GetActive(GetEntities<PULA_LIMITATIONS>(aBLTE), thisDate.Value.Date).ToList();
                }//end using

                activateLinks<PULA_LIMITATIONS>(pulaLimit);

                return new OperationResult.OK { ResponseResource = pulaLimit };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }

        }//end HttpMethod.GET

        // returns all PULALimitations (active if date is supplied)
      //  [WiMRequiresRole(new string[] { AdminRole, PublisherRole, CreatorRole, Public })]
        [HttpOperation(HttpMethod.GET)]
        public OperationResult Get(Int32 pulaLimitationsID, [Optional] string date)
        {
            try
            {
                List<PULA_LIMITATIONS> pulaLimitationList;
                DateTime? thisDate = ValidDate(date);

                if (pulaLimitationsID < 0)
                { return new OperationResult.BadRequest(); }
                                
                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    IQueryable<PULA_LIMITATIONS> query;
                    query = GetEntities<PULA_LIMITATIONS>(aBLTE).Where(pl => pl.PULA_LIMITATION_ID == pulaLimitationsID);

                    if (thisDate.HasValue)
                        query = GetActive(query, thisDate.Value.Date);

                    //process
                    pulaLimitationList = query.ToList();
                }//end using

                activateLinks<PULA_LIMITATIONS>(pulaLimitationList);

                return new OperationResult.OK { ResponseResource = pulaLimitationList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

        // returns Active PULA_LIMITATIONS for the given pulaID
        [HttpOperation(HttpMethod.GET, ForUriName = "GetPULALimitations")]
        public OperationResult GetPULALimitations(Int32 pulaId, [Optional]string date)
        {
            try
            {
                DateTime? thisDate = ValidDate(date);
                if (!thisDate.HasValue)
                    thisDate = DateTime.Now.Date;

                //set the date to current date if request is not authorized
                if (DateTime.Compare(thisDate.Value.Date, DateTime.Now.Date) < 0 && !CanManage()) thisDate = DateTime.Now.Date;

                List<PULA_LIMITATIONS> pulaLimitationList;
                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    IQueryable<PULA_LIMITATIONS> query1 = GetEntities<PULA_LIMITATIONS>(aBLTE).Where(a => a.PULA_ID == pulaId);

                    pulaLimitationList = GetActive(query1, thisDate.Value.Date).ToList();

                    activateLinks<PULA_LIMITATIONS>(pulaLimitationList);

                }//end using

                return new OperationResult.OK { ResponseResource = pulaLimitationList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

        // returns Active PULA_LIMITATIONS for the given pulaID
        [HttpOperation(HttpMethod.GET, ForUriName = "GetMapperLimitations")]
        public OperationResult GetMapperLimitations(Int32 pulaId, Int32 pulaSHPID, string date)
        {
            try
            {
                DateTime? thisDate = ValidDate(date);
                if (!thisDate.HasValue)
                    thisDate = DateTime.Now.Date;

                //set the date to current date if request is not authorized
                if (DateTime.Compare(thisDate.Value.Date, DateTime.Now.Date) < 0 && !CanManage()) thisDate = DateTime.Now.Date;

                MapperLimitations MapLimitList = new MapperLimitations();

                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    ACTIVE_INGREDIENT_PULA thisPULA = aBLTE.ACTIVE_INGREDIENT_PULA.Where(x=>x.PULA_ID == pulaId && x.PULA_SHAPE_ID == pulaSHPID).FirstOrDefault();

                    //go get the limitations, then when populating each MapperLimit - go get each piece using the id of each
                    IQueryable<PULA_LIMITATIONS> query1 = GetEntities<PULA_LIMITATIONS>(aBLTE).Where(a => a.PULA_ID == pulaId);

                    List<PULA_LIMITATIONS> pulaLimitationList = GetActive(query1, thisDate.Value.Date).ToList();
                    MapLimitList.MapperLimits = pulaLimitationList.Select(p => new MapperLimit()
                    {
                        PULAID = pulaId,
                        PULASHPID = pulaSHPID,
                        NAME = GetAIorProdName(p.ACTIVE_INGREDIENT_ID, p.PRODUCT_ID, thisDate.Value.Date, aBLTE),
                        USE = GetActive(aBLTE.CROP_USE.Where(u => u.CROP_USE_ID == p.CROP_USE_ID), thisDate.Value.Date).FirstOrDefault().USE,
                        APPMETHOD = aBLTE.APPLICATION_METHOD.Where(u => u.APPLICATION_METHOD_ID == p.APPLICATION_METHOD_ID).FirstOrDefault().METHOD,
                        FORM = aBLTE.FORMULATION.Where(u => u.FORMULATION_ID == p.FORMULATION_ID).FirstOrDefault().FORM,
                        LIMIT = aBLTE.LIMITATIONS.Where(u => u.LIMITATION_ID == p.LIMITATION_ID).FirstOrDefault()
                    }).ToList();
                    
                }//end using

                return new OperationResult.OK { ResponseResource = MapLimitList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }

        //---------------------Returns individual objects---------------------
       // [WiMRequiresRole(new string[] { AdminRole, PublisherRole, CreatorRole })]
        [HttpOperation(HttpMethod.GET, ForUriName = "GetEntity")]
        public OperationResult GetEntity(Int32 entityID)
        {
            PULA_LIMITATIONS aPULALimit;
            try
            {               
                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    aPULALimit = GetEntities<PULA_LIMITATIONS>(aBLTE).SingleOrDefault(ai => ai.PULA_LIMITATION_ID == entityID);

                }//end using

                activateLinks<PULA_LIMITATIONS>(aPULALimit);

                return new OperationResult.OK { ResponseResource = aPULALimit };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

        #endregion

        #region Status Methods

        [WiMRequiresRole(new string[] { AdminRole, PublisherRole })]
        [HttpOperation(HttpMethod.GET, ForUriName = "UpdatePulaLimitationsStatus")]
        public OperationResult UpdatePulaLimitationsStatus(Int32 entityID, string status, [Optional] string date)
        {
            //No editing of tables are allowed. An edit call will create and activate a new entity, and expire the old one
            USER_ loggedInUser;
            PULA_LIMITATIONS aPULALimitation;
            try
            {
                StatusType statusType = getStatusType(status);
                DateTime? thisDate = ValidDate(date);
                if (!thisDate.HasValue || thisDate.Value.Date < DateTime.Now.Date)
                    thisDate = DateTime.Now.Date;

                if (entityID <= 0 || string.IsNullOrEmpty(status) || statusType == StatusType.Created)
                { return new OperationResult.BadRequest(); }

                using (EasySecureString securedPassword = GetSecuredPassword())
                {
                    using (BLTRDSEntities aBLTE = GetRDS(securedPassword))
                    {
                        //version user
                        loggedInUser = LoggedInUser(aBLTE);

                        aPULALimitation = aBLTE.PULA_LIMITATIONS.FirstOrDefault(ai => ai.PULA_LIMITATION_ID == entityID);

                        if (aPULALimitation == null)
                        { return new OperationResult.BadRequest(); }

                        //assign version
                        aPULALimitation.VERSION_ID = SetVersion(aBLTE, aPULALimitation.VERSION_ID, loggedInUser.USER_ID, statusType, thisDate.Value.Date).VERSION_ID;

                        aBLTE.SaveChanges();
                        activateLinks<PULA_LIMITATIONS>(aPULALimitation);

                    }//end using
                }//end using

                return new OperationResult.OK { ResponseResource = aPULALimitation };
            }
            catch (Exception)
            { return new OperationResult.BadRequest(); }

        }//end HttpMethod.GET

        #endregion

        #region POST Methods

        [WiMRequiresRole(new string[] { AdminRole, PublisherRole, CreatorRole })]
        [HttpOperation(HttpMethod.POST)]
        public OperationResult POST(PULA_LIMITATIONS anEntity)
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
                            
                            aBLTE.PULA_LIMITATIONS.AddObject(anEntity);

                            aBLTE.SaveChanges();
                        }//end if
                        activateLinks<PULA_LIMITATIONS>(anEntity);
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
        public OperationResult Put(Int32 entityID, PULA_LIMITATIONS anEntity)
        {
            //No editing of tables are allowed. An edit call will create and activate a new entity, and expire the old one
            USER_ loggedInUser;
            PULA_LIMITATIONS aPULALimit;
            try
            {
                if (entityID >= 0)
                { return new OperationResult.BadRequest(); }

                using (EasySecureString securedPassword = GetSecuredPassword())
                {
                    using (BLTRDSEntities aBLTE = GetRDS(securedPassword))
                    {
                        //version user
                        loggedInUser = LoggedInUser(aBLTE);

                        aPULALimit = aBLTE.PULA_LIMITATIONS.FirstOrDefault(ai => ai.PULA_LIMITATION_ID == entityID);
                        if (aPULALimit == null)
                        { return new OperationResult.BadRequest(); }

                        if (aPULALimit.VERSION.PUBLISHED_TIME_STAMP.HasValue)
                        {
                            //can not edit a published entity. create new
                            // assign Version
                            anEntity.VERSION_ID = SetVersion(aBLTE, anEntity.VERSION_ID, loggedInUser.USER_ID, StatusType.Published, DateTime.Now.Date).VERSION_ID;
                            aBLTE.PULA_LIMITATIONS.AddObject(anEntity);
                            //expire originals
                            ExpireOtherEntities(aBLTE, LoggedInUser(aBLTE).USER_ID, anEntity, DateTime.Now.Date);
                            aPULALimit = anEntity;
                        }
                        else
                        {
                            aPULALimit.ACTIVE_INGREDIENT_ID = anEntity.ACTIVE_INGREDIENT_ID;
                            aPULALimit.APPLICATION_METHOD_ID = anEntity.APPLICATION_METHOD_ID;
                            aPULALimit.CROP_USE_ID = anEntity.CROP_USE_ID;
                            aPULALimit.FORMULATION_ID = anEntity.FORMULATION_ID;
                            aPULALimit.LIMITATION_ID = anEntity.LIMITATION_ID;
                            aPULALimit.PRODUCT_ID = anEntity.PRODUCT_ID;
                            aPULALimit.PULA_ID = anEntity.PULA_ID;
                        }//end if
                            
                        aBLTE.SaveChanges();

                        activateLinks<PULA_LIMITATIONS>(anEntity);
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
                    PULA_LIMITATIONS ObjectToBeDelete = aBLTE.PULA_LIMITATIONS.FirstOrDefault(f => f.PULA_LIMITATION_ID == entityID);

                    if (ObjectToBeDelete == null)
                    { return new OperationResult.BadRequest(); }

                    //NOTE: ShapeID can not be changed
                    //need to see if the PULA this PULA_LIMITATION is a part of is published (PULA_LIMITATION is published when created)
                    ACTIVE_INGREDIENT_PULA thisPULA = aBLTE.ACTIVE_INGREDIENT_PULA.FirstOrDefault(a => a.PULA_ID == ObjectToBeDelete.PULA_ID);
                    if (thisPULA.IS_PUBLISHED == 1)
                    {
                        ObjectToBeDelete.VERSION_ID = SetVersion(ObjectToBeDelete.VERSION_ID, LoggedInUser(aBLTE).USER_ID, StatusType.Expired, DateTime.Now.Date).VERSION_ID;
                    }
                    else
                    {
                        aBLTE.PULA_LIMITATIONS.DeleteObject(ObjectToBeDelete);
                    }//end if

                    aBLTE.SaveChanges();
                }//end using
            }//end using

            //Return object to verify persisitance
            return new OperationResult.OK { };
        }//end HttpMethod.GET

        #endregion
        
        #endregion
        #region Helper Methods
        private bool Exists(BLTRDSEntities aBLTE, ref PULA_LIMITATIONS anEntity)
        {
            PULA_LIMITATIONS existingEntity;
            PULA_LIMITATIONS thisEntity = anEntity;
            //check if it exists
            try
            {
                existingEntity = aBLTE.PULA_LIMITATIONS.FirstOrDefault(mt => mt.PULA_ID == thisEntity.PULA_ID &&
                                                                            (mt.ACTIVE_INGREDIENT_ID == thisEntity.ACTIVE_INGREDIENT_ID || thisEntity.ACTIVE_INGREDIENT_ID <= 0 || thisEntity.ACTIVE_INGREDIENT_ID == null) &&
                                                                            (mt.PRODUCT_ID == thisEntity.PRODUCT_ID || thisEntity.PRODUCT_ID <= 0 || thisEntity.PRODUCT_ID == null) &&
                                                                            (mt.LIMITATION_ID == thisEntity.LIMITATION_ID || thisEntity.LIMITATION_ID <= 0 || thisEntity.LIMITATION_ID == null) &&
                                                                            (mt.CROP_USE_ID == thisEntity.CROP_USE_ID || thisEntity.CROP_USE_ID <= 0 || thisEntity.CROP_USE_ID == null) &&
                                                                            (mt.APPLICATION_METHOD_ID == thisEntity.APPLICATION_METHOD_ID || thisEntity.APPLICATION_METHOD_ID <= 0 || thisEntity.APPLICATION_METHOD_ID == null) &&
                                                                            (mt.FORMULATION_ID == thisEntity.FORMULATION_ID || thisEntity.FORMULATION_ID <= 0 || thisEntity.FORMULATION_ID == null)
                                                                            );
            
                if (existingEntity == null)
                    return false;

                //if exists then update ref
                anEntity = existingEntity;
                return true;

            }
            catch (Exception)
            {
                return false;
            }
        }//end Exists
        protected void ExpireOtherEntities(BLTRDSEntities aBLTE, decimal userId, PULA_LIMITATIONS pulaLimitation, DateTime dt)
        {
            //get all published, should only be 1
            List<PULA_LIMITATIONS> aiList = aBLTE.PULA_LIMITATIONS.Where(p => p.PULA_LIMITATION_ID == pulaLimitation.PULA_LIMITATION_ID &&
                                                                                p.VERSION.PUBLISHED_TIME_STAMP <= dt.Date).ToList();
            if (aiList == null) return;

            foreach (var p in aiList)
            {
                if (!p.Equals(pulaLimitation))
                    p.VERSION = SetVersion(aBLTE, p.VERSION_ID, userId, StatusType.Expired, dt.Date);
            }//next
        }//end ExpireOtherEntities
        protected override void ExpireEntities(BLTRDSEntities aBLTE, decimal userId, decimal Id, DateTime dt)
        {
            //get all published, should only be 1
            List<PULA_LIMITATIONS> pulaLimitList = aBLTE.PULA_LIMITATIONS.Where(p => p.PULA_LIMITATION_ID == Id &&
                                                               p.VERSION.PUBLISHED_TIME_STAMP <= dt.Date).ToList();
            if (pulaLimitList == null) return;

            foreach (var p in pulaLimitList)
            {
                p.VERSION = SetVersion(aBLTE, p.VERSION_ID, userId, StatusType.Expired, dt.Date);
            }//next
        }

        private string GetAIorProdName(decimal? AI_ID, decimal? PROD_ID, DateTime d, BLTRDSEntities aBLTE)
        {
            string name = string.Empty;
            //IQueryable<PULA_LIMITATIONS> query1 = GetEntities<PULA_LIMITATIONS>(aBLTE).Where(a => a.PULA_ID == pulaId);
            //List<PULA_LIMITATIONS> pulaLimitationList = GetActive(query1, thisDate.Value.Date).ToList();

            if (AI_ID > 0)
            {
                IQueryable<ACTIVE_INGREDIENT> qA = aBLTE.ACTIVE_INGREDIENT.Where(x => x.ACTIVE_INGREDIENT_ID == AI_ID);
                
                name = GetActive(qA, d).FirstOrDefault().INGREDIENT_NAME;
                    
            }
            else
            {

                IQueryable<PRODUCT> qP = aBLTE.PRODUCT.Where(x => x.PRODUCT_ID == PROD_ID);

                PRODUCT qprod = GetActive(qP, d).FirstOrDefault();
                name = qprod.PRODUCT_NAME + " [" + qprod.PRODUCT_REGISTRATION_NUMBER + "]";

                //PRODUCT aProd = GetActive(aBLTE.PRODUCT.Where(x => x.PRODUCT_ID == PROD_ID).FirstOrDefault, d);
                //name = aProd.PRODUCT_NAME 
            }
            return name;
        }//end HttpMethod.GET
        
        #endregion
        
    }//end class PULALimitationsHandler

}//end namespace