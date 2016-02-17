//------------------------------------------------------------------------------
//----- AIClassHandler ---------------------------------------------------------
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
    public class AIClassHandler: HandlerBase
    {
        #region Properties
        public override string entityName
        {
            get { return "AI_CLASS"; }
        }
        #endregion
        #region Routed Methods
        #region GetMethods
        //---------------------Returns List of objects---------------------
        // returns all AIClass
     //   [WiMRequiresRole(new string[] { AdminRole, PublisherRole, CreatorRole })]
        [HttpOperation(HttpMethod.GET)]
        public OperationResult Get()
        {
            List<AI_CLASS> aiClassList;
            try
            {
                //using (EasySecureString securedPassword = GetSecuredPassword())
                //{
                    using (BLTRDSEntities aBLTE = GetRDS())
                    {
                        aiClassList = GetEntities<AI_CLASS>(aBLTE).ToList();
                    }//end using
               // }//end using
                activateLinks<AI_CLASS>(aiClassList);

                return new OperationResult.OK { ResponseResource = aiClassList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

        // returns versioned AI Classes
        //[RequiresAuthentication]
        [HttpOperation(HttpMethod.GET, ForUriName = "GetVersionedAIClasss")]
        public OperationResult GetVersionedAIClasses(string status, string date)
        {
            ObjectQuery<AI_CLASS> aiClassQuery;
            List<AI_CLASS> aiClasses;
            try
            {
                StatusType statustype = getStatusType(status);
                DateTime? thisDate = ValidDate(date);
                if (!thisDate.HasValue)
                    thisDate = DateTime.Now.Date;

                //using (EasySecureString securedPassword = GetSecuredPassword())
                //{
                    using (BLTRDSEntities aBLTE = GetRDS())
                    {
                        aiClassQuery = GetEntities<AI_CLASS>(aBLTE);
                        switch (statustype)
                        {
                            case (StatusType.Published):
                                aiClassQuery.Where(ai => ai.VERSION.PUBLISHED_TIME_STAMP != null);
                                break;
                            case (StatusType.Reviewed):
                                aiClassQuery.Where(ai => ai.VERSION.REVIEWED_TIME_STAMP != null &&
                                                ai.VERSION.PUBLISHED_TIME_STAMP == null);
                                break;
                            //created
                            default:
                                aiClassQuery.Where(ai => ai.VERSION.REVIEWED_TIME_STAMP == null &&
                                                ai.VERSION.PUBLISHED_TIME_STAMP == null);
                                break;
                        }//end switch

                        aiClassQuery.Where(ai => !ai.VERSION.EXPIRED_TIME_STAMP.HasValue ||
                                            ai.VERSION.EXPIRED_TIME_STAMP < thisDate.Value.Date);

                        aiClasses = aiClassQuery.ToList();

                    }//end using
                //}//end using

                activateLinks<AI_CLASS>(aiClasses);

                return new OperationResult.OK { ResponseResource = aiClasses };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET
        
        // returns all active AIClass
        [HttpOperation(HttpMethod.GET)]
        public OperationResult Get(string date)
        {
            List<AI_CLASS> aiClassList = null;
            try
            {
                DateTime? thisDate = ValidDate(date);
                if (!thisDate.HasValue)
                    thisDate = DateTime.Now; 

                //set the date to current date if request is not authorized
                if (DateTime.Compare(thisDate.Value.Date, DateTime.Now.Date) < 0 && !CanManage()) thisDate = DateTime.Now.Date;
                
                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    aiClassList = GetActive(GetEntities<AI_CLASS>(aBLTE), thisDate.Value.Date).OrderBy(a=>a.AI_CLASS_NAME).ToList();
                    
                }//end using

                activateLinks<AI_CLASS>(aiClassList);

                return new OperationResult.OK { ResponseResource = aiClassList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }

        }//end HttpMethod.GET

        // returns all AIClass (active if date is supplied)
  //      [WiMRequiresRole(new string[] { AdminRole, PublisherRole, CreatorRole })]
        [HttpOperation(HttpMethod.GET, ForUriName="GetAIClasses")]
        public OperationResult GetAIClasses(Int32 aiClassID, [Optional] string date)
        {
            try
            {
                List<AI_CLASS> aiClassList;
                DateTime? thisDate = ValidDate(date);
                if (!thisDate.HasValue)
                    thisDate = DateTime.Now.Date;
                //set the date to current date if request is not authorized
                if (DateTime.Compare(thisDate.Value.Date, DateTime.Now.Date) < 0 && !CanManage()) thisDate = DateTime.Now.Date;
 
                if (aiClassID < 0)
                { return new OperationResult.BadRequest(); }

                //using (EasySecureString securedPassword = GetSecuredPassword())
                //{
                    using (BLTRDSEntities aBLTE = GetRDS())
                    {
                        IQueryable<AI_CLASS> query;
                        query = GetEntities<AI_CLASS>(aBLTE).Where(aic => aic.AI_CLASS_ID == aiClassID);

                        if (thisDate.HasValue)
                            query = GetActive(query, thisDate.Value.Date);

                        //process
                        aiClassList = query.ToList();
                    }//end using
              //  }//end using

                activateLinks<AI_CLASS>(aiClassList);

                return new OperationResult.OK { ResponseResource = aiClassList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET
        
        // returns active Active Ingredients for the given pulaID
        [HttpOperation(HttpMethod.GET, ForUriName = "GetActiveIngredientClasses")]
        public OperationResult GetActiveIngredientClasses(Int32 activeIngredientId, [Optional]string date)
        {
            try
            {
                DateTime? thisDate = ValidDate(date);
                if (!thisDate.HasValue)
                    thisDate = DateTime.Now.Date.Date;

                //set the date to current date if request is not authorized
                if (DateTime.Compare(thisDate.Value.Date, DateTime.Now.Date) < 0 && !CanManage()) thisDate = DateTime.Now.Date;

                List<AI_CLASS> aiClassList;
                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    IQueryable<AI_CLASS> query1 =
                        (from AI_AIClass in GetActive(aBLTE.ACTIVE_INGREDIENT_AI_CLASS.Where(ai => ai.ACTIVE_INGREDIENT_ID == activeIngredientId), thisDate.Value.Date)
                         join AIclass in aBLTE.AI_CLASS
                         on AI_AIClass.AI_CLASS_ID equals AIclass.AI_CLASS_ID
                         select AIclass).Distinct();

                    aiClassList = GetActive(query1, thisDate.Value.Date).ToList();

                    activateLinks<AI_CLASS>(aiClassList);

                }//end using

                return new OperationResult.OK { ResponseResource = aiClassList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

        
        //---------------------Returns individual objects---------------------
   //     [WiMRequiresRole(new string[] { AdminRole, PublisherRole, CreatorRole })]
        [HttpOperation(HttpMethod.GET, ForUriName = "GetEntity")]
        public OperationResult GetEntity(Int32 entityID)
        {
            AI_CLASS anAIClass;
            try
            {
                //using (EasySecureString securedPassword = GetSecuredPassword())
                //{
                    using (BLTRDSEntities aBLTE = GetRDS())
                    {
                        anAIClass = GetEntities<AI_CLASS>(aBLTE).SingleOrDefault(ai => ai.ID == entityID);

                    }//end using
             //   }//end using
            
                activateLinks<AI_CLASS>(anAIClass);

                return new OperationResult.OK { ResponseResource = anAIClass };
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
        public OperationResult POST(AI_CLASS anEntity)
        {
            try
            {
                using (EasySecureString securedPassword = GetSecuredPassword())
                {
                    using (BLTRDSEntities aBLTE = GetRDS(securedPassword))
                    {
                        if(!Exists(aBLTE, ref anEntity))
                        {
                            //create version
                            anEntity.VERSION_ID = SetVersion(aBLTE, anEntity.VERSION_ID, LoggedInUser(aBLTE).USER_ID, StatusType.Published, DateTime.Now.Date).VERSION_ID;

                            anEntity.AI_CLASS_ID = GetNextID(aBLTE);
                       
                            aBLTE.AI_CLASS.AddObject(anEntity);

                            aBLTE.SaveChanges();
                        }
                        activateLinks<AI_CLASS>(anEntity);
                    }//end using
                }//end using

                
                return new OperationResult.OK { ResponseResource = anEntity };
            }
            catch (Exception)
            { return new OperationResult.BadRequest(); }

        }//end HttpMethod.GET

        [WiMRequiresRole(new string[] { AdminRole, PublisherRole, CreatorRole })]
        [HttpOperation(HttpMethod.POST, ForUriName = "AddAIClassToAI")]
        public OperationResult AddAIClass(Int32 entityID, ACTIVE_INGREDIENT AI)
        {
            AI_CLASS anAIClass = null;
            //ACTIVE_INGREDIENT aAIngr = null;
            ACTIVE_INGREDIENT_AI_CLASS newAIC_AI = null;
            try
            {
                //Return BadRequest if missing required fields
                if (entityID <= 0 || AI.ACTIVE_INGREDIENT_ID <= 0)
                { return new OperationResult.BadRequest(); }

                using (EasySecureString securedPassword = GetSecuredPassword())
                {
                    using (BLTRDSEntities aBLTE = GetRDS(securedPassword))
                    {
                        decimal ai_ID = AI.ACTIVE_INGREDIENT_ID;
                        anAIClass = GetEntities<AI_CLASS>(aBLTE).SingleOrDefault(c => c.ID == entityID);
                        if (anAIClass == null || AI == null)
                        { return new OperationResult.BadRequest(); }

                        newAIC_AI = new ACTIVE_INGREDIENT_AI_CLASS();
                        newAIC_AI.ACTIVE_INGREDIENT_ID = AI.ACTIVE_INGREDIENT_ID;
                        newAIC_AI.AI_CLASS_ID = anAIClass.AI_CLASS_ID;

                        //check if this relationship already exists
                        if (!Exists1(aBLTE, ref newAIC_AI))
                        {
                            newAIC_AI.ACTIVE_INGREDIENT_AI_CLASS_ID = GetNextAIcAI_ID(aBLTE);

                            //create version
                            newAIC_AI.VERSION_ID = SetVersion(aBLTE, newAIC_AI.VERSION_ID, LoggedInUser(aBLTE).USER_ID, StatusType.Published, DateTime.Now.Date).VERSION_ID;

                            aBLTE.ACTIVE_INGREDIENT_AI_CLASS.AddObject(newAIC_AI);
                            aBLTE.SaveChanges();

                        }//end if

                    }//end using
                }//end using

                return new OperationResult.OK();
            }
            catch (Exception)
            { return new OperationResult.BadRequest(); }
        }//end HttpMethod.GET

        #endregion
        
        #region PUT/EDIT Methods

        [WiMRequiresRole(new string[] { AdminRole, PublisherRole })]
        [HttpOperation(HttpMethod.PUT)]
        public OperationResult Put(Int32 entityID, AI_CLASS anEntity)
        {
            //No editing of tables are allowed. An edit call will create and activate a new entity, and expire the old one
            USER_ loggedInUser;
            AI_CLASS anAIClass;
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

                        anAIClass = aBLTE.AI_CLASS.FirstOrDefault(c => c.ID == entityID);
                        if (anAIClass == null)
                        { return new OperationResult.BadRequest(); }

                        if (anAIClass.VERSION.PUBLISHED_TIME_STAMP.HasValue)
                        {
                            //can not edit a published entity. Create new
                            //assign next pulaID
                            anEntity.AI_CLASS_ID = anAIClass.AI_CLASS_ID;
                            //assign version
                            anEntity.VERSION_ID = SetVersion(aBLTE, anEntity.VERSION_ID, loggedInUser.USER_ID, StatusType.Published, DateTime.Now.Date).VERSION_ID;
                            aBLTE.AI_CLASS.AddObject(anEntity);
                            //expire originals
                            ExpireOtherEntities(aBLTE, LoggedInUser(aBLTE).USER_ID, anEntity, DateTime.Now.Date);
                            anAIClass = anEntity;
                        }
                        else
                        {
                            anAIClass.AI_CLASS_NAME = anEntity.AI_CLASS_NAME;
                        }//end if

                        aBLTE.SaveChanges();

                        activateLinks<AI_CLASS>(anAIClass);
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
                    AI_CLASS ObjectToBeDelete = aBLTE.AI_CLASS.FirstOrDefault(am => am.ID == entityID);

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
                        aBLTE.AI_CLASS.DeleteObject(ObjectToBeDelete);
                    }//end if

                    aBLTE.SaveChanges();
                }//end using
            }//end using

            //Return object to verify persisitance
            return new OperationResult.OK { };
        }//end HttpMethod.DELETE

        [WiMRequiresRole(new string[] { AdminRole, PublisherRole })]
        [HttpOperation(HttpMethod.DELETE, ForUriName = "RemoveAIClassFromAI")]
        public OperationResult RemoveAIClassFromAI(Int32 entityID, Int32 aiEntityID)
        {
            //No deleting of tables are allowed. An delete call will expire the entity
            USER_ loggedInUser;
            AI_CLASS anAIClass;
            ACTIVE_INGREDIENT anAI;
            ACTIVE_INGREDIENT_AI_CLASS anAIC;

            try
            {
                DateTime? thisDate = DateTime.Now.Date;

                if (entityID <= 0 || aiEntityID <= 0)
                { return new OperationResult.BadRequest { }; }

                using (EasySecureString securedPassword = GetSecuredPassword())
                {
                    using (BLTRDSEntities aBLTE = GetRDS(securedPassword))
                    {
                        loggedInUser = LoggedInUser(aBLTE);
                        anAIClass = aBLTE.AI_CLASS.FirstOrDefault(c => c.ID == entityID);
                        anAI = aBLTE.ACTIVE_INGREDIENT.FirstOrDefault(c => c.ID == aiEntityID);

                        anAIC = aBLTE.ACTIVE_INGREDIENT_AI_CLASS.FirstOrDefault(c => c.AI_CLASS_ID == anAIClass.AI_CLASS_ID && c.ACTIVE_INGREDIENT_ID == anAI.ACTIVE_INGREDIENT_ID);

                        if (anAIC == null)
                        { return new OperationResult.BadRequest(); }
                        //NOTE: ShapeID can not be changed
                        if (anAIC.VERSION.PUBLISHED_TIME_STAMP.HasValue)
                        {
                            //expire the Product-AI relationship
                            anAIC.VERSION = SetVersion(aBLTE, anAIC.VERSION_ID, LoggedInUser(aBLTE).USER_ID, StatusType.Expired, thisDate.Value.Date);
                        }
                        else
                        {
                            aBLTE.ACTIVE_INGREDIENT_AI_CLASS.DeleteObject(anAIC);
                        }//end if

                        aBLTE.SaveChanges();

                    }//end using
                }//end using

                return new OperationResult.OK();
            }
            catch (Exception)
            { return new OperationResult.BadRequest(); }

        }//end HttpMethod.GET
        #endregion

        #endregion
        #region Helper Methods
        private bool Exists(BLTRDSEntities aBLTE, ref AI_CLASS anEntity)
        {
            AI_CLASS existingEntity;
            AI_CLASS thisEntity = anEntity;
            //check if it exists
            try
            {
                existingEntity = aBLTE.AI_CLASS.FirstOrDefault(mt => string.Equals(mt.AI_CLASS_NAME.ToUpper(), thisEntity.AI_CLASS_NAME.ToUpper()));
              

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

        private bool Exists1(BLTRDSEntities aBLTE, ref ACTIVE_INGREDIENT_AI_CLASS anEntity)
        {
            ACTIVE_INGREDIENT_AI_CLASS existingEntity;
            ACTIVE_INGREDIENT_AI_CLASS thisEntity = anEntity;
            //check if it exists
            try
            {
                existingEntity = aBLTE.ACTIVE_INGREDIENT_AI_CLASS.FirstOrDefault(pai => pai.ACTIVE_INGREDIENT_ID == thisEntity.ACTIVE_INGREDIENT_ID && pai.AI_CLASS_ID == thisEntity.AI_CLASS_ID);


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

        private void ExpireOtherEntities(BLTRDSEntities aBLTE, decimal userId, AI_CLASS aiClass, DateTime dt)
        {
            //get all published, should only be 1
            List<AI_CLASS> aiList = aBLTE.AI_CLASS.Where(p => p.AI_CLASS_ID == aiClass.AI_CLASS_ID &&
                                                              p.VERSION.PUBLISHED_TIME_STAMP <= dt.Date).ToList();
            if (aiList == null) return;

            foreach (var p in aiList)
            {
                if (!p.Equals(aiClass))
                    p.VERSION = SetVersion(aBLTE, p.VERSION_ID, userId, StatusType.Expired, dt.Date);
            }//next
        }//end ExpireOtherEntities
        private decimal GetNextID(BLTRDSEntities aBLTE)
        {
            //create pulaID
            Decimal nextID = 1;
            if (aBLTE.AI_CLASS.Count() > 0)
                nextID = aBLTE.AI_CLASS.OrderByDescending(p => p.AI_CLASS_ID).First().AI_CLASS_ID + 1;

            return nextID;
        }
        private decimal GetNextAIcAI_ID(BLTRDSEntities aBLTE)
        {
            //create pulaID
            Decimal nextID = 1;
            if (aBLTE.ACTIVE_INGREDIENT_AI_CLASS.Count() > 0)
                nextID = aBLTE.ACTIVE_INGREDIENT_AI_CLASS.OrderByDescending(p => p.ACTIVE_INGREDIENT_AI_CLASS_ID).First().ACTIVE_INGREDIENT_AI_CLASS_ID + 1;

            return nextID;
        }

        
        protected override void ExpireEntities(BLTRDSEntities aBLTE, decimal userId, decimal Id, DateTime dt)
        {
            //get all published, should only be 1
            List<AI_CLASS> aiList = aBLTE.AI_CLASS.Where(p => p.AI_CLASS_ID == Id &&
                                                         p.VERSION.PUBLISHED_TIME_STAMP <= dt.Date).ToList();
            if (aiList == null) return;

            foreach (var p in aiList)
            {
                p.VERSION = SetVersion(aBLTE, p.VERSION_ID, userId, StatusType.Expired, dt.Date);
            }//next
        }
        #endregion

    }//end class AIClassHandler
}//end namespace