//------------------------------------------------------------------------------
//----- SimpleUTF8XmlCodec -----------------------------------------------------
//------------------------------------------------------------------------------

//-------1---------2---------3---------4---------5---------6---------7---------8
//       01234567890123456789012345678901234567890123456789012345678901234567890
//-------+---------+---------+---------+---------+---------+---------+---------+

// copyright:   2012 WiM - USGS

//    authors:  Jeremy Newson USGS Wisconsin Internet Mapping
//              
//  
//   purpose:   Create in place of OpenRasta's XmlSerializerCodec which does not 
//              allows the EntityKey properties to be ignored
//
//discussion:   A Codec is an enCOder/DECoder for a resources in 
//              this case the resources are POCO classes derived from the EF. 
//              https://github.com/openrasta/openrasta/wiki/Codecs
//
//     

#region Comments
// 06.15.12 - JKN - Created from UTF8XmlSerialixerCodec
// 06.19.12 - JKN - Added OverrideAttribute methods which ignore EntityKey and Reference objects
#endregion


using System;
using System.IO;
using System.Text;
using System.Xml;

using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Data.Objects.DataClasses;
using System.Xml.Serialization;
using OpenRasta.Codecs;
using OpenRasta.TypeSystem;
using OpenRasta.Web;



namespace BLTServices.Codecs
{
    [MediaType("application/xml;q=0.4", ".xml")]
    public class SimpleUTF8XmlSerializerCodec : UTF8XmlCodec
    {
        #region Methods
        public override object ReadFrom(IHttpEntity request, IType destinationType, string parameterName)
        {
            if (destinationType.StaticType == null)
                throw new InvalidOperationException();

            return new XmlSerializer(destinationType.StaticType).Deserialize(request.Stream);
        }

        
        public override void WriteToCore(object obj, IHttpEntity response)
        {
            //XmlAttributeOverrides Overrider = null;
            //Type type = obj.GetType();

            //// Check for ListTypes
            //if (type.IsGenericType && type.GetGenericTypeDefinition()
            //        == typeof(List<>))
            //{
            //    type = type.GetGenericArguments()[0];
            //}
                        
            //Overrider = OverrideReferenceAttributes(type);
           
            ////ignore toplevel superfluous EntityKey objects
            //Overrider.Add(typeof(EntityObject), "EntityKey", new XmlAttributes { XmlIgnore = true });

            //addMediaReferences(obj);

            // create the overrider serialzier    
            XmlSerializer serializer = new XmlSerializer(obj.GetType(),  
                                                        this.OverrideReferenceAttributes( obj.GetType()));
       
                //serialize
            serializer.Serialize(Writer, obj);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Overrides xmlIgnore Attribute for Reference type objects
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        protected XmlAttributeOverrides OverrideReferenceAttributes(Type entityType)
        {
            XmlAttributeOverrides xmlOverrider = new XmlAttributeOverrides();
            XmlAttributes xmlAttribute = null;

            // Check for ListTypes
            if (entityType.IsGenericType && entityType.GetGenericTypeDefinition()
                    == typeof(List<>))
            {
                //override to generic type
                entityType = entityType.GetGenericArguments()[0];
            }          

            IEnumerable<string> properties =
                entityType
                    .GetProperties()
                    .Where(e => e.Name.Contains("Reference"))
                    .Select(e => e.Name);
            
                // assign XmlAttribute to override those fields with XmlIgnoreAttribute    
            foreach (string propertyName in properties)
            {
                xmlAttribute = new XmlAttributes
                { XmlIgnore = true };

                xmlOverrider.Add(entityType, propertyName, xmlAttribute);

            }//Next


            //Lastly ignore toplevel superfluous EntityKey objects
            xmlOverrider.Add(typeof(EntityObject), "EntityKey", new XmlAttributes { XmlIgnore = true });

            return xmlOverrider;
        
        }//end OverrideAttributes

         #endregion
    }
}
