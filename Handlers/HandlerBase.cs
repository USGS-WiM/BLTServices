using System;
using System.Data;
using System.Data.EntityClient;
using System.Data.Metadata.Edm;
using System.Data.Objects;
using System.Data.Objects.DataClasses;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Serialization;
using System.Linq;
using System.Web;
using System.Text;
using System.Configuration;

using BLTServices.Authentication;
using OpenRasta.Web;
using OpenRasta.Security;

namespace BLTServices.Handlers
{
   public abstract class HandlerBase
   {
       #region Constants
       //role constants must match db table
       protected const string AdminRole = "ADMIN";
       protected const string PublisherRole = "PUBLISH";
       protected const string CreatorRole = "CREATE";
       protected const string EnforcerRole = "ENFORCE";
       protected const string Public = "PUBLIC";
       protected const string ReviewRole = "REVIEW";

       #endregion

       #region "Base Properties"
       protected String connectionString = ConfigurationManager.ConnectionStrings["BLTRDSEntities"].ConnectionString;

       // will be automatically injected by DI in OpenRasta
       public ICommunicationContext Context { get; set; }
    
       public string username
       {
           get { return Context.User.Identity.Name; }
       }

       public abstract string entityName { get; }
        #endregion
       #region Base Queries

       protected ObjectQuery<T> GetEntities<T>(BLTRDSEntities aBLTE) where T : HypermediaEntity
       {
           ObjectQuery<T> entities = null;
           //Get basic authentication password
           
                   // Get the metadata workspace from the connection.
                   MetadataWorkspace workspace = aBLTE.MetadataWorkspace;

                   // Get a collection of the entity containers.
                   ReadOnlyCollection<EntityContainer> containers =
                            workspace.GetItems<EntityContainer>(
                                               DataSpace.CSpace);

                   EntitySet es;
                   if (containers[0].TryGetEntitySetByName(entityName, true, out es))
                   {
                       string queryString =
                           @"SELECT VALUE anEntity FROM BLTRDSEntities." + es.Name + " as anEntity";

                       ObjectQuery<T> entityQuery =
                           aBLTE.CreateQuery<T>(queryString,
                               new ObjectParameter("ent", es.ElementType.Name));

                       entities = entityQuery;

                   }//end if
  
           return entities;
           //return new OperationResult.OK { ResponseResource = entities };
       }//end GetSecuredEntities

       protected IQueryable<ACTIVE_INGREDIENT_PULA> GetActive(IQueryable<ACTIVE_INGREDIENT_PULA> Obj, DateTime thisDate)
       {

           return Obj.Where(p => p.IS_PUBLISHED >= 1 &&
                                 (p.EFFECTIVE_DATE.HasValue && thisDate >= p.EFFECTIVE_DATE) &&
                                 ((!p.VERSION.EXPIRED_TIME_STAMP.HasValue) || thisDate < p.VERSION.EXPIRED_TIME_STAMP));

       }//end GetActive
       protected IQueryable<ACTIVE_INGREDIENT> GetActive(IQueryable<ACTIVE_INGREDIENT> Obj, DateTime thisDate)
       {

           return Obj.Where(p => (p.VERSION.PUBLISHED_TIME_STAMP.HasValue && thisDate >= p.VERSION.PUBLISHED_TIME_STAMP) &&
                                 ((!p.VERSION.EXPIRED_TIME_STAMP.HasValue) || thisDate < p.VERSION.EXPIRED_TIME_STAMP));

       }//end GetActive
       protected IQueryable<ACTIVE_INGREDIENT_AI_CLASS> GetActive(IQueryable<ACTIVE_INGREDIENT_AI_CLASS> Obj, DateTime thisDate)
       {

           return Obj.Where(p => (p.VERSION.PUBLISHED_TIME_STAMP.HasValue && thisDate >= p.VERSION.PUBLISHED_TIME_STAMP) &&
                                 ((!p.VERSION.EXPIRED_TIME_STAMP.HasValue) || thisDate < p.VERSION.EXPIRED_TIME_STAMP));

       }//end GetActive
       protected IQueryable<LIMITATION> GetActive(IQueryable<LIMITATION> Obj, DateTime thisDate)
       {
           return Obj.Where(p => (p.VERSION.PUBLISHED_TIME_STAMP.HasValue && thisDate >= p.VERSION.PUBLISHED_TIME_STAMP) &&
                                 ((!p.VERSION.EXPIRED_TIME_STAMP.HasValue) || thisDate < p.VERSION.EXPIRED_TIME_STAMP));

       }//end GetActive
       protected IQueryable<PULA_LIMITATIONS> GetActive(IQueryable<PULA_LIMITATIONS> Obj, DateTime thisDate)
       {

           return Obj.Where(p => (p.VERSION.PUBLISHED_TIME_STAMP.HasValue && thisDate >= p.VERSION.PUBLISHED_TIME_STAMP) &&
                                 ((!p.VERSION.EXPIRED_TIME_STAMP.HasValue) || thisDate < p.VERSION.EXPIRED_TIME_STAMP));

       }//end GetActive
       protected IQueryable<AI_CLASS> GetActive(IQueryable<AI_CLASS> Obj, DateTime thisDate)
       {

           return Obj.Where(p => (p.VERSION.PUBLISHED_TIME_STAMP.HasValue && thisDate >= p.VERSION.PUBLISHED_TIME_STAMP) &&
                                 ((!p.VERSION.EXPIRED_TIME_STAMP.HasValue) || thisDate < p.VERSION.EXPIRED_TIME_STAMP));

       }//end GetActive
       protected IQueryable<CROP_USE> GetActive(IQueryable<CROP_USE> Obj, DateTime thisDate)
       {

           return Obj.Where(p => (p.VERSION.PUBLISHED_TIME_STAMP.HasValue && thisDate >= p.VERSION.PUBLISHED_TIME_STAMP) &&
                                 ((!p.VERSION.EXPIRED_TIME_STAMP.HasValue) || thisDate < p.VERSION.EXPIRED_TIME_STAMP));

       }//end GetActive
       protected IQueryable<PRODUCT> GetActive(IQueryable<PRODUCT> Obj, DateTime thisDate)
       {

           return Obj.Where(p => (p.VERSION.PUBLISHED_TIME_STAMP.HasValue && thisDate >= p.VERSION.PUBLISHED_TIME_STAMP) &&
                                 ((!p.VERSION.EXPIRED_TIME_STAMP.HasValue) || thisDate < p.VERSION.EXPIRED_TIME_STAMP));

       }//end GetActive
       protected IQueryable<PRODUCT_ACTIVE_INGREDIENT> GetActive(IQueryable<PRODUCT_ACTIVE_INGREDIENT> Obj, DateTime thisDate)
       {

           return Obj.Where(p => (p.VERSION.PUBLISHED_TIME_STAMP.HasValue && thisDate >= p.VERSION.PUBLISHED_TIME_STAMP) &&
                                 ((!p.VERSION.EXPIRED_TIME_STAMP.HasValue) || thisDate < p.VERSION.EXPIRED_TIME_STAMP));

       }//end GetActive
       protected IQueryable<APPLICATION_METHOD> GetActive(IQueryable<APPLICATION_METHOD> Obj, DateTime thisDate)
       {

           return Obj.Where(p => (p.VERSION.PUBLISHED_TIME_STAMP.HasValue && thisDate >= p.VERSION.PUBLISHED_TIME_STAMP) &&
                                 ((!p.VERSION.EXPIRED_TIME_STAMP.HasValue) || thisDate < p.VERSION.EXPIRED_TIME_STAMP));

       }//end GetActive
       protected IQueryable<FORMULATION> GetActive(IQueryable<FORMULATION> Obj, DateTime thisDate)
       {

           return Obj.Where(p => (p.VERSION.PUBLISHED_TIME_STAMP.HasValue && thisDate >= p.VERSION.PUBLISHED_TIME_STAMP) &&
                                 ((!p.VERSION.EXPIRED_TIME_STAMP.HasValue) || thisDate < p.VERSION.EXPIRED_TIME_STAMP));

       }//end GetActive
       protected IQueryable<VERSION> GetActive(IQueryable<VERSION> Obj, DateTime thisDate)
       {

           return Obj.Where(p => (p.PUBLISHED_TIME_STAMP.HasValue && thisDate.Date >= p.PUBLISHED_TIME_STAMP) &&
                                 ((!p.EXPIRED_TIME_STAMP.HasValue) || thisDate.Date < p.EXPIRED_TIME_STAMP));

       }//end GetActive

       #endregion
       #region "Base Methods"
       protected abstract void ExpireEntities(BLTRDSEntities aBLTE, decimal userId, decimal Id, DateTime dt);
       public bool CanManage()
       {
           try
           {
               if(IsAuthorized(EnforcerRole))
                   return true;

               if (IsAuthorized(PublisherRole))
                   return true;

               if (IsAuthorized(AdminRole))
                   return true;

               if (IsAuthorized(CreatorRole))
                   return true;

               //else
               return false;
           }
           catch (Exception)
           {
               return false;
           }//end try

       }//end CanManager
       public bool IsAuthorized(string role)
       {
           try
           { 
               return Context.User.IsInRole(role);
           }
           catch (Exception)
           {

               return false;
           }//end try
          
       }//end IsAuthorized

       protected VERSION SetVersion(decimal versionId, Decimal userId, StatusType stype, DateTime timeStamp )
       {
           VERSION newVersion;
           using (EasySecureString securedPassword = GetSecuredPassword())
           {
               using (BLTRDSEntities aBLTE = GetRDS(securedPassword))
               {

                  newVersion= SetVersion(aBLTE,versionId, userId, stype, timeStamp);

               }//end using
           }//end using

           return newVersion;
       
       }//end SetVersion
       protected VERSION SetVersion(BLTRDSEntities aBLTE,Decimal versionId, Decimal userId, StatusType stype, DateTime timeStamp)
       {           
               if (versionId == null) versionId = -1;

               VERSION newVersion = aBLTE.VERSIONs.FirstOrDefault(v => v.VERSION_ID == versionId);
            
               switch (stype)
               {
                   case StatusType.Created:

                       newVersion = new VERSION();
                       newVersion.CREATOR_ID = userId;
                       newVersion.CREATED_TIME_STAMP = timeStamp.Date;

                       aBLTE.VERSIONs.AddObject(newVersion);
                       break;
                   case StatusType.Reviewed:
                       if (newVersion == null)
                           newVersion = SetVersion(aBLTE, -1, userId, StatusType.Created, DateTime.Today);

                       newVersion.REVIEWER_ID = userId;
                       newVersion.REVIEWED_TIME_STAMP = timeStamp.Date;
                       break;
                   case StatusType.Published:

                       if (newVersion == null)
                           newVersion = SetVersion(aBLTE, -1, userId, StatusType.Reviewed, DateTime.Today);
                       //do not overwrite
                       if (newVersion.PUBLISHED_TIME_STAMP.HasValue) break;
                       //else
                       newVersion.PUBLISHER_ID = userId;
                       newVersion.PUBLISHED_TIME_STAMP = timeStamp.Date;
                       break;
                   case StatusType.Expired:
                       if (newVersion.EXPIRED_TIME_STAMP.HasValue && 
                           newVersion.PUBLISHED_TIME_STAMP.HasValue ) break;

                       if (newVersion.EXPIRED_TIME_STAMP.HasValue && 
                           DateTime.Compare(newVersion.EXPIRED_TIME_STAMP.Value, timeStamp) == 0) break;

                       //else
                       newVersion.EXPIRER_ID = userId;
                       newVersion.EXPIRED_TIME_STAMP = timeStamp.Date;
                       break;
                   default:
                       break;
               }//end switch
                //save
                aBLTE.SaveChanges();
           

           return newVersion;

       }//end SetVersion

       protected USER_ LoggedInUser(BLTRDSEntities rds)
       {
           return rds.USER_.FirstOrDefault(dm => dm.USERNAME.ToUpper().Equals(username.ToUpper()));
       }//loggedInUser

       protected BLTRDSEntities GetRDS(EasySecureString password)
       {
           return new BLTRDSEntities(string.Format(connectionString, Context.User.Identity.Name, password.decryptString()));
       }

       protected BLTRDSEntities GetRDS()
       {
           return new BLTRDSEntities(string.Format(connectionString, "BLTPUBLIC", "B1TPu673sS"));
       }

       protected EasySecureString GetSecuredPassword()
       {
           //return new EasySecureString("B1TMan673sS");
           return new EasySecureString(BLTBasicAuthentication.ExtractBasicHeader(Context.Request.Headers["Authorization"]).Password);
       }

       protected DateTime? ValidDate(string date)
       {
           DateTime tempDate;
           try
           {
               if (date == null) return null;
               if (!DateTime.TryParse(date, out tempDate))
               {
                   //try oadate
                   tempDate = DateTime.FromOADate(Convert.ToDouble(date));
               }


               return tempDate;
               // 
           }
           catch (Exception)
           {

               return null;
           }

       }//end ValidDate

       protected void activateLinks<T>(T anEntity)where T:HypermediaEntity
       {
           if (anEntity != null)
               anEntity.LoadLinks(Context.ApplicationBaseUri.AbsoluteUri, linkType.e_individual);
       }//end activateLinks
       protected void activateLinks<T>(List<T> EntityList) where T: HypermediaEntity
       {
           if (EntityList != null)
               EntityList.ForEach(x=>x.LoadLinks(Context.ApplicationBaseUri.AbsoluteUri, linkType.e_group));
       }//end activateLinks

       #endregion
       protected StatusType getStatusType(string statusString)
       {
           try
           {
               return (StatusType)Enum.Parse(typeof(StatusType), statusString);
           }
           catch (Exception)
           {
               switch (statusString.ToUpper().Trim())
               {
                   case "1":
                   case "CREATED":
                       return StatusType.Created;
                   case "2":
                   case "REVIEWED":
                       return StatusType.Reviewed;
                   case "3":
                   case "PUBLISHED":
                       return StatusType.Published;
                   case "4":
                   case "EFFECTIVE":
                       return StatusType.Effective;
                   case "5":
                   case "EXPIRED":
                       return StatusType.Expired;
                   default:
                       return StatusType.Created;
               }//end switch
           }//end try
       
       }//end getStatusType
       protected enum StatusType
       {
           Created = 1,
           Reviewed = 2,
           Published = 3,
           Effective = 4,
           Expired = 5
       }//end enum
    }//end class HandlerBase

}//end namespace