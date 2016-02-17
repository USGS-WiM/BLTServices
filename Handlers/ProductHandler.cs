//------------------------------------------------------------------------------
//----- ProductHandler ---------------------------------------------------------
//------------------------------------------------------------------------------

//-------1---------2---------3---------4---------5---------6---------7---------8
//       01234567890123456789012345678901234567890123456789012345678901234567890
//-------+---------+---------+---------+---------+---------+---------+---------+

// copyright:   2013 WiM - USGS

//    authors:  Tonia Roddick USGS Wisconsin Internet Mapping
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
// 05.29.13 - TR - Created
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
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using System.Runtime.InteropServices;

namespace BLTServices.Handlers
{
    public class ProductHandler:HandlerBase
    {
        #region Properties
        public override string entityName
        {
            get { return "PRODUCT"; }
        }
        #endregion
        #region Routed Methods
        #region GetMethods
        //---------------------Returns List of objects---------------------
        // returns all PRODUCT
        //[WiMRequiresRole(new string[] { AdminRole, PublisherRole, CreatorRole })]
        [HttpOperation(HttpMethod.GET)]
        public OperationResult Get()
        {
            List<PRODUCT> productList;
            try
            {
                //using (EasySecureString securedPassword = GetSecuredPassword())
                //{
                    using (BLTRDSEntities aBLTE = GetRDS())
                    {
                          productList =  GetEntities<PRODUCT>(aBLTE).ToList();
                    }//end using
                //}//end using

                activateLinks<PRODUCT>(productList);

                return new OperationResult.OK { ResponseResource = productList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

        //[RequiresAuthentication]
        [HttpOperation(HttpMethod.GET, ForUriName = "GetVersionedProducts")]
        public OperationResult GetVersionedProducts(string status, string date)
        {
            ObjectQuery<PRODUCT> productQuery;
            List<PRODUCT> products;
            try
            {
                StatusType statustype = getStatusType(status);
                DateTime? thisDate = ValidDate(date);
                if (!thisDate.HasValue)
                    thisDate = DateTime.Now.Date;

                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    productQuery = GetEntities<PRODUCT>(aBLTE);
                    switch (statustype)
                    {
                        case (StatusType.Published):
                            productQuery.Where(ai => ai.VERSION.PUBLISHED_TIME_STAMP != null);
                            break;
                        case (StatusType.Reviewed):
                            productQuery.Where(ai => ai.VERSION.REVIEWED_TIME_STAMP != null &&
                                            ai.VERSION.PUBLISHED_TIME_STAMP == null);
                            break;
                        //created
                        default:
                            productQuery.Where(ai => ai.VERSION.REVIEWED_TIME_STAMP == null &&
                                            ai.VERSION.PUBLISHED_TIME_STAMP == null);
                            break;
                    }//end switch

                    productQuery.Where(ai => !ai.VERSION.EXPIRED_TIME_STAMP.HasValue ||
                                        ai.VERSION.EXPIRED_TIME_STAMP < thisDate.Value.Date);

                    products = productQuery.ToList();

                }//end using

                activateLinks<PRODUCT>(products);

                return new OperationResult.OK { ResponseResource = products };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET
        
        // returns all active PRODUCT
        [HttpOperation(HttpMethod.GET)]
        public OperationResult Get(string date)
        {
            List<PRODUCT> aProductList = null;
            //ProductList aProductList = new ProductList();
            try
            {
                DateTime? thisDate = ValidDate(date);
                if (!thisDate.HasValue)
                    thisDate = DateTime.Now.Date; 

                //set the date to current date if request is not authorized
                if (DateTime.Compare(thisDate.Value.Date, DateTime.Now.Date) < 0 && !CanManage()) thisDate = DateTime.Now.Date;
                
                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    //just return the first 50 for now..takes too long
                    //aProductList = aBLTE.PRODUCT.Where(a => a.PRODUCT_NAME.StartsWith("ZOECON")).ToList();
                    aProductList = GetActive(GetEntities<PRODUCT>(aBLTE), thisDate.Value.Date).OrderBy(a=>a.PRODUCT_NAME).ToList();
                }//end using                

                activateLinks<PRODUCT>(aProductList);

                return new OperationResult.OK { ResponseResource = aProductList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }

        }//end HttpMethod.GET

        // returns all active PRODUCT
        [HttpOperation(HttpMethod.GET, ForUriName="GetJqueryProductRequest")]
        public OperationResult Get(string date, string product)
        {
            //product = first 3 letters of the Product name or it could be the first 3 numbers of a Reg Number
            List<PRODUCT> aProductList = null;
            try
            {
                DateTime? thisDate = ValidDate(date);
                if (!thisDate.HasValue)
                    thisDate = DateTime.Now.Date;

                //set the date to current date if request is not authorized
                if (DateTime.Compare(thisDate.Value.Date, DateTime.Now.Date) < 0 && !CanManage()) thisDate = DateTime.Now.Date;

                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    //see if spaces were passed in (comes through as "+")
                    if (product.Contains("+"))
                    {
                        product = product.Replace("+", " ");
                    }

                    IQueryable<PRODUCT> query;

                    query = GetEntities<PRODUCT>(aBLTE).Where(p => p.PRODUCT_NAME.StartsWith(product.ToUpper()));
                    if (query.Count() <= 0)
                    {
                        query = GetEntities<PRODUCT>(aBLTE).Where(p => p.PRODUCT_REGISTRATION_NUMBER.StartsWith(product.ToUpper()));
                    }
                    if (thisDate.HasValue)
                        query = GetActive(query, thisDate.Value.Date);

                    //process
                    query = query.OrderBy(a => a.PRODUCT_NAME);
                    aProductList = query.ToList();

                }//end using                

                activateLinks<PRODUCT>(aProductList);

                return new OperationResult.OK { ResponseResource = aProductList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }

        }//end HttpMethod.GET

       // [RequiresAuthentication]
        [HttpOperation(HttpMethod.GET, ForUriName="GetProducts")]
        public OperationResult Get(Int32 productID, [Optional] string date)
        {
            try
            {
                List<PRODUCT> productList;
                DateTime? thisDate = ValidDate(date);
                if (!thisDate.HasValue)
                    thisDate = DateTime.Now.Date;

                //set the date to current date if request is not authorized
                if (DateTime.Compare(thisDate.Value.Date, DateTime.Now.Date) < 0 && !CanManage()) thisDate = DateTime.Now.Date;

                if (productID < 0)
                { return new OperationResult.BadRequest(); }

                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    IQueryable<PRODUCT> query;
                    query = GetEntities<PRODUCT>(aBLTE).Where(ai => ai.PRODUCT_ID == productID);

                    if (thisDate.HasValue)
                        query = GetActive(query, thisDate.Value.Date);

                    //process
                    productList = query.ToList();
                }//end using
              
                activateLinks<PRODUCT>(productList);

                return new OperationResult.OK { ResponseResource = productList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

        // returns active Active Ingredients for the given pulaID
        [HttpOperation(HttpMethod.GET, ForUriName = "GetActiveIngredientProduct")]
        public OperationResult GetActiveIngredientProduct(Int32 activeIngredientID, [Optional]string date)
        {
            try
            {
                DateTime? thisDate = ValidDate(date);
                if (!thisDate.HasValue)
                    thisDate = DateTime.Now.Date;

                //set the date to current date if request is not authorized
                if (DateTime.Compare(thisDate.Value.Date, DateTime.Now.Date) < 0 && !CanManage()) thisDate = DateTime.Now.Date;

                List<PRODUCT> productList;
                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    IQueryable<PRODUCT> query1 =
                            (from ProductAI in GetActive(aBLTE.PRODUCT_ACTIVE_INGREDIENT.Where(p => p.ACTIVE_INGREDIENT_ID == activeIngredientID), thisDate.Value.Date)
                             join p in aBLTE.PRODUCT
                             on ProductAI.PRODUCT_ID equals p.PRODUCT_ID 
                             select p).Distinct();

                    //only return published
                    productList = GetActive(query1, thisDate.Value.Date).ToList();
                    
                    activateLinks<PRODUCT>(productList);

                }//end using

                return new OperationResult.OK { ResponseResource = productList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET


        // returns active Active Ingredients for the given pulaID
        [HttpOperation(HttpMethod.GET, ForUriName = "GetPULALimitationsProduct")]
        public OperationResult GetPULALimitationsCropUse(Int32 pulaLimitatationsID, [Optional]string date)
        {
            try
            {
                DateTime? thisDate = ValidDate(date);
                if (!thisDate.HasValue)
                    thisDate = DateTime.Now.Date;

                //set the date to current date if request is not authorized
                if (DateTime.Compare(thisDate.Value.Date, DateTime.Now.Date) < 0 && !CanManage()) thisDate = DateTime.Now.Date;

                List<PRODUCT> productList;
                using (BLTRDSEntities aBLTE = GetRDS())
                {

                    IQueryable<PRODUCT> query1 =
                            (from PULALimit in GetActive(aBLTE.PULA_LIMITATIONS.Where(p => p.PULA_LIMITATION_ID == pulaLimitatationsID), thisDate.Value.Date)
                             join cu in aBLTE.PRODUCT
                             on PULALimit.PRODUCT_ID equals cu.PRODUCT_ID
                             select cu).Distinct();

                    productList = GetActive(query1, thisDate.Value.Date).ToList();

                    activateLinks<PRODUCT>(productList);

                }//end using

                return new OperationResult.OK { ResponseResource = productList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

        //---------------------Returns individual objects---------------------
 //       [WiMRequiresRole(new string[] { AdminRole, PublisherRole, CreatorRole })]
        [HttpOperation(HttpMethod.GET, ForUriName="GetEntity")]
        public OperationResult GetEntity(Int32 entityID)
        {
            PRODUCT aProduct;
            try
            {                
                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    aProduct = GetEntities<PRODUCT>(aBLTE).SingleOrDefault(c => c.ID == entityID);

                }//end using
               
                activateLinks<PRODUCT>(aProduct);

                return new OperationResult.OK { ResponseResource = aProduct };
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
        public OperationResult POST(PRODUCT anEntity)
        {
            anEntity.PRODUCT_NAME = anEntity.PRODUCT_NAME.ToUpper();

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
                           
                            anEntity.PRODUCT_ID = GetNextID(aBLTE);

                            aBLTE.PRODUCT.AddObject(anEntity);

                            aBLTE.SaveChanges();
                        }//end if

                        activateLinks<PRODUCT>(anEntity); 
                    }//end using
                }//end using

                return new OperationResult.OK { ResponseResource = anEntity };
            }
            catch (Exception)
            { return new OperationResult.BadRequest(); }

        }//end HttpMethod.GET

        [WiMRequiresRole(new string[] { AdminRole, PublisherRole, CreatorRole })]
        [HttpOperation(HttpMethod.POST, ForUriName = "addProductToAI")]
        public OperationResult AddProductToAI(Int32 entityID, ACTIVE_INGREDIENT AI)
        {
            PRODUCT aProduct = null;
            //ACTIVE_INGREDIENT aAIngr = null;
            PRODUCT_ACTIVE_INGREDIENT newPAI = null;
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
                        aProduct = GetEntities<PRODUCT>(aBLTE).FirstOrDefault(c => c.PRODUCT_ID == entityID);
                       
                        if (aProduct == null || AI == null)
                        { return new OperationResult.BadRequest(); }

                        newPAI = new PRODUCT_ACTIVE_INGREDIENT();
                        newPAI.PRODUCT_ID = aProduct.PRODUCT_ID;
                        newPAI.ACTIVE_INGREDIENT_ID = AI.ACTIVE_INGREDIENT_ID;

                        //check if this relationship already exists
                        if (!Exists1(aBLTE, ref newPAI))
                        {
                            newPAI.PRODUCT_ACTIVE_INGREDIENT_ID = GetNextPAI_ID(aBLTE);

                            //create version
                            newPAI.VERSION_ID = SetVersion(aBLTE, newPAI.VERSION_ID, LoggedInUser(aBLTE).USER_ID, StatusType.Published, DateTime.Now.Date).VERSION_ID;
                           
                            aBLTE.PRODUCT_ACTIVE_INGREDIENT.AddObject(newPAI);
                            aBLTE.SaveChanges();                        
                  
                        }//end if

                    }//end using
                }//end using

                return new OperationResult.OK ();
            }
            catch (Exception)
            { return new OperationResult.BadRequest(); }


        }//end HttpMethod.GET

        #endregion
        
        #region PUT/EDIT Methods

        [WiMRequiresRole(new string[] { AdminRole, PublisherRole})]
        [HttpOperation(HttpMethod.PUT)]
        public OperationResult Put(Int32 entityID, PRODUCT anEntity)
        {
            //No editing of tables are allowed. An edit call will create and activate a new entity, and expire the old one
            USER_ loggedInUser;
            PRODUCT aProduct;
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

                        aProduct = aBLTE.PRODUCT.FirstOrDefault(c => c.ID == entityID);

                        if (aProduct == null)
                        { return new OperationResult.BadRequest(); }

                        if (aProduct.VERSION.PUBLISHED_TIME_STAMP.HasValue)
                        {
                            //can not edit a published entity. Create new
                            //assign next pulaID
                            anEntity.PRODUCT_ID = aProduct.PRODUCT_ID;
                            //assign version
                            anEntity.VERSION_ID = SetVersion(aBLTE, anEntity.VERSION_ID, loggedInUser.USER_ID, StatusType.Published, DateTime.Now.Date).VERSION_ID;
                            aBLTE.PRODUCT.AddObject(anEntity);
                            //expire originals
                            ExpireOtherEntities(aBLTE, LoggedInUser(aBLTE).USER_ID, anEntity, DateTime.Now.Date);
                            aProduct = anEntity;
                        }
                        else
                        {
                            aProduct.PRODUCT_NAME = anEntity.PRODUCT_NAME;
                        }//end if

                        aBLTE.SaveChanges();

                        activateLinks<PRODUCT>(anEntity);
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

                    PRODUCT ObjectToBeDelete = aBLTE.PRODUCT.FirstOrDefault(am => am.ID == entityID);

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
                        aBLTE.PRODUCT.DeleteObject(ObjectToBeDelete);
                    }//end if

                    aBLTE.SaveChanges();
                }//end using
            }//end using

            //Return object to verify persisitance
            return new OperationResult.OK { };
        }//end HttpMethod.DELETE

        [WiMRequiresRole(new string[] { AdminRole, PublisherRole })]
        [HttpOperation(HttpMethod.DELETE, ForUriName = "RemoveProductFromAI")]
        public OperationResult RemoveProductFromAI(Int32 entityID, Int32 aiEntityID)
        {
            //No deleting of tables are allowed. An delete call will expire the entity
            USER_ loggedInUser;
            PRODUCT aProd;
            PRODUCT_ACTIVE_INGREDIENT aPAI;

            try
            {
                DateTime? thisDate = DateTime.Now;

                if (entityID <= 0 || aiEntityID <= 0)
                { return new OperationResult.BadRequest { }; }

                using (EasySecureString securedPassword = GetSecuredPassword())
                {
                    using (BLTRDSEntities aBLTE = GetRDS(securedPassword))
                    {
                        loggedInUser = LoggedInUser(aBLTE);
                        aProd = aBLTE.PRODUCT.FirstOrDefault(c => c.PRODUCT_ID == entityID);
                        
                        aPAI = aBLTE.PRODUCT_ACTIVE_INGREDIENT.FirstOrDefault(c => c.PRODUCT_ID == aProd.PRODUCT_ID && c.ACTIVE_INGREDIENT_ID == aiEntityID);

                        if (aPAI == null)
                        { return new OperationResult.BadRequest(); }
                         //NOTE: ShapeID can not be changed
                        if (aPAI.VERSION.PUBLISHED_TIME_STAMP.HasValue)
                        {
                            //expire the Product-AI relationship
                            aPAI.VERSION = SetVersion(aBLTE, aPAI.VERSION_ID, LoggedInUser(aBLTE).USER_ID, StatusType.Expired, thisDate.Value.Date);
                        }
                        else
                        {
                            aBLTE.PRODUCT_ACTIVE_INGREDIENT.DeleteObject(aPAI);
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
        private bool Exists(BLTRDSEntities aBLTE, ref PRODUCT anEntity)
        {
            PRODUCT existingEntity;
            PRODUCT thisEntity = anEntity;
            //check if it exists
            try
            {
                existingEntity = aBLTE.PRODUCT.FirstOrDefault(mt => string.Equals(mt.PRODUCT_NAME.ToUpper(), thisEntity.PRODUCT_NAME.ToUpper()) &&
                                                                         string.Equals(mt.PRODUCT_REGISTRATION_NUMBER,thisEntity.PRODUCT_REGISTRATION_NUMBER));
        

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

        private bool Exists1(BLTRDSEntities aBLTE, ref PRODUCT_ACTIVE_INGREDIENT anEntity)
        {
            PRODUCT_ACTIVE_INGREDIENT existingEntity;
            PRODUCT_ACTIVE_INGREDIENT thisEntity = anEntity;
            //check if it exists
            try
            {
                existingEntity = aBLTE.PRODUCT_ACTIVE_INGREDIENT.FirstOrDefault(pai => pai.ACTIVE_INGREDIENT_ID == thisEntity.ACTIVE_INGREDIENT_ID && pai.PRODUCT_ID == thisEntity.PRODUCT_ID);


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
            if (aBLTE.PRODUCT.Count() > 0)
                nextID = aBLTE.PRODUCT.OrderByDescending(p => p.PRODUCT_ID).First().PRODUCT_ID + 1;

            return nextID;
        }

        private decimal GetNextPAI_ID(BLTRDSEntities aBLTE)
        {
            //create pulaID
            Decimal nextID = 1;
            if (aBLTE.PRODUCT_ACTIVE_INGREDIENT.Count() > 0)
                nextID = aBLTE.PRODUCT_ACTIVE_INGREDIENT.OrderByDescending(p => p.PRODUCT_ACTIVE_INGREDIENT_ID).First().PRODUCT_ACTIVE_INGREDIENT_ID + 1;

            return nextID;
        }

        private void ExpireOtherEntities(BLTRDSEntities aBLTE, decimal userId, PRODUCT product, DateTime dt)
        {
            //get all published, should only be 1
            List<PRODUCT> aiList = aBLTE.PRODUCT.Where(p => p.PRODUCT_ID == product.PRODUCT_ID &&
                                                        p.VERSION.PUBLISHED_TIME_STAMP <= dt.Date).ToList();
            if (aiList == null) return;

            foreach (var p in aiList)
            {
                if (!p.Equals(product))
                    p.VERSION = SetVersion(aBLTE, p.VERSION_ID, userId, StatusType.Expired, dt.Date);
            }//next
        }//end ExpireOtherEntities
        protected override void ExpireEntities(BLTRDSEntities aBLTE, decimal userId, decimal Id, DateTime dt)
        {
            //get all published, should only be 1
            List<PRODUCT> aiList = aBLTE.PRODUCT.Where(p => p.PRODUCT_ID == Id &&
                                                        p.VERSION.PUBLISHED_TIME_STAMP <= dt).ToList();
            if (aiList == null) return;

            foreach (var p in aiList)
            {
                p.VERSION = SetVersion(aBLTE, p.VERSION_ID, userId, StatusType.Expired, dt.Date);
            }//next
        }
        #endregion


    }//end class CropUseHandler
}// end namespace