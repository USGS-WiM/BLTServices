#region Comments
// 01.23.12 - JB - Created
#endregion
#region Copywright
/* Authors:
 *      Jonathan Baier (jbaier@usgs.gov)
 * Copyright:
 *      2012 USGS - WiM
 */
#endregion

using System;
using System.Collections;
using System.Data.Objects.DataClasses;
using System.Xml.Serialization;
using OpenRasta.Codecs;
using OpenRasta.TypeSystem;
using OpenRasta.Web;

namespace BLTServices.Codecs
{
    [MediaType("application/xml;q=0.4", ".xml")]
    public class WiMXmlSerializerCodec : WiMXmlCodec
    {
        public override object ReadFrom(IHttpEntity request, IType destinationType, string parameterName)
        {
            if (destinationType.StaticType == null)
                throw new InvalidOperationException();

            return new XmlSerializer(destinationType.StaticType).Deserialize(request.Stream);
        }

        public override void WriteToCore(object obj, IHttpEntity response)
        {           
            var serializer = new XmlSerializer(obj.GetType());
            serializer.Serialize(Writer, obj);
        }
    }
}
