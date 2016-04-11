//------------------------------------------------------------------------------
//----- ActiveIngredientHander -------------------------------------------------
//------------------------------------------------------------------------------

//-------1---------2---------3---------4---------5---------6---------7---------8
//       01234567890123456789012345678901234567890123456789012345678901234567890
//-------+---------+---------+---------+---------+---------+---------+---------+

// copyright:   2013 WiM - USGS

//    authors:  Jeremy K. Newson USGS Wisconsin Internet Mapping
//              
//  
//   purpose:   Handles Peak summary resources through the HTTP uniform interface.
//              Equivalent to the controller in MVC.
//
//discussion:   Handlers are objects which handle all interaction with resources in 
//              this case the resources are POCO classes derived from the EF. 
//              https://github.com/openrasta/openrasta/wiki/Handlers
//
//              

#region Comments
// 05.14.13 - JKN- Redesigned handler and updated methods
// 11.09.12 - TR - Changed class name from SiteHandler to PULALimitationsHandler and removed ForUriName "Sites" references
// 10.19.12 - JB - Created from STN
#endregion

using BLTServices.Authentication;

using OpenRasta.Web;
using OpenRasta.Security;
using OpenRasta.Diagnostics;

using System;
using System.Data;
using System.Data.EntityClient;
using System.Data.Metadata.Edm;
using System.Data.Objects;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Reflection;
using System.Web;
using System.Runtime.InteropServices;


namespace BLTServices.Handlers
{
    public class ActiveIngredientHandler : HandlerBase
    {

        #region Properties
        public override string entityName
        {
            get { return "ACTIVE_INGREDIENT"; }
        }
        #endregion
        #region Routed Methods
        #region GetMethods
        //---------------------Returns List of objects---------------------
        // returns all PULAs
        //[WiMRequiresRole(new string[] { AdminRole, PublisherRole, CreatorRole })]
        [HttpOperation(HttpMethod.GET)]
        public OperationResult Get()
        {
            List<ACTIVE_INGREDIENT> aiList;
            try
            {
                //using (EasySecureString securedPassword = GetSecuredPassword())
                //{
                    using (BLTRDSEntities aBLTE = GetRDS())
                    {
                        aiList = GetEntities<ACTIVE_INGREDIENT>(aBLTE).ToList();
                        aiList = aiList.OrderBy(a => a.INGREDIENT_NAME).ToList();
                    }//end using
                //}//end using

                activateLinks<ACTIVE_INGREDIENT>(aiList);

                return new OperationResult.OK { ResponseResource = aiList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

        // returns all PULAs
        //[RequiresAuthentication]
        [HttpOperation(HttpMethod.GET, ForUriName = "GetVersionedActiveIngredients")]
        public OperationResult GetVersionedActiveIngredients(string status, string date)
        {
            ObjectQuery<ACTIVE_INGREDIENT> aiQuery;
            List<ACTIVE_INGREDIENT> activeIngredient;
            try
            {
                StatusType statustype = getStatusType(status);
                DateTime? thisDate = ValidDate(date);
                if (!thisDate.HasValue)
                    thisDate = DateTime.Now;

                using (EasySecureString securedPassword = GetSecuredPassword())
                {
                    using (BLTRDSEntities aBLTE = GetRDS(securedPassword))
                    {
                        aiQuery = GetEntities<ACTIVE_INGREDIENT>(aBLTE);
                        switch (statustype)
                        {
                            case (StatusType.Published):
                                aiQuery.Where(ai => ai.VERSION.PUBLISHED_TIME_STAMP != null);
                                break;
                            case (StatusType.Reviewed):
                                aiQuery.Where(ai => ai.VERSION.REVIEWED_TIME_STAMP != null &&
                                                ai.VERSION.PUBLISHED_TIME_STAMP == null);
                                break;
                            //created
                            default:
                                aiQuery.Where(ai => ai.VERSION.REVIEWED_TIME_STAMP == null &&
                                                ai.VERSION.PUBLISHED_TIME_STAMP == null);
                                break;
                        }//end switch

                        aiQuery.Where(ai => !ai.VERSION.EXPIRED_TIME_STAMP.HasValue ||
                                            ai.VERSION.EXPIRED_TIME_STAMP < thisDate.Value);

                        activeIngredient = aiQuery.ToList();
                        activeIngredient = activeIngredient.OrderBy(a => a.INGREDIENT_NAME).ToList();
                    }//end using
                }//end using

                activateLinks<ACTIVE_INGREDIENT>(activeIngredient);

                return new OperationResult.OK { ResponseResource = activeIngredient };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET


        // returns all active Ingredients
        [HttpOperation(HttpMethod.GET)]
        public OperationResult Get(string date)
        {
            List<ACTIVE_INGREDIENT> aiList = null;
            try
            {
                DateTime? thisDate = ValidDate(date);
                if (!thisDate.HasValue)
                    thisDate = DateTime.Now; 
                
                //set the date to current date if request is not authorized
                if (DateTime.Compare(thisDate.Value, DateTime.Now) < 0 && !CanManage()) thisDate = DateTime.Now;

                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    aiList = GetActive(GetEntities<ACTIVE_INGREDIENT>(aBLTE), thisDate.Value).OrderBy(a=>a.INGREDIENT_NAME).ToList();
                }//end using
                activateLinks<ACTIVE_INGREDIENT>(aiList);

                return new OperationResult.OK { ResponseResource = aiList };
            }
            catch (Exception ex)
            {
               return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }

        }//end HttpMethod.GET

       // [RequiresAuthentication]
        [HttpOperation(HttpMethod.GET, ForUriName = "GetAI")]
        public OperationResult Get(Int32 activeIngredientID, [Optional] string date)
        {
            try
            {
                List<ACTIVE_INGREDIENT> aiList;
                DateTime? thisDate = ValidDate(date);
                if (!thisDate.HasValue)
                    thisDate = DateTime.Now;

                //set the date to current date if request is not authorized
                if (DateTime.Compare(thisDate.Value, DateTime.Now) < 0 && !CanManage()) thisDate = DateTime.Now;

                if (activeIngredientID < 0)
                { return new OperationResult.BadRequest(); }

                //using (EasySecureString securedPassword = GetSecuredPassword())
                //{
                    using (BLTRDSEntities aBLTE = GetRDS())
                    {
                        IQueryable<ACTIVE_INGREDIENT> query;
                        query = GetEntities<ACTIVE_INGREDIENT>(aBLTE).Where(ai => ai.ACTIVE_INGREDIENT_ID == activeIngredientID);
        
                        if (thisDate.HasValue)
                            query = GetActive(query, thisDate.Value);

                        //process
                        aiList = query.ToList();
                    }//end using
                //}//end using

                activateLinks<ACTIVE_INGREDIENT>(aiList);

                return new OperationResult.OK { ResponseResource = aiList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

        // returns active Active Ingredients for the given pulaID
        [HttpOperation(HttpMethod.GET, ForUriName = "GetPULAactiveIngredients")]
        public OperationResult GetPULAactiveIngredients(Int32 pulaId, [Optional]string date)
        {
            try
            {
                DateTime? thisDate = ValidDate(date);
                if (!thisDate.HasValue)
                    thisDate = DateTime.Now;
                //set the date to current date if request is not authorized
                if (DateTime.Compare(thisDate.Value, DateTime.Now) < 0 && !CanManage()) thisDate = DateTime.Now;
                
                List<ACTIVE_INGREDIENT> aiList;
                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    //active PULA_LIMITATION.. get all PULALimitation with pula id = and activedate = .. 
                    //then join ai (get all of them activeingredient table)..on matching ids, select distinct
                    IQueryable<ACTIVE_INGREDIENT> query1 =
                            (from PULALimit in GetActive(aBLTE.PULA_LIMITATIONS.Where(p => p.PULA_ID == pulaId), thisDate.Value)
                            join AI in aBLTE.ACTIVE_INGREDIENT
                            on PULALimit.ACTIVE_INGREDIENT_ID equals AI.ACTIVE_INGREDIENT_ID
                            select AI).Distinct();

                    aiList = GetActive(query1,thisDate.Value).ToList();

                    activateLinks<ACTIVE_INGREDIENT>(aiList);

                }//end using

                return new OperationResult.OK { ResponseResource = aiList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET
    

        //---------------------Returns individual objects---------------------
    //    [WiMRequiresRole(new string[] { AdminRole, PublisherRole, CreatorRole })]
        [HttpOperation(HttpMethod.GET, ForUriName="GetEntity")]
        public OperationResult GetEntity(Int32 entityID)
        {
            ACTIVE_INGREDIENT anAI;
            try
            {
                //using (EasySecureString securedPassword = GetSecuredPassword())
                //{
                    using (BLTRDSEntities aBLTE = GetRDS())
                    {
                        anAI = GetEntities<ACTIVE_INGREDIENT>(aBLTE).SingleOrDefault(ai => ai.ID == entityID);

                    }//end using
                //}//end using

                activateLinks<ACTIVE_INGREDIENT>(anAI);

                return new OperationResult.OK { ResponseResource = anAI };
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
        public OperationResult POST(ACTIVE_INGREDIENT anEntity)
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

                            anEntity.ACTIVE_INGREDIENT_ID = GetNextID(aBLTE);

                            aBLTE.ACTIVE_INGREDIENT.AddObject(anEntity);

                            aBLTE.SaveChanges();
                        }
                        else
                        {
                            //it exists, check if expired
                            if (anEntity.VERSION.EXPIRED_TIME_STAMP.HasValue)
                            {
                                ACTIVE_INGREDIENT newAI = new ACTIVE_INGREDIENT();
                                newAI.INGREDIENT_NAME = anEntity.INGREDIENT_NAME;
                                newAI.PC_CODE = anEntity.PC_CODE;
                                newAI.CAS_NUMBER = anEntity.CAS_NUMBER;
                                newAI.VERSION_ID = SetVersion(aBLTE, newAI.VERSION_ID, LoggedInUser(aBLTE).USER_ID, StatusType.Published, DateTime.Now.Date).VERSION_ID;
                                newAI.ACTIVE_INGREDIENT_ID = anEntity.ACTIVE_INGREDIENT_ID;
                                //anEntity.ID = 0;
                                aBLTE.ACTIVE_INGREDIENT.AddObject(newAI);
                                aBLTE.SaveChanges();
                            }
                        }//end if

                        activateLinks<ACTIVE_INGREDIENT>(anEntity);                       
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
        public OperationResult Put(Int32 entityID, ACTIVE_INGREDIENT anEntity)
        {
            //No editing of tables are allowed. An edit call will create and activate a new entity, and expire the old one
            USER_ loggedInUser;
            ACTIVE_INGREDIENT aActiveIngredient;
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

                        aActiveIngredient = aBLTE.ACTIVE_INGREDIENT.FirstOrDefault(ai => ai.ID == entityID);

                        if (aActiveIngredient == null)
                        { return new OperationResult.BadRequest(); }

                        //all active ingredients are published when created... doesn't mean the PULA is published

                        if (aActiveIngredient.VERSION.PUBLISHED_TIME_STAMP.HasValue)
                        {
                            //can not edit a published entity. Create new
                            //use same active_ingredient_id
                            anEntity.ACTIVE_INGREDIENT_ID = aActiveIngredient.ACTIVE_INGREDIENT_ID;
                            //assign version
                            anEntity.VERSION_ID = SetVersion(aBLTE, anEntity.VERSION_ID, loggedInUser.USER_ID, StatusType.Published, DateTime.Now.Date).VERSION_ID;
                            aBLTE.ACTIVE_INGREDIENT.AddObject(anEntity);
                            //expire originals
                            ExpireOtherEntities(aBLTE, LoggedInUser(aBLTE).USER_ID, anEntity, DateTime.Now.Date);
                            aActiveIngredient = anEntity;
                        }
                        else
                        {
                            aActiveIngredient.INGREDIENT_NAME = anEntity.INGREDIENT_NAME;
                        }//end if

                        aBLTE.SaveChanges();

                        activateLinks<ACTIVE_INGREDIENT>(anEntity);
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

                    ACTIVE_INGREDIENT ObjectToBeDelete = aBLTE.ACTIVE_INGREDIENT.FirstOrDefault(am => am.ID == entityID);

                    if (ObjectToBeDelete == null)
                    { return new OperationResult.BadRequest(); }

                    //NOTE: ShapeID can not be changed
                    if (ObjectToBeDelete.VERSION.PUBLISHED_TIME_STAMP.HasValue)
                    {
                        //set the date to be first of following month 
                        int nextMo = DateTime.Now.Month + 1;
                        DateTime nextMonth = Convert.ToDateTime(nextMo + "/01/" + DateTime.Now.Year);
                        ObjectToBeDelete.VERSION_ID = SetVersion(ObjectToBeDelete.VERSION_ID, LoggedInUser(aBLTE).USER_ID, StatusType.Expired, nextMonth).VERSION_ID;
                    }
                    else
                    {
                        aBLTE.ACTIVE_INGREDIENT.DeleteObject(ObjectToBeDelete);
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
        private bool Exists(BLTRDSEntities aBLTE, ref ACTIVE_INGREDIENT anEntity)
        {
            ACTIVE_INGREDIENT existingEntity;
            ACTIVE_INGREDIENT thisEntity = anEntity;
            //check if it exists
            try
            {

                existingEntity = aBLTE.ACTIVE_INGREDIENT.FirstOrDefault(mt => string.Equals(mt.INGREDIENT_NAME.ToUpper(), thisEntity.INGREDIENT_NAME.ToUpper()) &&
                                                                            (string.Equals(mt.PC_CODE.ToUpper(),thisEntity.PC_CODE.ToUpper())) &&
                                                                            (string.Equals(mt.CAS_NUMBER.ToUpper(), thisEntity.CAS_NUMBER.ToUpper()) || string.IsNullOrEmpty(thisEntity.CAS_NUMBER)));
               

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
                if (aBLTE.ACTIVE_INGREDIENT.Count() > 0)
                    nextID = aBLTE.ACTIVE_INGREDIENT.OrderByDescending(p => p.ACTIVE_INGREDIENT_ID).First().ACTIVE_INGREDIENT_ID + 1;
                
                return nextID;
        }

        private decimal GetNextprID(BLTRDSEntities aBLTE)
        {
            //create pulaID
            Decimal nextID = 1;
            if (aBLTE.PRODUCT_ACTIVE_INGREDIENT.Count() > 0)
                nextID = aBLTE.PRODUCT_ACTIVE_INGREDIENT.OrderByDescending(p => p.PRODUCT_ACTIVE_INGREDIENT_ID).First().PRODUCT_ACTIVE_INGREDIENT_ID + 1;

            return nextID;
        }

        private void ExpireOtherEntities(BLTRDSEntities aBLTE, decimal userId, ACTIVE_INGREDIENT ai, DateTime dt)
        {
            //get all published, should only be 1
            List<ACTIVE_INGREDIENT> aiList = aBLTE.ACTIVE_INGREDIENT.Where(p => p.ACTIVE_INGREDIENT_ID == ai.ACTIVE_INGREDIENT_ID && 
                                                                                p.VERSION.PUBLISHED_TIME_STAMP <= dt.Date).ToList();
            if (aiList == null) return;

            foreach (var p in aiList)
            {
                if(!p.Equals(ai))
                    p.VERSION = SetVersion(aBLTE, p.VERSION_ID, userId, StatusType.Expired, dt);
            }//next
        }//end ExpireOtherEntities
        protected override void ExpireEntities(BLTRDSEntities aBLTE, decimal userId, decimal Id, DateTime dt)
        {
            //get all published, should only be 1
            List<ACTIVE_INGREDIENT> aiList = aBLTE.ACTIVE_INGREDIENT.Where(p => p.ACTIVE_INGREDIENT_ID == Id &&
                                                                                p.VERSION.PUBLISHED_TIME_STAMP <= dt).ToList();
            if (aiList == null) return;

            foreach (var p in aiList)
            {
                    p.VERSION = SetVersion(aBLTE, p.VERSION_ID, userId, StatusType.Expired, dt);
            }//next
        }//end ExpireEntities
        
       #endregion

    }//end class ActiveIngredientHandler

}//end namespace