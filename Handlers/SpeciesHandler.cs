//------------------------------------------------------------------------------
//----- SpeciesHandler --------------------------------------------------------
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
// 09.24.13 - TR - Reformat whole thing to hit Fish & Wildlife TESS Services (getAllSpecies, getASpecies, PostPULASpecies, RemovePULASpecies, getPULASpecies)
// 07.08.13 - JKN - Added Method for adding species to pula (/PULAs/{entityID}/AddSpeciesToPULA)
// 05.30.13 - TR - Created
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
using System.Net;
using System.IO;
using System.Reflection;
using System.Web;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace BLTServices.Handlers
{
    public class SpeciesHandler: HandlerBase
    {
        #region Properties
        public override string entityName
        {
            get { return "SPECIES"; }
        }
        #endregion
        #region Routed Methods
        #region GetMethods
                
        // returns simpleSpecies
       // [WiMRequiresRole(new string[] { AdminRole, PublisherRole, CreatorRole })]
        [HttpOperation(HttpMethod.GET, ForUriName="GetSpeciesList")]
        public OperationResult GetSpeciesList()
        {
            //SpeciesModel to hold each return to pass to View
            SpeciesList sppList = new SpeciesList();
            
            try
            {                
                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    //get species list from Service
                    sppList = GetAllSpecies(); 
                        
                    }//end using

                return new OperationResult.OK { ResponseResource = sppList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

       // [WiMRequiresRole(new string[] { AdminRole, PublisherRole, CreatorRole })]
        [HttpOperation(HttpMethod.GET)]
        public OperationResult Get(Int32 speciesID)
        {
            SpeciesList aSpp;
            try
            {                
                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    //get species list from Service
                    HttpWebRequest SppRequest = null;
                    HttpWebResponse SppResponse = null;
                    string firstPart = "http://ecos.fws.gov/tat_services/TessQuery?xquery=for+%24item+in+%2FSPECIES_DETAIL%0D%0Awhere+%0D%0A%24item%2FENTITY_ID+eq+";
                    string secondPart = "+++%0D%0Areturn+%0D%0A%3CSPECIES%3E{%24item%2FENTITY_ID%2C%24item%2FSPCODE%2C%24item%2FSCINAME%2C%24item%2FCOMNAME%2C%24item%2FSTATUS%2C%24item%2FSTATUS_TEXT}%3C%2FSPECIES%3E&request=query";
                    string url = firstPart + speciesID + secondPart;
                    //request
                    SppRequest = WebRequest.Create(url) as HttpWebRequest;
                    XmlDocument SppXmlDoc = new XmlDocument();
                    //make request
                    using (SppResponse = SppRequest.GetResponse() as HttpWebResponse)
                    {
                        SppXmlDoc.Load(SppResponse.GetResponseStream());
                    }

                    //extract info
                    var xml = XDocument.Parse(SppXmlDoc.InnerXml);

                    XmlRootAttribute xRoot = new XmlRootAttribute();
                    xRoot.ElementName = "results";
                    xRoot.IsNullable = true;
                    XmlSerializer serializer = new XmlSerializer(typeof(SpeciesList), xRoot);
                    aSpp = (SpeciesList)serializer.Deserialize(xml.Root.CreateReader());
                }//end using

                return new OperationResult.OK { ResponseResource = aSpp };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

      //  [WiMRequiresRole(new string[] { AdminRole, PublisherRole, CreatorRole, Public })]
        [HttpOperation(HttpMethod.GET, ForUriName = "GetPULASpecies")]
        public OperationResult GetPULASpecies(Int32 activeIngredientPULAID)
        {
            try
            {
                SpeciesList allSpecies = GetAllSpecies();
                List<SPECIES_ACTIVE_INGREDIENT_PULA> SppPulaList = new List<SPECIES_ACTIVE_INGREDIENT_PULA>();
                SpeciesList SppSpeciesList = new SpeciesList();

                using (BLTRDSEntities aBLTE = GetRDS())
                {
                    SppPulaList = aBLTE.SPECIES_ACTIVE_INGREDIENT_PULA.Where(p => p.PULA_ID == activeIngredientPULAID).ToList();
                    SpeciesList spL = new SpeciesList();
                    spL.SPECIES = SppPulaList.AsEnumerable().Select(sp => new SimpleSpecies()
                                                                                            {      
                                                                                                ENTITY_ID = (Int32)sp.SPECIES_ID 
                                                                                            }).ToList<SimpleSpecies>();
                        
                    //now compare these IDs to the full list to return only the PULA species
                    SppSpeciesList.SPECIES = (from s in allSpecies.SPECIES 
                                                from sp in spL.SPECIES 
                                                where s.ENTITY_ID == sp.ENTITY_ID select s).ToList<SimpleSpecies>();

                } //end using
                

                return new OperationResult.OK { ResponseResource = SppSpeciesList };
            }
            catch (Exception ex)
            {
                return new OperationResult.InternalServerError { ResponseResource = ex.InnerException.ToString() };
            }
        }//end HttpMethod.GET

        #endregion
        
        #region POST Methods
        //PostPULASpecies, RemovePULASpecies

        [WiMRequiresRole(new string[] { AdminRole, PublisherRole, CreatorRole })]
        [HttpOperation(HttpMethod.POST, ForUriName ="AddSpeciesToPULA")]
        public OperationResult AddSpeciesToPULA(Int32 entityID, SpeciesList speciesList, [Optional] string date)
        {
            USER_ loggedInUser;
            ACTIVE_INGREDIENT_PULA anAIPULA;
            SpeciesList ReturningSpp = new SpeciesList();
            try
            {
                //Return BadRequest if missing required fields
                if (speciesList.SPECIES.Count <= 0 || entityID <= 0)
                { return new OperationResult.BadRequest(); }

                using (EasySecureString securedPassword = GetSecuredPassword())
                {
                    using (BLTRDSEntities aBLTE = GetRDS(securedPassword))
                    {
                        DateTime? thisDate = ValidDate(date);
                        if (!thisDate.HasValue)
                            thisDate = DateTime.Now.Date;

                        //version user
                        loggedInUser = LoggedInUser(aBLTE);

                        //get pulaID
                        anAIPULA = aBLTE.ACTIVE_INGREDIENT_PULA.FirstOrDefault(ai => ai.PULA_ID == entityID);
                        if(anAIPULA == null)
                        { return new OperationResult.NoContent(); }
                        
                        //add species
                        foreach (SimpleSpecies ss in speciesList.SPECIES)
                        {
                            if (aBLTE.SPECIES_ACTIVE_INGREDIENT_PULA.FirstOrDefault(sai => sai.SPECIES_ID.Equals(ss.ENTITY_ID) &&
                                                                                       sai.PULA_ID.Equals(anAIPULA.PULA_ID)) == null)
                            {
                                //new species PULA
                                var aSpeciesAIPULA = new SPECIES_ACTIVE_INGREDIENT_PULA() 
                                                    { 
                                                        SPECIES_ID = ss.ENTITY_ID,
                                                        PULA_ID = anAIPULA.PULA_ID,
                                                        VERSION = SetVersion(aBLTE,-1, loggedInUser.USER_ID, StatusType.Created, DateTime.Now.Date)
                                                    };
                               aBLTE.SPECIES_ACTIVE_INGREDIENT_PULA.AddObject(aSpeciesAIPULA);
                               aBLTE.SaveChanges();
                           }
                        }//next species
                                

                        //return the list to tell user what was imported
                        SpeciesList aSppList = new SpeciesList();
                        SpeciesList allSpecies = GetAllSpecies();
                        List<SPECIES_ACTIVE_INGREDIENT_PULA> SppPulaList = aBLTE.SPECIES_ACTIVE_INGREDIENT_PULA.Where(p => p.PULA_ID == anAIPULA.PULA_ID).ToList();

                        
                        aSppList.SPECIES = SppPulaList.AsEnumerable().Select(sp => new SimpleSpecies()
                        {
                            ENTITY_ID = (Int32)sp.SPECIES_ID
                        }).ToList<SimpleSpecies>();

                        
                        //now compare these IDs to the full list to return only the PULA species
                        ReturningSpp.SPECIES = (from s in allSpecies.SPECIES
                                                  from sp in aSppList.SPECIES
                                                  where s.ENTITY_ID == sp.ENTITY_ID
                                                  select s).ToList<SimpleSpecies>();
                    }//end using
                }//end using

                return new OperationResult.OK { ResponseResource = ReturningSpp };
            }
            catch (Exception)
            { return new OperationResult.BadRequest(); }

        }//end HttpMethod.GET

        [WiMRequiresRole(new string[] { AdminRole, PublisherRole, CreatorRole })]
        [HttpOperation(HttpMethod.POST, ForUriName = "RemoveSpeciesFromPULA")]
        public OperationResult RemoveSpeciesFromPULA(Int32 entityID, SpeciesList speciesList, [Optional] string date)
        {
            USER_ loggedInUser;
            ACTIVE_INGREDIENT_PULA anAIPULA;
            try
            {
                //Return BadRequest if missing required fields
                if (speciesList.SPECIES.Count <= 0 || entityID <= 0)
                { return new OperationResult.BadRequest(); }

                DateTime? thisDate = ValidDate(date);
                if (!thisDate.HasValue)
                    thisDate = DateTime.Now.Date;

                using (EasySecureString securedPassword = GetSecuredPassword())
                {
                    using (BLTRDSEntities aBLTE = GetRDS(securedPassword))
                    {
                        //version user
                        loggedInUser = LoggedInUser(aBLTE);

                        //get pulaID
                        anAIPULA = aBLTE.ACTIVE_INGREDIENT_PULA.FirstOrDefault(ai => ai.ID == entityID);
                        if (anAIPULA == null)
                        { return new OperationResult.NoContent(); }

                        //remove species
                        foreach (SimpleSpecies ss in speciesList.SPECIES)
                        {
                            //new species PULA
                           
                            var aSpeciesAIPULA = aBLTE.SPECIES_ACTIVE_INGREDIENT_PULA.FirstOrDefault(sai=>sai.SPECIES_ID == ss.ENTITY_ID && sai.PULA_ID == anAIPULA.PULA_ID);
                           
                            if(aSpeciesAIPULA != null)
                               aBLTE.SPECIES_ACTIVE_INGREDIENT_PULA.DeleteObject(aSpeciesAIPULA);

                        }//next species

                        aBLTE.SaveChanges();

                        //return the list to tell user what was imported
                        speciesList = new SpeciesList();

                    }//end using
                }//end using

                return new OperationResult.OK();
            }
            catch (Exception)
            { return new OperationResult.BadRequest(); }

        }//end HttpMethod.GET
        
        #endregion
        
        #region PUT Methods
        //no PUT - all through public TESS Services
        #endregion

        #region DELETE Methods
        //no DELETE - TESS services
        #endregion
        #endregion Routed Methods

        #region Helper Methods
        private SpeciesList GetAllSpecies()
        {
            try
            {
                SpeciesList sppList = new SpeciesList();

                //get species list from Service
                HttpWebRequest SppRequest = null;
                HttpWebResponse SppResponse = null;

                //request
                //               http://ecos.fws.gov/tat_services/TessQuery?xquery=++for+%24item+in+%2FSPECIES_DETAIL%0D%0Awhere+%0D%0A%24item%2FSTATUS+eq+%22E%22+or%0D%0A%24item%2FSTATUS+eq+%22EXPN%22+or%0D%0A%24item%2FSTATUS+eq+%22PE%22+or%0D%0A%24item%2FSTATUS+eq+%22PEXPN%22+or%0D%0A%24item%2FSTATUS+eq+%22PT%22+or%0D%0A%24item%2FSTATUS+eq+%22T%22%0D%0Areturn+%0D%0A%3CSPECIES%3E%7B%24item%2FENTITY_ID%2C%24item%2FSCINAME%2C%24item%2FCOMNAME%2C%24item%2FSTATUS%2C%24item%2FSTATUS_TEXT%7D%3C%2FSPECIES%3E&request=query")) as HttpWebRequest;
                SppRequest = WebRequest.Create(String.Format(
                    "http://ecos.fws.gov/tat_services/TessQuery?xquery=++for+%24item+in+%2FSPECIES_DETAIL%0D%0Awhere+%0D%0A%24item%2FSTATUS+eq+%22E%22+or%0D%0A%24item%2FSTATUS+eq+%22EXPN%22+or%0D%0A%24item%2FSTATUS+eq+%22PE%22+or%0D%0A%24item%2FSTATUS+eq+%22PEXPN%22+or%0D%0A%24item%2FSTATUS+eq+%22PT%22+or%0D%0A%24item%2FSTATUS+eq+%22T%22%0D%0Areturn+%0D%0A<SPECIES>%7B%24item%2FENTITY_ID%2C%24item%2FSCINAME%2C%24item%2FCOMNAME%2C%24item%2FSTATUS%2C%24item%2FSPCODE%2C%24item%2FSTATUS_TEXT%7D<%2FSPECIES>&request=query")) as HttpWebRequest;
     
                XmlDocument SppXmlDoc = new XmlDocument();

                //make request
                using (SppResponse = SppRequest.GetResponse() as HttpWebResponse)
                {
                    SppXmlDoc.Load(SppResponse.GetResponseStream());
                }
                        
                //extract info
                var xml = XDocument.Parse(SppXmlDoc.InnerXml);
                        
                XmlRootAttribute xRoot = new XmlRootAttribute();
                xRoot.ElementName = "results";
                xRoot.IsNullable = true;
                XmlSerializer serializer = new XmlSerializer(typeof(SpeciesList),xRoot);
                return sppList = (SpeciesList) serializer.Deserialize(xml.Root.CreateReader());

            }
            catch (Exception)
            {
                SpeciesList sp = new SpeciesList();
                return sp;
            }
        }

        #endregion

        protected override void ExpireEntities(BLTRDSEntities aBLTE, decimal userId, decimal Id, DateTime dt)
        {
            throw new NotImplementedException();
        }
    }// end class divisionHandler
}//end namespace