//------------------------------------------------------------------------------
//----- RequiresRoleInterceptor ---------------------------------------------
//------------------------------------------------------------------------------

//-------1---------2---------3---------4---------5---------6---------7---------8
//       01234567890123456789012345678901234567890123456789012345678901234567890
//-------+---------+---------+---------+---------+---------+---------+---------+

// copyright:   2012 WiM - USGS

//    authors:  Jeremy K. Newson USGS Wisconsin Internet Mapping
//              
//  
//   purpose:   Intercepts role authorization using dependancy injection
//
//discussion:   
//
//     

#region Comments
// 7.03.12 - jkn - Created, adapeted from openRasta.Security.RequiresRoleInterceptor

#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using OpenRasta.OperationModel;
using OpenRasta.OperationModel.Interceptors;
using OpenRasta.Web;


namespace BLTServices.Authentication
{
    public class WiMRequiresRoleInterceptor  : OperationInterceptor
    {
        
        #region Constructors
        
        public WiMRequiresRoleInterceptor(ICommunicationContext context)
        {
            _context = context;
        }

        #endregion

        #region Properties & Fields
        ICommunicationContext _context;

        public List<string> Roles { get; set; }

        #endregion

        #region Method

        public override bool BeforeExecute(IOperation operation)
        {
            Boolean isAuthorized = false;
            foreach (string role in Roles)
            {
                //one role is all that is needed
                if (role == null || _context.User.IsInRole(role))
                {
                    isAuthorized = true;
                    break;
                }
                
            }//next

           
            if (!isAuthorized)  _context.OperationResult = new OperationResult.Unauthorized();
            
            return isAuthorized;
        }

        #endregion

    }//end class RequiresRolesInterceptor
}//end namespace