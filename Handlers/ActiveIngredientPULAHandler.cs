//------------------------------------------------------------------------------
//----- ActiveIngredientPULAHander ---------------------------------------------
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
// 05.07.13 - JKN  -Created

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
using System.Net;
using System.ServiceModel.Syndication;
using System.Reflection;
using System.Web;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.Linq;
using BLTServices.Resources;

namespace BLTServices.Handlers
   
{

    public class ActiveIngredientPULAHander : HandlerBase
    {
        #region Properties
        public override string entityName
        {
            get { return "ACTIVE_INGREDIENT_PULA"; }
        }
        #endregion
        #region Routed Methods    
        #region Get Methods
        //---------------------Returns List of objects---------------------
        // returns all PULAs
 //       [WiMRequiresRole(new string[] { AdminRole, PublisherRole })]
        [HttpOperation(HttpMethod.GET, ForUriName="GetAll")]
        public OperationResult Get()
        {
            List<ACTIVE_INGREDIENT_PULA> aiPula;
            try
            {
                //using (EasySecureString securedPassword = GetSecuredPassword())
                //{
                    using (BLTRDSEntities aBLTE = GetRDS())
                    {
                        aiPula = GetEntities<ACTIVE_INGREDIENT_PULA>(aBLTE).ToList();
                    }//end using
               // }//end using

                activateLinks<ACTIVE_INGREDIENT_PULA>(aiPula);

                return new OperationResult.OK { ResponseResource = aiPula };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

        // returns all PULAs
       // [RequiresAuthentication]
        [HttpOperation(HttpMethod.GET, ForUriName = "GetVersionedPulas")]
        public OperationResult GetVersionedPulas(string status, string date)
        {
            ObjectQuery<ACTIVE_INGREDIENT_PULA> aiQuery;
            List<ACTIVE_INGREDIENT_PULA> aiPula;
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
                        aiQuery = GetEntities<ACTIVE_INGREDIENT_PULA>(aBLTE);
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
                                            ai.VERSION.EXPIRED_TIME_STAMP < thisDate.Value.Date);

                        aiPula=aiQuery.ToList();

                    }//end using
               // }//end using

                activateLinks<ACTIVE_INGREDIENT_PULA>(aiPula);

                return new OperationResult.OK { ResponseResource = aiPula };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET
        
        // returns all active PULAs
        [HttpOperation(HttpMethod.GET)]
        public OperationResult Get(string date)
        {
            List<ACTIVE_INGREDIENT_PULA> aiPula= null;
            try
            {
                DateTime? thisDate = ValidDate(date);
                if (!thisDate.HasValue)
                    thisDate = DateTime.Now.Date;
                
                //set the date to current date if request is not authorized
                if (DateTime.Compare(thisDate.Value.Date, DateTime.Now.Date) < 0 && !CanManage()) thisDate = DateTime.Now.Date;
               
                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    aiPula = GetActive(GetEntities<ACTIVE_INGREDIENT_PULA>(aBLTE), thisDate.Value.Date).ToList();
                }//end using
                activateLinks<ACTIVE_INGREDIENT_PULA>(aiPula);

                return new OperationResult.OK { ResponseResource = aiPula };
            }
            catch (Exception ex)
            {
               return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }

        }//end HttpMethod.GET

        // returns the active PULAS for the given PULAID
        [HttpOperation(HttpMethod.GET, ForUriName = "GetAIPULA")]
        public OperationResult GetAIPULA(Int32 pulaId, [Optional] string date)
        {
            List<ACTIVE_INGREDIENT_PULA> aiPulas;
            try
            {
                if (pulaId <= 0)
                { return new OperationResult.BadRequest { }; }

                DateTime? thisDate = ValidDate(date);
                if (!thisDate.HasValue)
                    thisDate = DateTime.Now.Date;

                //set the date to current date if request is not authorized
                if (DateTime.Compare(thisDate.Value.Date, DateTime.Now.Date) < 0 && !CanManage()) thisDate = DateTime.Now.Date;

                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    aiPulas = GetActive(GetEntities<ACTIVE_INGREDIENT_PULA>(aBLTE), thisDate.Value.Date).Where(p => p.PULA_SHAPE_ID == pulaId).ToList();
                }//end using

                activateLinks<ACTIVE_INGREDIENT_PULA>(aiPulas);

                return new OperationResult.OK { ResponseResource = aiPulas };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

        // returns active PULAS for the given shapeID
        [HttpOperation(HttpMethod.GET, ForUriName = "GetSimplePULAS")]
        public OperationResult GetSimplePULAS([Optional] string date)
        {
            PULAList aiPulas = new PULAList();
            try
            {
                DateTime? thisDate = ValidDate(date);
                if (!thisDate.HasValue)
                    thisDate = DateTime.Now.Date;


                //set the date to current date if request is not authorized
                if (DateTime.Compare(thisDate.Value.Date, DateTime.Now.Date) < 0 && !CanManage()) thisDate = DateTime.Now.Date;

                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    IQueryable<ACTIVE_INGREDIENT_PULA> query = GetEntities<ACTIVE_INGREDIENT_PULA>(aBLTE);


                    aiPulas.PULA = GetActive(GetEntities<ACTIVE_INGREDIENT_PULA>(aBLTE), thisDate.Value.Date).AsEnumerable().Select(p => new SimplePULA()
                    {
                        entityID = p.ID,
                        ShapeID = p.PULA_SHAPE_ID,
                        isPublished = p.IS_PUBLISHED,
                        Created = p.VERSION.CREATED_TIME_STAMP,
                        Published = p.VERSION.PUBLISHED_TIME_STAMP,
                        Expired = p.VERSION.EXPIRED_TIME_STAMP
                    }).ToList<SimplePULA>();
                }//end using   

                //activateLinks<SimplePULA>(aiPulas.PULA);

                return new OperationResult.OK { ResponseResource = aiPulas };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

        // returns active PULAS for these filter choices for internal app
      //  [WiMRequiresRole(new string[] { AdminRole, PublisherRole, CreatorRole })]
        [HttpOperation(HttpMethod.GET, ForUriName = "GetFilteredSimplePULAs")]
        public OperationResult GetFilteredSimplePULAs([Optional] string date, [Optional] Int32 activeIngredientID, [Optional] Int32 productID, [Optional] Int32 eventID)
        {
             PULAList aiList = new PULAList();
            IQueryable<ACTIVE_INGREDIENT_PULA> query = null;
            try
            {
                DateTime? thisDate = null;
                if (date != null)
                {
                    thisDate = ValidDate(date); 
                }
                
                Int32 FilteredActiveIngredient = (activeIngredientID > 0) ? activeIngredientID : -1;
                Int32 FilteredProduct = (productID > 0) ? productID : -1;
                Int32 FilteredEvent = (eventID > 0) ? eventID : -1;
                
                //changed validateDate to return today if null.. since need to return effective pulas
                if (!thisDate.HasValue)
                    thisDate = DateTime.Now.Date;
                
                using (BLTRDSEntities aBLTE = GetRDS())
                {
                   // query = GetEntities<ACTIVE_INGREDIENT_PULA>(aBLTE).Where(p=> p.VERSION.PUBLISHED_TIME_STAMP.Value <= thisDate.Value);
                    query = GetEntities<ACTIVE_INGREDIENT_PULA>(aBLTE).Where(p => p.EFFECTIVE_DATE.Value <= thisDate.Value);

                    if (FilteredEvent > 0)
                    {
                        query = (from PULA in query
                                 where PULA.EVENT_ID == FilteredEvent
                                 select PULA);
                    }//end if

                    if (FilteredActiveIngredient > 0)
                    {
                       query = (from PULA in query
                            join pLimit in aBLTE.PULA_LIMITATIONS
                            on PULA.PULA_ID equals pLimit.PULA_ID
                            where pLimit.ACTIVE_INGREDIENT_ID == FilteredActiveIngredient
                            select PULA);
                        
                    }//end if

                    if (FilteredProduct > 0)
                    {
                        query = (from PULA in query
                                 join pLimit in aBLTE.PULA_LIMITATIONS
                                 on PULA.PULA_ID equals pLimit.PULA_ID
                                 where pLimit.PRODUCT_ID == FilteredProduct
                                 select PULA);
                    }//end if


                    aiList.PULA = query.AsEnumerable().Select(p => new SimplePULA()
                        {
                            entityID = p.ID,
                            ShapeID = p.PULA_SHAPE_ID,
                            isPublished = p.IS_PUBLISHED,
                            Created = p.VERSION.CREATED_TIME_STAMP,
                            Published = p.VERSION.PUBLISHED_TIME_STAMP,
                            Effective = p.EFFECTIVE_DATE,
                            Expired = p.VERSION.EXPIRED_TIME_STAMP
                        }).ToList<SimplePULA>();

                }//end using   

                //activateLinks<SimplePULA>(aiList.PULA);

                return new OperationResult.OK { ResponseResource = aiList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

        // returns active PULAS for these filter choices for internal app
        [HttpOperation(HttpMethod.GET, ForUriName = "GetEffectiveSimplePULAs")]
        public OperationResult GetEffectiveSimplePULAs([Optional] string date, [Optional] Int32 activeIngredientID, [Optional] Int32 productID)
        {
            PULAList aiList = new PULAList();
            IQueryable<ACTIVE_INGREDIENT_PULA> query = null;
            try
            {
                DateTime? thisDate = null;
                if (date != "0")
                {
                    thisDate = ValidDate(date);
                }

                Int32 FilteredActiveIngredient = (activeIngredientID > 0) ? activeIngredientID : -1;
                Int32 FilteredProduct = (productID > 0) ? productID : -1;
                Boolean isManager = CanManage();
                //changed validateDate to return today if null.. since need to return effective pulas
                if (!thisDate.HasValue && !isManager)
                    thisDate = DateTime.Now.Date;

                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    query = GetActive(GetEntities<ACTIVE_INGREDIENT_PULA>(aBLTE), thisDate.Value.Date);

                    if (FilteredActiveIngredient > 0)
                    {
                        query = (from PULA in query
                                 join pLimit in aBLTE.PULA_LIMITATIONS
                                 on PULA.PULA_ID equals pLimit.PULA_ID
                                 where pLimit.ACTIVE_INGREDIENT_ID == FilteredActiveIngredient
                                 select PULA);

                    }//end if

                    if (FilteredProduct > 0)
                    {
                        query = (from PULA in query
                                 join pLimit in aBLTE.PULA_LIMITATIONS
                                 on PULA.PULA_ID equals pLimit.PULA_ID
                                 where pLimit.PRODUCT_ID == FilteredProduct
                                 select PULA);
                    }//end if


                    aiList.PULA = query.AsEnumerable().Select(p => new SimplePULA()
                    {
                        entityID = p.ID,
                        ShapeID = p.PULA_SHAPE_ID,
                        isPublished = p.IS_PUBLISHED,
                        Created = p.VERSION.CREATED_TIME_STAMP,
                        Published = p.VERSION.PUBLISHED_TIME_STAMP,
                        Expired = p.VERSION.EXPIRED_TIME_STAMP,
                        Effective = p.EFFECTIVE_DATE
                    }).ToList<SimplePULA>();

                }//end using   

                //activateLinks<SimplePULA>(aiList.PULA);

                return new OperationResult.OK { ResponseResource = aiList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET


        // returns active PULAS for the given shapeID
        [HttpOperation(HttpMethod.GET, ForUriName = "GetShapePULAS")]
        public OperationResult GetShapePULAS(Int32 shapeId, [Optional]string date)
        {
            ACTIVE_INGREDIENT_PULA aiPula;
            List<ACTIVE_INGREDIENT_PULA> aiPulaList;
            IQueryable<ACTIVE_INGREDIENT_PULA> query = null;
            try
            {
                Boolean mana = CanManage();
                //string role = Context.User.IsInRole();
                Boolean auth = IsAuthorized("REVIEW");
                Boolean isManager = (CanManage() || IsAuthorized("REVIEW"));
                DateTime? thisDate = ValidDate(date);
                if (!thisDate.HasValue)
                    thisDate = DateTime.Now.Date;

                //set the date to current date if request is not authorized
                if (DateTime.Compare(thisDate.Value.Date, DateTime.Now.Date) < 0 && !isManager) thisDate = DateTime.Now.Date;

                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    query = GetEntities<ACTIVE_INGREDIENT_PULA>(aBLTE).Where(p => p.PULA_SHAPE_ID == shapeId);

                    if (!isManager)
                        aiPula = GetActive(query, thisDate.Value.Date).FirstOrDefault();
                    else
                    { 
                        //remove expired
                        aiPulaList = query.Where(p => 
                                 ((!p.VERSION.EXPIRED_TIME_STAMP.HasValue) || thisDate.Value < p.VERSION.EXPIRED_TIME_STAMP)).ToList();
                   
                        if (aiPulaList.Where(p => p.VERSION.PUBLISHED_TIME_STAMP.HasValue)
                                .ToList().Count > 0) 
                            //published
                            aiPulaList = aiPulaList.Where(p => p.VERSION.PUBLISHED_TIME_STAMP.HasValue)
                                .ToList();
                        
                        else if (aiPulaList.Where(p => p.VERSION.REVIEWED_TIME_STAMP.HasValue)
                                .ToList().Count > 0)
                            //Reviewed
                            aiPulaList = aiPulaList.Where(p => p.VERSION.REVIEWED_TIME_STAMP.HasValue)
                                .ToList();

                        //take the first
                        aiPula = aiPulaList.FirstOrDefault();

                    }//end if
                
                }//end using   

                activateLinks<ACTIVE_INGREDIENT_PULA>(aiPula);

                return new OperationResult.OK { ResponseResource = aiPula };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

        //returns PULAs for a given eventID
        [HttpOperation(HttpMethod.GET, ForUriName = "GetEventPULAs")]
        public OperationResult GetEventSimplePULAs(Int32 eventId)
        {
            PULAList aiPulas = new PULAList();
            try
            {
                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    IQueryable<ACTIVE_INGREDIENT_PULA> query = GetEntities<ACTIVE_INGREDIENT_PULA>(aBLTE);
                    
                    aiPulas.PULA = aBLTE.ACTIVE_INGREDIENT_PULA.AsEnumerable().Where(aip => aip.EVENT_ID == eventId)
                        .Select(aip => new SimplePULA
                        {
                            entityID = aip.ID,
                            ShapeID = aip.PULA_SHAPE_ID,
                            isPublished = aip.IS_PUBLISHED,
                            Created = aip.VERSION.CREATED_TIME_STAMP,
                            Published = aip.VERSION.PUBLISHED_TIME_STAMP,
                            Effective = aip.EFFECTIVE_DATE,
                            Expired = aip.VERSION.EXPIRED_TIME_STAMP
                    }).ToList<SimplePULA>();
                }//end using   

                //activateLinks<SimplePULA>(aiPulas.PULA);

                return new OperationResult.OK { ResponseResource = aiPulas };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

       
     //   [WiMRequiresRole(new string[] { AdminRole, PublisherRole })]
        [HttpOperation(HttpMethod.GET, ForUriName = "GetEntity")]
        public OperationResult GetEntity(Int32 entityID)
        {
            ACTIVE_INGREDIENT_PULA anAIPULA;
            try
            {
                //using (EasySecureString securedPassword = GetSecuredPassword())
                //{
                    using (BLTRDSEntities aBLTE = GetRDS())
                    {
                        //assign pulaID
                        anAIPULA = GetEntities<ACTIVE_INGREDIENT_PULA>(aBLTE).SingleOrDefault(p => p.ID == entityID);
                        
                    }//end using
               // }//end using

                activateLinks<ACTIVE_INGREDIENT_PULA>(anAIPULA);

                return new OperationResult.OK { ResponseResource = anAIPULA };
            }
            catch (Exception)
            { return new OperationResult.BadRequest(); }

        }//end HttpMethod.GET

        #endregion

        #region Status Methods

        [WiMRequiresRole(new string[] { AdminRole, PublisherRole })]
        [HttpOperation(HttpMethod.GET, ForUriName = "UpdatePulaStatus")]
        public OperationResult UpdatePulaStatus(Int32 entityID, string status, [Optional] string date)
        {
            //No editing of tables are allowed. An edit call will create and activate a new entity, and expire the old one
            USER_ loggedInUser;
            ACTIVE_INGREDIENT_PULA aPULA;
            try
            {
                StatusType statusType = getStatusType(status);
                DateTime? thisDate = ValidDate(date);
                if (!thisDate.HasValue || thisDate.Value.Month < DateTime.Now.Month)
                {
                    thisDate = DateTime.Now.Date;
                }
                if (entityID <= 0 || string.IsNullOrEmpty(status) || statusType == StatusType.Created)
                { return new OperationResult.BadRequest(); }

                using (EasySecureString securedPassword = GetSecuredPassword())
                {
                    using (BLTRDSEntities aBLTE = GetRDS(securedPassword))
                    {
                        //version user
                        loggedInUser = LoggedInUser(aBLTE);

                        aPULA = aBLTE.ACTIVE_INGREDIENT_PULA.FirstOrDefault(ai => ai.ID == entityID);
                        
                        if (aPULA == null)
                        { return new OperationResult.BadRequest(); }

                        //assign version
                        aPULA.VERSION_ID = SetVersion(aBLTE, aPULA.VERSION_ID, loggedInUser.USER_ID, statusType, thisDate.Value.Date).VERSION_ID;
                        
                        if (statusType == StatusType.Published)
                        {
                            aPULA.IS_PUBLISHED = 1;
                            ExpireOtherEntities(aBLTE, loggedInUser.USER_ID, aPULA, thisDate.Value.Date);
                            aBLTE.SaveChanges();
                           
                        }//end if

                        if (statusType == StatusType.Expired)
                        {
                            //set date to be 1st of current month for future query-ing
                            int nextMo = DateTime.Now.Month + 1;
                            DateTime nextMonth = Convert.ToDateTime(nextMo + "/01/" + DateTime.Now.Year);
                            ExpireEntities(aBLTE, LoggedInUser(aBLTE).USER_ID, aPULA.PULA_ID, nextMonth.Date);

                            aBLTE.SaveChanges();

                        }//end if

                        aBLTE.SaveChanges();
                        activateLinks<ACTIVE_INGREDIENT_PULA>(aPULA);

                    }//end using
                }//end using

                return new OperationResult.OK { ResponseResource = aPULA };
            }
            catch (Exception)
            { return new OperationResult.BadRequest(); }

        }//end HttpMethod.GET

        #endregion

        #region POST Methods

         [WiMRequiresRole(new string[] { AdminRole, PublisherRole, CreatorRole })]
        [HttpOperation(HttpMethod.POST)]
        public OperationResult POST(ACTIVE_INGREDIENT_PULA anEntity)
        {            
            try
            {
                using (EasySecureString securedPassword = GetSecuredPassword())
                {
                    using (BLTRDSEntities aBLTE = GetRDS(securedPassword))
                    {
                        if (!Exists(aBLTE, ref anEntity))
                        {
                            //assign pulaID
                            anEntity.PULA_ID = GetNextID(aBLTE);

                            //assign version
                            anEntity.VERSION_ID = SetVersion(aBLTE, anEntity.VERSION_ID, LoggedInUser(aBLTE).USER_ID, StatusType.Created, DateTime.Today).VERSION_ID;

                            aBLTE.ACTIVE_INGREDIENT_PULA.AddObject(anEntity);
                            aBLTE.SaveChanges();
                        }//end if
                                            
                    }//end using
                }//end using

                return new OperationResult.OK { ResponseResource = anEntity };
            }
            catch (Exception)
            { return new OperationResult.BadRequest(); }

        }//end HttpMethod.GET

        #endregion

        #region PUT Methods
        /// 
        /// Force the user to provide authentication and authorization 
        /// 
        [WiMRequiresRole(new string[] { AdminRole, PublisherRole, CreatorRole })]
        [HttpOperation(HttpMethod.PUT)]
        public OperationResult Put(Int32 entityID, ACTIVE_INGREDIENT_PULA anEntity)
        {
            ACTIVE_INGREDIENT_PULA aPULA;

            //Return BadRequest if missing required fields
            if ((entityID <= 0||anEntity.PULA_SHAPE_ID <= 0))
                 return new OperationResult.BadRequest();
            try
            {
                //Get basic authentication password
                using (EasySecureString securedPassword = GetSecuredPassword())
                {
                    using (BLTRDSEntities aBLTE = GetRDS(securedPassword))
                    {
                        aPULA = aBLTE.ACTIVE_INGREDIENT_PULA.FirstOrDefault(aip => aip.ID == entityID);
                        if (aPULA == null)
                        { return new OperationResult.BadRequest(); }

                        //NOTE: ShapeID can not be changed
                        if (aPULA.VERSION.PUBLISHED_TIME_STAMP.HasValue)
                        {
                            //can not edit a published entity. Create new
                            //assign next pulaID
                            anEntity.PULA_ID = GetNextID(aBLTE);

                            //assign version
                            anEntity.VERSION_ID = SetVersion(aBLTE, anEntity.VERSION_ID, LoggedInUser(aBLTE).USER_ID, StatusType.Published, DateTime.Now.Date).VERSION_ID;

                            anEntity.IS_PUBLISHED = 1;
                            aBLTE.ACTIVE_INGREDIENT_PULA.AddObject(anEntity);

                            //expire originals
                            ExpireOtherEntities(aBLTE, LoggedInUser(aBLTE).USER_ID, anEntity, DateTime.Now.Date);

                            aPULA = anEntity;
                        }
                        else
                        {
                            //Update fields
                            aPULA.PULA_ID = anEntity.PULA_ID;
                            aPULA.PULA_SHAPE_ID = anEntity.PULA_SHAPE_ID;
                            aPULA.OTHER_JUSTIFICATION = anEntity.OTHER_JUSTIFICATION;
                            aPULA.BASE_DATA = anEntity.BASE_DATA;
                            aPULA.BASE_DATA_MODIFIERS = anEntity.BASE_DATA_MODIFIERS;
                            aPULA.ADDITIONAL_INFORMATION = anEntity.ADDITIONAL_INFORMATION;
                            aPULA.INTERIM_PROPOSED_DECISION = anEntity.INTERIM_PROPOSED_DECISION;
                            aPULA.FOCUS_MEETING = anEntity.FOCUS_MEETING;
                            aPULA.BIOLOGICAL_OPINION_REGREVIEW = anEntity.BIOLOGICAL_OPINION_REGREVIEW;
                            aPULA.BIOLOGICAL_OPINION_LIT = anEntity.BIOLOGICAL_OPINION_LIT;
                            aPULA.PROPOSED_DECISION = anEntity.PROPOSED_DECISION;
                            aPULA.VERSION_ID = anEntity.VERSION_ID;
                            aPULA.ID = anEntity.ID;
                            aPULA.IS_PUBLISHED = anEntity.IS_PUBLISHED;
                            aPULA.EFFECTIVE_DATE = anEntity.EFFECTIVE_DATE;
                            aPULA.EVENT_ID = anEntity.EVENT_ID;
                            aPULA.COMMENTS = anEntity.COMMENTS;
                            aPULA.SEC3_NEWCHEM = anEntity.SEC3_NEWCHEM;
                            aPULA.SEC3_NEWUSE = anEntity.SEC3_NEWUSE;
                            aPULA.SEC24 = anEntity.SEC24;
                            aPULA.FINAL_DECISION = anEntity.FINAL_DECISION;
                            aPULA.SEC18 = anEntity.SEC18;

                        }// end if

                        aBLTE.SaveChanges();

                        activateLinks<ACTIVE_INGREDIENT_PULA>(aPULA);

                    }//end using
                }//end using

                return new OperationResult.OK { ResponseResource = aPULA };

            }
            catch (Exception)
            { return new OperationResult.BadRequest(); }
        }//end HttpMethod.PUT

        [RequiresAuthentication]
        [HttpOperation(HttpMethod.PUT, ForUriName = "AddComments")]
        public OperationResult PUT(Int32 pulaID, ACTIVE_INGREDIENT_PULA anAIPULA)
        {
            CONTRIBUTORPULAVIEW aContributorPULA;

            //Return BadRequest if missing required fields
            if (pulaID <= 0)
                return new OperationResult.BadRequest();
            try
            {
                //Get basic authentication password

                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    aContributorPULA = aBLTE.CONTRIBUTORPULAVIEWs.FirstOrDefault(aip => aip.ID == pulaID);

                    if (aContributorPULA == null)
                    { return new OperationResult.BadRequest(); }

                    if (aContributorPULA.COMMENTS != null)
                    { aContributorPULA.COMMENTS += anAIPULA.COMMENTS; }
                    else
                    { aContributorPULA.COMMENTS = anAIPULA.COMMENTS; }

                    aBLTE.SaveChanges();

                    activateLinks<CONTRIBUTORPULAVIEW>(aContributorPULA);

                }//end using

                return new OperationResult.OK { };
            }
            catch (Exception)
            { return new OperationResult.BadRequest(); }
        }//end HttpMethod.PUT
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

                    ACTIVE_INGREDIENT_PULA ObjectToBeDelete = aBLTE.ACTIVE_INGREDIENT_PULA.FirstOrDefault(aip => aip.ID == entityID);
                    
                    if (ObjectToBeDelete == null)
                    { return new OperationResult.BadRequest(); }

                    //NOTE: ShapeID can not be changed
                    if (ObjectToBeDelete.VERSION.PUBLISHED_TIME_STAMP.HasValue)
                    {
                        //set the date to be first of following month 
                        int nextMo = DateTime.Now.Month + 1;
                        DateTime nextMonth = Convert.ToDateTime(nextMo + "/01/" + DateTime.Now.Year);
                        ObjectToBeDelete.VERSION_ID = SetVersion(ObjectToBeDelete.VERSION_ID, LoggedInUser(aBLTE).USER_ID, StatusType.Expired, nextMonth.Date).VERSION_ID;
                    }
                    else
                    {
                        aBLTE.ACTIVE_INGREDIENT_PULA.DeleteObject(ObjectToBeDelete);
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
        private bool Exists(BLTRDSEntities aBLTE, ref ACTIVE_INGREDIENT_PULA anEntity)
        {
            ACTIVE_INGREDIENT_PULA existingEntity;
            ACTIVE_INGREDIENT_PULA thisEntity = anEntity;
            //check if it exists
            try
            {
                existingEntity = aBLTE.ACTIVE_INGREDIENT_PULA.FirstOrDefault(mt => mt.PULA_SHAPE_ID == thisEntity.PULA_SHAPE_ID &&
                                                                             mt.VERSION.CREATED_TIME_STAMP.HasValue & mt.VERSION.PUBLISHED_TIME_STAMP.HasValue &
                                                                             !mt.VERSION.EXPIRED_TIME_STAMP.HasValue);

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
            if (aBLTE.ACTIVE_INGREDIENT_PULA.Count() > 0)
            {
                nextID = aBLTE.ACTIVE_INGREDIENT_PULA.OrderByDescending(p => p.PULA_ID).First().PULA_ID + 1;
            }
            return nextID;            
        }

        private void ExpireOtherEntities(BLTRDSEntities aBLTE, decimal userId, ACTIVE_INGREDIENT_PULA pula, DateTime dt)
        {
            //get all published AIPULAS, should only be 1
            List<ACTIVE_INGREDIENT_PULA> aiPulaList = aBLTE.ACTIVE_INGREDIENT_PULA.Where(p => p.PULA_ID == pula.PULA_ID).ToList();
           
            if (aiPulaList == null) return;

            foreach (var p in aiPulaList)
            {
                if (!p.Equals(pula))
                {
                    p.VERSION = SetVersion(aBLTE, p.VERSION_ID, userId, StatusType.Expired, dt);
                }//end if
            }//next
        }//end ExpireOtherEntities
       
        protected override void ExpireEntities(BLTRDSEntities aBLTE, decimal userId, decimal Id, DateTime dt)
        {
            //get all published AIPULAS, should only be 1
            List<ACTIVE_INGREDIENT_PULA> aiPulaList = aBLTE.ACTIVE_INGREDIENT_PULA.Where(p => p.PULA_ID == Id).ToList();

            if (aiPulaList == null) return;

            foreach (var p in aiPulaList)
            {
                if (!p.VERSION.EXPIRED_TIME_STAMP.HasValue)
                {
                    p.VERSION = SetVersion(aBLTE, p.VERSION_ID, userId, StatusType.Expired, dt);
                }//end if
            }//next
        }
        #endregion
        
    }//end CollectionMethodsHandler

}//end namespace