//------------------------------------------------------------------------------
//----- LoginHandler -----------------------------------------------------------
//------------------------------------------------------------------------------

//-------1---------2---------3---------4---------5---------6---------7---------8
//       01234567890123456789012345678901234567890123456789012345678901234567890
//-------+---------+---------+---------+---------+---------+---------+---------+

// copyright:   2012 WiM - USGS

//    authors:  Jon Baier USGS Wisconsin Internet Mapping
//              Jeremy K. Newson USGS Wisconsin Internet Mapping
//              
//  
//   purpose:   Handles Login resources through the HTTP uniform interface.
//              Equivalent to the controller in MVC.
//
//discussion:   Handlers are objects which handle all interaction with resources in 
//              this case the resources are POCO classes derived from the EF. 
//              https://github.com/openrasta/openrasta/wiki/Handlers
//
//     
#region Comments
// 10.24.12 - JB - Updated handler to return Member Object
// 05.29.12 - jkn - updated
// 02.17.12 - JB - Created
#endregion
using BLTServices.Resources;
using BLTServices.Authentication;

using OpenRasta.Authentication.Basic;
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

namespace BLTServices.Handlers
{
    public class LoginHandler : HandlerBase
    {
        #region Properties
        public override string entityName
        {
            get { return ""; }
        }

        #endregion
             #region Routed Methods

             #region GetMethods
             /// 
        /// Force the user to provide authentication 
        /// 
        [RequiresAuthentication]
        [HttpOperation(HttpMethod.GET)]
        public OperationResult Get()
        {

            USER_ aUser = null;

            //Get basic authentication password
            using (EasySecureString securedPassword = GetSecuredPassword())
            {
                using (BLTRDSEntities aBLTE = GetRDS(securedPassword))
                {
                    List<USER_> MemberList = aBLTE.USER_.AsEnumerable()
                                    .Where(u => u.USERNAME.ToUpper() == Context.User.Identity.Name.ToUpper()).ToList();

                    aUser = MemberList.First<USER_>();            

                }//end using
            }//end using
            return new OperationResult.OK { ResponseResource = aUser };

        }//end HttpMethod.GET
        #endregion
        #endregion




        protected override void ExpireEntities(BLTRDSEntities aBLTE, decimal userId, decimal Id, DateTime dt)
        {
            throw new NotImplementedException();
        }
    }//end class loginHandler

}// end namespace
