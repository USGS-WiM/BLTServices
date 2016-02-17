//------------------------------------------------------------------------------
//----- Configuration -----------------------------------------------------------
//------------------------------------------------------------------------------

//-------1---------2---------3---------4---------5---------6---------7---------8
//       01234567890123456789012345678901234567890123456789012345678901234567890
//-------+---------+---------+---------+---------+---------+---------+---------+

// copyright:   2012 WiM - USGS

//    authors:  Jonathan Baier
//              Jeremy Newson          
//  
//   purpose:   Configuration implements the IConfiurationSource interface. OpenRasta
//              will call the Configure method and use it to configure the application 
//              through a fluent interface using the Resource space as root objects. 
//
//discussion:   The ResourceSpace is where you can define the resources in the application and what
//              handles them and how thy are represented. 
//              https://github.com/openrasta/openrasta/wiki/Configuration
//
//     
#region Comments
// 05.15.13 - JKN- Implement remaining resources
// 11.09.12 - TR - Added PULA and PULALimitations Resources
// 10.10.12 - JB - Created
#endregion                          
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Objects.DataClasses;
using System.ServiceModel.Syndication;

using OpenRasta.Authentication;
using OpenRasta.Authentication.Basic;
using OpenRasta.Codecs;
using OpenRasta.Codecs.WebForms;
using OpenRasta.Configuration;
using OpenRasta.DI;
using OpenRasta.IO;
using OpenRasta.Pipeline.Contributors;
using OpenRasta.Web.UriDecorators;

using BLTServices.Authentication;
using BLTServices.Resources;
using BLTServices.Handlers;
using BLTServices.Codecs;


namespace BLTServices
{
    public class Configuration:IConfigurationSource
    {
        public void Configure()
        {
            using (OpenRastaConfiguration.Manual)
            {

                // specify the authentication scheme you want to use, you can register multiple ones
                ResourceSpace.Uses.CustomDependency<IAuthenticationScheme, BasicAuthenticationScheme>(DependencyLifetime.Singleton);

                // register your basic authenticator in the DI resolver
                ResourceSpace.Uses.CustomDependency<IBasicAuthenticator, BLTBasicAuthentication>(DependencyLifetime.Transient);

                // Allow codec choice by extension 
                ResourceSpace.Uses.UriDecorator<ContentTypeExtensionUriDecorator>();

                AddAUTHENTICATION_Resources();
                AddACTIVE_INGREDIENT_Resources();
                AddACTIVE_INGREDIENT_PULA_Resources();
                AddAI_CLASS_Resources();
                AddAPPLICATION_METHOD_Resources();
                AddCROP_USE_Resources();
                AddDIVISION_Resources();
                AddEVENT_Resources();
                AddFORMULATION_Resources();
                AddLIMITATION_Resources();
                AddORGANIZATION_Resources();
                AddPRODUCT_Resources();
                AddPULA_LIMITATIONS_Resources();
                AddROLE_Resources();                
                AddSPECIES_Resources();
                AddUSER_Resources();
                AddVERSION_Resources();

            }//End using OpenRastaConfiguration.Manual

        }//End Configure()

        #region Helper methods

        private void AddACTIVE_INGREDIENT_Resources()
        {
            ResourceSpace.Has.ResourcesOfType<List<ACTIVE_INGREDIENT>>()
                .AtUri("/ActiveIngredients")
                .And.AtUri("/ActiveIngredients?status={status}&date={date}").Named("GetVersionedActiveIngredients")
                .And.AtUri("/ActiveIngredients?publishedDate={date}")
                .And.AtUri("/ActiveIngredients?aiID={activeIngredientID}&publishedDate={date}").Named("GetAI")
                .And.AtUri("/PULAS/{pulaID}/ActiveIngredients?publishedDate={date}").Named("GetPULAactiveIngredients") //not being used by internal app
                .HandledBy<ActiveIngredientHandler>()
                .TranscodedBy<SimpleUTF8XmlSerializerCodec>(null).ForMediaType("application/xml;q=1")
                .And.TranscodedBy<JsonDotNetCodec>(null).ForMediaType("application/json;q=0.9").ForExtension("json");


            ResourceSpace.Has.ResourcesOfType<ACTIVE_INGREDIENT>()
                .AtUri("/ActiveIngredients/{entityID}").Named("GetEntity")
                .HandledBy<ActiveIngredientHandler>()
                .TranscodedBy<SimpleUTF8XmlSerializerCodec>(null).ForMediaType("application/xml;q=1")
                .And.TranscodedBy<JsonDotNetCodec>(null).ForMediaType("application/json;q=0.9").ForExtension("json");


        }//end AddACTIVE_INGREDIENT_Resources

        private void AddACTIVE_INGREDIENT_PULA_Resources()
        {
            ResourceSpace.Has.ResourcesOfType<List<ACTIVE_INGREDIENT_PULA>>()
                .AtUri("/PULAs").Named("GetAll")
                .And.AtUri("/PULAs?status={status}&date={date}").Named("GetVersionedPulas")
                .And.AtUri("/PULAs?publishedDate={date}")
                .And.AtUri("/PULAs?pulaId={pulaId}&publishedDate={date}").Named("GetAIPULAS")
                .HandledBy<ActiveIngredientPULAHander>()
                .TranscodedBy<SimpleUTF8XmlSerializerCodec>().ForMediaType("application/xml;q=1").ForExtension("xml")
                .And.TranscodedBy<JsonDotNetCodec>(null).ForMediaType("application/json;q=0.9").ForExtension("json");

            ResourceSpace.Has.ResourcesOfType<PULAList>()
               .AtUri("/PULAs/simplePULAs?publishedDate={date}").Named("GetSimplePULAS")
               .And.AtUri("/PULAs/FilteredSimplePULAs?date={date}&aiID={activeIngredientID}&productID={productID}&eventID={eventID}").Named("GetFilteredSimplePULAs")
               .And.AtUri("/PULAs/EffectiveSimplePULAs?publishedDate={date}&aiID={activeIngredientID}&productID={productID}").Named("GetEffectiveSimplePULAs") //not sure if this is getting used
               .And.AtUri("/Events/{eventId}/PULAs").Named("GetEventPULAs")
               .HandledBy<ActiveIngredientPULAHander>()
               .TranscodedBy<SimpleUTF8XmlSerializerCodec>().ForMediaType("application/xml;q=1").ForExtension("xml")
               .And.TranscodedBy<JsonDotNetCodec>(null).ForMediaType("application/json;q=0.9").ForExtension("json");


            ResourceSpace.Has.ResourcesOfType<ACTIVE_INGREDIENT_PULA>()
                .AtUri("/PULAs/{entityID}").Named("GetEntity")
                .And.AtUri("/PULAs/POI/{shapeId}?publishedDate={date}").Named("GetShapePULAS")
                .And.AtUri("/PULAs/{entityID}/updateStatus?status={status}&statusDate={date}").Named("UpdatePulaStatus")
                .And.AtUri("/PULAs/{pulaID}/Expire&date={date}").Named("ExpirePULA")
                .And.AtUri("/PULAs/{pulaID}/AddComments").Named("AddComments")
                .HandledBy<ActiveIngredientPULAHander>()
                .TranscodedBy<SimpleUTF8XmlSerializerCodec>().ForMediaType("application/xml;q=1").ForExtension("xml")
                .And.TranscodedBy<JsonDotNetCodec>(null).ForMediaType("application/json;q=0.9").ForExtension("json");

        } //end AddACTIVE_INGREDIENT_PULA_Resources
       
        private void AddAI_CLASS_Resources()
        {
            ResourceSpace.Has.ResourcesOfType<List<AI_CLASS>>()
                .AtUri("/AIClasses")
                .And.AtUri("/AIClasses?status={status}&date={date}").Named("GetVersionedAIClasss")
                .And.AtUri("/AIClasses?publishedDate={date}")
                .And.AtUri("/AIClasses?aiClassID={aiClassID}&publishedDate={date}") //not being used by internal app
                .And.AtUri("/ActiveIngredients/{activeIngredientID}/AIClass?publisedDate={date}").Named("GetActiveIngredientClasses")
                .HandledBy<AIClassHandler>()
                .TranscodedBy<SimpleUTF8XmlSerializerCodec>()
                .And.TranscodedBy<JsonDotNetCodec>(null).ForMediaType("application/json;q=0.9").ForExtension("json");

            ResourceSpace.Has.ResourcesOfType<AI_CLASS>()
                .AtUri("/AIClasses/{entityID}").Named("GetEntity")
                .And.AtUri("/AIClasses/{entityID}/RemoveAIClassFromAI?activeIngredientID={aiEntityID}").Named("RemoveAIClassFromAI")
                .And.AtUri("/AIClasses/{entityID}/AddAIClass").Named("AddAIClassToAI")
                .HandledBy<AIClassHandler>()
                .TranscodedBy<SimpleUTF8XmlSerializerCodec>()
                .And.TranscodedBy<JsonDotNetCodec>(null).ForMediaType("application/json;q=0.9").ForExtension("json");

        }//end AddAI_CLASS_Resources

        private void AddAPPLICATION_METHOD_Resources()
        {
            ResourceSpace.Has.ResourcesOfType<List<APPLICATION_METHOD>>()
                .AtUri("/ApplicationMethods")
                .And.AtUri("/ApplicationMethods?status={status}&date={date}").Named("GetVersionedApplicationMethods")
                .And.AtUri("/ApplicationMethods?publishedDate={date}")
                .And.AtUri("/ApplicationMethods?applicationMethodID={applicationMethodID}&publishedDate={date}").Named("GetApplicationMethods")
                .And.AtUri("/PULALimitations/{pulaLimitationsID}/ApplicationMethod?publishedDate={date}").Named("GetPULALimitationsApplicationMethod")
                .HandledBy<ApplicationMethodHandler>()
                .TranscodedBy<SimpleUTF8XmlSerializerCodec>()
                .And.TranscodedBy<JsonDotNetCodec>(null).ForMediaType("application/json;q=0.9").ForExtension("json");

            ResourceSpace.Has.ResourcesOfType<APPLICATION_METHOD>()
                .AtUri("/ApplicationMethods/{entityID}").Named("GetEntity")
                .HandledBy<ApplicationMethodHandler>()
                .TranscodedBy<SimpleUTF8XmlSerializerCodec>()
                .And.TranscodedBy<JsonDotNetCodec>(null).ForMediaType("application/json;q=0.9").ForExtension("json");
                        
        }//end AddAPPLICATION_METHOD_Resources

        private void AddAUTHENTICATION_Resources()
        {
            //Authentication
            ResourceSpace.Has.ResourcesOfType<USER_>()
            .AtUri("/login")
            .HandledBy<LoginHandler>()
            .TranscodedBy<UTF8XmlSerializerCodec>();

        }//end AddAUTHENTICATION_Resources

        private void AddCROP_USE_Resources()
        {
            ResourceSpace.Has.ResourcesOfType<List<CROP_USE>>()
                .AtUri("/CropUses")
                .And.AtUri("/CropUses?status={status}&date={date}").Named("GetVersionedCropUses")
                .And.AtUri("/CropUses?publishedDate={date}")
                .And.AtUri("/CropUses?CropUseID={cropUseID}&publishedDate={date}")
                .And.AtUri("/PULALimitations/{pulaLimitationsID}/CropUse?publishedDate={date}").Named("GetPULALimitationsCropUse")
                .HandledBy<CropUseHandler>()
                .TranscodedBy<SimpleUTF8XmlSerializerCodec>()
                .And.TranscodedBy<JsonDotNetCodec>(null).ForMediaType("application/json;q=0.9").ForExtension("json");

            ResourceSpace.Has.ResourcesOfType<CROP_USE>()
                .AtUri("/CropUses/{entityID}").Named("GetEntity")
                .HandledBy<CropUseHandler>()
                .TranscodedBy<SimpleUTF8XmlSerializerCodec>()
                .And.TranscodedBy<JsonDotNetCodec>(null).ForMediaType("application/json;q=0.9").ForExtension("json");
                        
        }//end AddCROP_USE_Resources

        private void AddDIVISION_Resources()
        {
            ResourceSpace.Has.ResourcesOfType<List<DIVISION>>()
                .AtUri("/Divisions")
                .HandledBy<DivisionHandler>()
                .TranscodedBy<SimpleUTF8XmlSerializerCodec>()
                .And.TranscodedBy<JsonDotNetCodec>(null).ForMediaType("application/json;q=0.9").ForExtension("json");

            ResourceSpace.Has.ResourcesOfType<DIVISION>()
                .AtUri("/Divisions/{divisionID}")
                .HandledBy<DivisionHandler>()
                .TranscodedBy<SimpleUTF8XmlSerializerCodec>()
                .And.TranscodedBy<JsonDotNetCodec>(null).ForMediaType("application/json;q=0.9").ForExtension("json");
                        
        }//end AddDIVISION_Resources

        private void AddEVENT_Resources()
        {
            ResourceSpace.Has.ResourcesOfType<List<EVENT>>()
                .AtUri("/Events")
                .HandledBy<EventsHandler>()
                .TranscodedBy<SimpleUTF8XmlSerializerCodec>()
                .And.TranscodedBy<JsonDotNetCodec>(null).ForMediaType("application/json;q=0.9").ForExtension("json");

            ResourceSpace.Has.ResourcesOfType<EVENT>()
                .AtUri("/Events/{eventID}")
                .HandledBy<EventsHandler>()
                .TranscodedBy<SimpleUTF8XmlSerializerCodec>()
                .And.TranscodedBy<JsonDotNetCodec>(null).ForMediaType("application/json;q=0.9").ForExtension("json");

        }//end AddEVENT_Resources
       
        private void AddFORMULATION_Resources()
        {
            ResourceSpace.Has.ResourcesOfType<List<FORMULATION>>()
                .AtUri("/Formulations")
                .And.AtUri("/Formulations?status={status}&date={date}").Named("GetVersionedFormulations")
                .And.AtUri("/Formulations?publishedDate={date}")
                .And.AtUri("/Formulations?FormulationID={formulationID}&publishedDate={date}")
                .And.AtUri("/PULALimitations/{pulaLimitationsID}/Formulation?publishedDate={date}").Named("GetPULALimitationsFormulation")
                .HandledBy<FormulationHandler>()
                .TranscodedBy<SimpleUTF8XmlSerializerCodec>()
                .And.TranscodedBy<JsonDotNetCodec>(null).ForMediaType("application/json;q=0.9").ForExtension("json");

            ResourceSpace.Has.ResourcesOfType<FORMULATION>()
                .AtUri("/Formulations/{entityID}").Named("GetEntity")
                .HandledBy<FormulationHandler>()
                .TranscodedBy<SimpleUTF8XmlSerializerCodec>()
                .And.TranscodedBy<JsonDotNetCodec>(null).ForMediaType("application/json;q=0.9").ForExtension("json");

        }//end AddFORMULATION_Resources

        private void AddLIMITATION_Resources()
        {
            ResourceSpace.Has.ResourcesOfType<List<LIMITATION>>()
                .AtUri("/Limitations")
                .And.AtUri("/Limitations?status={status}&date={date}").Named("GetVersionedLimitations")
                .And.AtUri("/Limitations?publishedDate={date}")
                .And.AtUri("/Limitations/{limitationID}?publishedDate={date}").Named("GetLimitations")
                .And.AtUri("/PULALimitations/{pulaLimitationsID}/Limitation?publishedDate={date}").Named("GetPULALimitationsLimitation")
                .HandledBy<LimitationHandler>()
                .TranscodedBy<SimpleUTF8XmlSerializerCodec>()
                .And.TranscodedBy<JsonDotNetCodec>(null).ForMediaType("application/json;q=0.9").ForExtension("json");

            ResourceSpace.Has.ResourcesOfType<LIMITATION>()
                .AtUri("/Limitations/{entityID}").Named("GetEntity")
                .HandledBy<LimitationHandler>()
                .TranscodedBy<SimpleUTF8XmlSerializerCodec>()
                .And.TranscodedBy<JsonDotNetCodec>(null).ForMediaType("application/json;q=0.9").ForExtension("json");

        }//end AddLIMITATION_Resources

        private void AddORGANIZATION_Resources()
        {
            ResourceSpace.Has.ResourcesOfType<List<ORGANIZATION>>()
                .AtUri("/Organizations")
                .HandledBy<OrganizationHandler>()
                .TranscodedBy<SimpleUTF8XmlSerializerCodec>()
                .And.TranscodedBy<JsonDotNetCodec>(null).ForMediaType("application/json;q=0.9").ForExtension("json");

            ResourceSpace.Has.ResourcesOfType<ORGANIZATION>()
                .AtUri("/Organizations/{organizationID}")
                .HandledBy<OrganizationHandler>()
                .TranscodedBy<SimpleUTF8XmlSerializerCodec>()
                .And.TranscodedBy<JsonDotNetCodec>(null).ForMediaType("application/json;q=0.9").ForExtension("json");

        }//end AddORGANIZATION_Resources

        private void AddPRODUCT_Resources()
        {
            ResourceSpace.Has.ResourcesOfType<List<PRODUCT>>()
                .AtUri("/Products")
                .And.AtUri("/Products?publishedDate={date}")
                .And.AtUri("/Products?ProductID={productID}&publishedDate={date}")
                .And.AtUri("/Products?publishedDate={date}&term={product}").Named("GetJqueryProductRequest")
                .And.AtUri("/ActiveIngredients/{activeIngredientID}/Product?publishedDate={date}").Named("GetActiveIngredientProduct")
                .HandledBy<ProductHandler>()
                .TranscodedBy<SimpleUTF8XmlSerializerCodec>(null).ForMediaType("application/xml;q=1")
               .And.TranscodedBy<JsonDotNetCodec>(null).ForMediaType("application/json;q=0.9").ForExtension("json");


            ResourceSpace.Has.ResourcesOfType<PRODUCT>()
                .AtUri("/Products/{entityID}").Named("GetEntity")
                .And.AtUri("/Products/{entityID}/RemoveProductFromAI?activeIngredientID={aiEntityID}").Named("RemoveProductFromAI")
                .And.AtUri("/Products/{entityID}/AddProductToAI").Named("addProductToAI")
                .HandledBy<ProductHandler>()
                .TranscodedBy < SimpleUTF8XmlSerializerCodec> (null).ForMediaType("application/xml;q=1")
               .And.TranscodedBy<JsonDotNetCodec>(null).ForMediaType("application/json;q=0.9").ForExtension("json");

        }//end AddPRODUCT_Resources

        private void AddPULA_LIMITATIONS_Resources()
        {
            ResourceSpace.Has.ResourcesOfType<List<PULA_LIMITATIONS>>()
                .AtUri("/PULALimitations")
                .And.AtUri("/PULALimitations?publishedDate={date}")
                .And.AtUri("/PULALimitations?pulaLimitationsID={pulaLimitationsID}&publishedDate={date}")
                .And.AtUri("/PULAs/{pulaID}/PULALimitations?publishedDate={date}").Named("GetPULALimitations")                
                .HandledBy<PULALimitationsHandler>()
                .TranscodedBy<SimpleUTF8XmlSerializerCodec>().ForMediaType("application/xml;q=1").ForExtension("xml")
                .And.TranscodedBy<JsonDotNetCodec>(null).ForMediaType("application/json;q=0.9").ForExtension("json");

            ResourceSpace.Has.ResourcesOfType<PULA_LIMITATIONS>()
                .AtUri("/PULALimitation/{entityID}").Named("GetEntity")
                .And.AtUri("/PULALimitation/{entityID}/updateStatus/{status}&statusDate={date}").Named("UpdatePulaLimitationsStatus")
                .HandledBy<PULALimitationsHandler>()
                .TranscodedBy<SimpleUTF8XmlSerializerCodec>().ForMediaType("application/xml;q=1").ForExtension("xml")
                .And.TranscodedBy<JsonDotNetCodec>(null).ForMediaType("application/json;q=0.9").ForExtension("json");

            ResourceSpace.Has.ResourcesOfType<MapperLimitations>()
                .AtUri("/PULAs/{pulaID}/LimitationsForMapper?ShapeID={pulaSHPID}&EffectDate={date}").Named("GetMapperLimitations")
                .HandledBy<PULALimitationsHandler>()
                .TranscodedBy<SimpleUTF8XmlSerializerCodec>().ForMediaType("application/xml;q=1").ForExtension("xml")
                .And.TranscodedBy<JsonDotNetCodec>(null).ForMediaType("application/json;q=0.9").ForExtension("json");

        }//end AddPULA_LIMITATIONS_Resources

        private void AddROLE_Resources()
        {
            ResourceSpace.Has.ResourcesOfType<List<ROLE>>()
                .AtUri("/Roles")
                .HandledBy<RoleHandler>()
                .TranscodedBy<SimpleUTF8XmlSerializerCodec>()
                .And.TranscodedBy<JsonDotNetCodec>(null).ForMediaType("application/json;q=0.9").ForExtension("json");

            ResourceSpace.Has.ResourcesOfType<ROLE>()
                .AtUri("/Roles/{roleID}")
                .HandledBy<RoleHandler>()
                .TranscodedBy<SimpleUTF8XmlSerializerCodec>()
                .And.TranscodedBy<JsonDotNetCodec>(null).ForMediaType("application/json;q=0.9").ForExtension("json");

        }//end AddROLE_Resources

        private void AddSPECIES_Resources()
        {
            ResourceSpace.Has.ResourcesOfType<SpeciesList>()
                .AtUri("/ActiveIngredientPULA/{activeIngredientPULAID}/Species").Named("GetPULASpecies")
                .And.AtUri("/PULAs/{entityID}/AddSpeciesToPULA?publishedDate={date}").Named("AddSpeciesToPULA")
                .And.AtUri("/SimpleSpecies").Named("GetSpeciesList")
                .And.AtUri("/PULAs/{entityID}/RemoveSpeciesFromPULA?publishedDate={date}").Named("RemoveSpeciesFromPULA")                
                .HandledBy<SpeciesHandler>()
                .TranscodedBy<SimpleUTF8XmlSerializerCodec>(null).ForMediaType("application/xml;q=1")
                .And.TranscodedBy<JsonDotNetCodec>(null).ForMediaType("application/json;q=0.9").ForExtension("json");
            
            ResourceSpace.Has.ResourcesOfType<SimpleSpecies>()
                .AtUri("/Species/{speciesID}")
                .HandledBy<SpeciesHandler>()
                .TranscodedBy<SimpleUTF8XmlSerializerCodec>()
                .And.TranscodedBy<JsonDotNetCodec>(null).ForMediaType("application/json;q=0.9").ForExtension("json");
        }//end AddSPECIES_Resources
        
        private void AddUSER_Resources()
        {
            ResourceSpace.Has.ResourcesOfType<List<USER_>>()
                .AtUri("/Users")
                .And.AtUri("Versions/{versionID}/Users").Named("GetVersionUsers")
                .HandledBy<UserHandler>()
                .TranscodedBy<SimpleUTF8XmlSerializerCodec>()
                .And.TranscodedBy<JsonDotNetCodec>(null).ForMediaType("application/json;q=0.9").ForExtension("json");

            ResourceSpace.Has.ResourcesOfType<USER_>()
                .AtUri("/Users/{userID}")
                .And.AtUri("Users?username={userName}").Named("GetUserByUserName")
                .And.AtUri("/Users?username={userName}newPass={newPassword}").Named("ChangeUserPassword")
                .HandledBy<UserHandler>()
                .TranscodedBy<SimpleUTF8XmlSerializerCodec>()
                .And.TranscodedBy<JsonDotNetCodec>(null).ForMediaType("application/json;q=0.9").ForExtension("json");

        }//end AddUSER_Resources

        private void AddVERSION_Resources()
        {
            ResourceSpace.Has.ResourcesOfType<List<Version>>()
               .AtUri("/Versions")
               .And.AtUri("/Versions?publishedDate={date}")
               .HandledBy<VersionHandler>()
               .TranscodedBy<SimpleUTF8XmlSerializerCodec>(null).ForMediaType("application/xml;q=1")
               .And.TranscodedBy<JsonDotNetCodec>(null).ForMediaType("application/json;q=0.9"); 
            
            ResourceSpace.Has.ResourcesOfType<VERSION>()
                .AtUri("/Versions/{PULALimitID}")
                .And.AtUri("/Version/{entityID}").Named("GetVersion")
                .And.AtUri("/ActiveIngredients/{aiEntityID}/Version").Named("GetActiveIngredientVersion")
                .HandledBy<VersionHandler>()
                .TranscodedBy<SimpleUTF8XmlSerializerCodec>(null).ForMediaType("application/xml;q=1")
               .And.TranscodedBy<JsonDotNetCodec>(null).ForMediaType("application/json;q=0.9");

        }
        #endregion

    }//End class Configuration
}//End namespace