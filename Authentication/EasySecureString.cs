#region Comments
// 02.17.12 - JB - Created
#endregion
#region Copywright
/* Authors:
 *      Jonathan Baier (jbaier@usgs.gov)
 * Copyright:
 *      2012 USGS - WiM
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Runtime.InteropServices;
using System.Web;

namespace BLTServices.Authentication
{
    public class EasySecureString : IDisposable
    {
        private IntPtr _stringPointer;
        private SecureString _secureString = new SecureString();

        public EasySecureString(String value) {
            for (int i = 0; i < value.Length; i++)
            {
                _secureString.AppendChar(value[i]);
            }
            _secureString.MakeReadOnly();            
        }

        public String decryptString() {
            _stringPointer = Marshal.SecureStringToBSTR(_secureString);
            return Marshal.PtrToStringBSTR(_stringPointer);
        }


        // Implement IDisposable.
        // Do not make this method virtual.
        // A derived class should not be able to override this method.
        public void Dispose()
        {
            Dispose(true);
            // Take this off the Finalization queue 
            // to prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) 
        {
            if (disposing) 
            {
                // Free other state (managed objects).
                _secureString.Dispose();
            }
            Marshal.ZeroFreeBSTR(_stringPointer);
        }

        // Use C# destructor syntax for finalization code.
        ~EasySecureString()
        {
            // Simply call Dispose(false).
            Dispose (false);
        }
    }
}