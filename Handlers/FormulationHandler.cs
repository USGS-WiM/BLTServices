//------------------------------------------------------------------------------
//----- Formulation Handler -----------------------------------------------------------
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
// 07.22.13 - TR - created from ModifiersHandler

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
    public class FormulationHandler : HandlerBase
    {

        #region Properties
        public override string entityName
        {
            get { return "FORMULATION"; }
        }
        #endregion
        #region Routed Methods
        #region GetMethods
        //---------------------Returns List of objects---------------------
        // returns all FORMULATION
      //  [WiMRequiresRole(new string[] { AdminRole, PublisherRole, CreatorRole })]
        [HttpOperation(HttpMethod.GET)]
        public OperationResult Get()
        {
            List<FORMULATION> aFormulation;
            try
            {
                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    aFormulation = GetEntities<FORMULATION>(aBLTE).ToList();
                }//end using
                
                activateLinks<FORMULATION>(aFormulation);

                return new OperationResult.OK { ResponseResource = aFormulation };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

        // returns all PULAs
        //[RequiresAuthentication]
        [HttpOperation(HttpMethod.GET, ForUriName = "GetVersionedFormulations")]
        public OperationResult GetVersionedFormulations(string status, string date)
        {
            ObjectQuery<FORMULATION> formulaQuery;
            List<FORMULATION> formulaList;
            try
            {
                StatusType statustype = getStatusType(status);
                DateTime? thisDate = ValidDate(date);
                if (!thisDate.HasValue)
                    thisDate = DateTime.Now.Date;

                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    formulaQuery = GetEntities<FORMULATION>(aBLTE);
                    switch (statustype)
                    {
                        case (StatusType.Published):
                            formulaQuery.Where(ai => ai.VERSION.PUBLISHED_TIME_STAMP != null);
                            break;
                        case (StatusType.Reviewed):
                            formulaQuery.Where(ai => ai.VERSION.REVIEWED_TIME_STAMP != null &&
                                            ai.VERSION.PUBLISHED_TIME_STAMP == null);
                            break;
                        //created
                        default:
                            formulaQuery.Where(ai => ai.VERSION.REVIEWED_TIME_STAMP == null &&
                                            ai.VERSION.PUBLISHED_TIME_STAMP == null);
                            break;
                    }//end switch

                    formulaQuery.Where(ai => !ai.VERSION.EXPIRED_TIME_STAMP.HasValue ||
                                        ai.VERSION.EXPIRED_TIME_STAMP < thisDate.Value.Date);

                    formulaList = formulaQuery.ToList();

                }//end using
                
                activateLinks<FORMULATION>(formulaList);

                return new OperationResult.OK { ResponseResource = formulaList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

        // returns all active FORMULATION
        [HttpOperation(HttpMethod.GET)]
        public OperationResult Get(string date)
        {
            List<FORMULATION> aFormulationList = null;
            try
            {
                DateTime? thisDate = ValidDate(date);
                if (!thisDate.HasValue)
                    thisDate = DateTime.Now.Date;

                //set the date to current date if request is not authorized
                if (DateTime.Compare(thisDate.Value.Date, DateTime.Now.Date) < 0 && !CanManage()) thisDate = DateTime.Now.Date;

                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    aFormulationList = GetActive(GetEntities<FORMULATION>(aBLTE), thisDate.Value.Date).OrderBy(a=>a.FORM).ToList();
                }//end using

                activateLinks<FORMULATION>(aFormulationList);

                return new OperationResult.OK { ResponseResource = aFormulationList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }

        }//end HttpMethod.GET

   //     [RequiresAuthentication]
        [HttpOperation(HttpMethod.GET, ForUriName= "GetFormulations")]
        public OperationResult Get(Int32 formulationID, [Optional] string date)
        {
            try
            {
                List<FORMULATION> formulationList;
                DateTime? thisDate = ValidDate(date);

                if (formulationID < 0)
                { return new OperationResult.BadRequest(); }

                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    IQueryable<FORMULATION> query;
                    query = GetEntities<FORMULATION>(aBLTE).Where(f => f.FORMULATION_ID == formulationID);

                    if (thisDate.HasValue)
                        query = GetActive(query, thisDate.Value.Date);

                    //process
                    formulationList = query.ToList();
                }//end using
                
                activateLinks<FORMULATION>(formulationList);

                return new OperationResult.OK { ResponseResource = formulationList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

        // returns active Active Ingredients for the given pulaID
        [HttpOperation(HttpMethod.GET, ForUriName = "GetPULALimitationsFormulation")]
        public OperationResult GetPULALimitationsFormulation(Int32 pulaLimitatationsID, [Optional]string date)
        {
            try
            {
                DateTime? thisDate = ValidDate(date);
                if (!thisDate.HasValue)
                    thisDate = DateTime.Now.Date;

                //set the date to current date if request is not authorized
                if (DateTime.Compare(thisDate.Value.Date, DateTime.Now.Date) < 0 && !CanManage()) thisDate = DateTime.Now.Date;

                List<FORMULATION> formulationList;
                using (BLTRDSEntities aBLTE = GetRDS())
                {

                    IQueryable<FORMULATION> query1 =
                            (from PULALimit in GetActive(aBLTE.PULA_LIMITATIONS.Where(p => p.PULA_LIMITATION_ID == pulaLimitatationsID), thisDate.Value.Date)
                             join f in aBLTE.FORMULATION
                             on PULALimit.FORMULATION_ID equals f.FORMULATION_ID
                             select f).Distinct();

                    formulationList = GetActive(query1, thisDate.Value.Date).ToList();

                    activateLinks<FORMULATION>(formulationList);

                }//end using

                return new OperationResult.OK { ResponseResource = formulationList };
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
            FORMULATION aFormulation;
            try
            {
                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    aFormulation = GetEntities<FORMULATION>(aBLTE).SingleOrDefault(c => c.ID == entityID);

                }//end using

                activateLinks<FORMULATION>(aFormulation);

                return new OperationResult.OK { ResponseResource = aFormulation };
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
        public OperationResult POST(FORMULATION anEntity)
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

                            anEntity.FORMULATION_ID = GetNextID(aBLTE);

                            aBLTE.FORMULATION.AddObject(anEntity);

                            aBLTE.SaveChanges();
                        }
                        else
                        {//it exists, check if expired
                            if (anEntity.VERSION.EXPIRED_TIME_STAMP.HasValue)
                            {
                                FORMULATION newF = new FORMULATION();
                                newF.FORM = anEntity.FORM;
                                newF.VERSION_ID = SetVersion(aBLTE, newF.VERSION_ID, LoggedInUser(aBLTE).USER_ID, StatusType.Published, DateTime.Now.Date).VERSION_ID;
                                newF.FORMULATION_ID = anEntity.FORMULATION_ID;
                                //anEntity.ID = 0;
                                aBLTE.FORMULATION.AddObject(newF);
                                aBLTE.SaveChanges();
                            }//end if
                        }//end if//end if

                        activateLinks<FORMULATION>(anEntity);
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
        public OperationResult Put(Int32 entityID, FORMULATION anEntity)
        {
            //No editing of tables are allowed. An edit call will create and activate a new entity, and expire the old one
            USER_ loggedInUser;
            FORMULATION aFormulation;
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

                        aFormulation = aBLTE.FORMULATION.FirstOrDefault(c => c.ID == entityID);
                        if (aFormulation == null)
                        { return new OperationResult.BadRequest(); }

                        if (aFormulation.VERSION.PUBLISHED_TIME_STAMP.HasValue)
                        {
                            //can not edit a published entity. Create new
                            //assign next pulaID
                            anEntity.FORMULATION_ID = aFormulation.FORMULATION_ID;
                            //assign version
                            anEntity.VERSION_ID = SetVersion(aBLTE, anEntity.VERSION_ID, loggedInUser.USER_ID, StatusType.Published, DateTime.Now.Date).VERSION_ID;
                            aBLTE.FORMULATION.AddObject(anEntity);
                            //expire originals
                            ExpireOtherEntities(aBLTE, LoggedInUser(aBLTE).USER_ID, anEntity, DateTime.Now.Date);
                            aFormulation = anEntity;
                        }
                        else
                        {
                            aFormulation.FORM = anEntity.FORM;
                        }//end if

                        aBLTE.SaveChanges();

                        activateLinks<FORMULATION>(aFormulation);
                    }//end using
                }//end using

                return new OperationResult.OK { ResponseResource = aFormulation };
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
                    FORMULATION ObjectToBeDelete = aBLTE.FORMULATION.FirstOrDefault(f => f.ID == entityID);

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
                        aBLTE.FORMULATION.DeleteObject(ObjectToBeDelete);
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
        private bool Exists(BLTRDSEntities aBLTE, ref FORMULATION anEntity)
        {
            FORMULATION existingEntity;
            FORMULATION thisEntity = anEntity;
            //check if it exists
            try
            {
                existingEntity = aBLTE.FORMULATION.FirstOrDefault(f => string.Equals(f.FORM.ToUpper(), thisEntity.FORM.ToUpper()));


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
            if (aBLTE.FORMULATION.Count() > 0)
                nextID = aBLTE.FORMULATION.OrderByDescending(p => p.FORMULATION_ID).First().FORMULATION_ID + 1;

            return nextID;
        }
        private void ExpireOtherEntities(BLTRDSEntities aBLTE, decimal userId, FORMULATION formulation, DateTime dt)
        {
            //get all published, should only be 1
            List<FORMULATION> aiList = aBLTE.FORMULATION.Where(p => p.FORMULATION_ID == formulation.FORMULATION_ID &&
                                                                                p.VERSION.PUBLISHED_TIME_STAMP <= dt.Date).ToList();
            if (aiList == null) return;

            foreach (var p in aiList)
            {
                if (!p.Equals(formulation))
                    p.VERSION = SetVersion(aBLTE, p.VERSION_ID, userId, StatusType.Expired, dt.Date);
            }//next
        }//end ExpireOtherEntities
        protected override void ExpireEntities(BLTRDSEntities aBLTE, decimal userId, decimal Id, DateTime dt)
        {
            //get all published, should only be 1
            List<FORMULATION> formulationList = aBLTE.FORMULATION.Where(p => p.FORMULATION_ID == Id &&
                                                               p.VERSION.PUBLISHED_TIME_STAMP <= dt.Date).ToList();
            if (formulationList == null) return;

            foreach (var p in formulationList)
            {
                p.VERSION = SetVersion(aBLTE, p.VERSION_ID, userId, StatusType.Expired, dt.Date);
            }//next
        }
        #endregion


    }//end class FormulationHandler

}//end namespace