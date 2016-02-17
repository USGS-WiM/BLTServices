
using System;
using System.Text;
using System.Xml;
using OpenRasta.Codecs;
using OpenRasta.TypeSystem;
using OpenRasta.Web;

namespace BLTServices.Codecs
{
    public abstract class WiMXmlCodec : IMediaTypeReader, IMediaTypeWriter
    {
        public object Configuration { get; set; }
        protected XmlWriter Writer { get; private set; }

        public abstract void WriteToCore(object entity, IHttpEntity response);
        public abstract object ReadFrom(IHttpEntity request, IType destinationType, string memberName);

        public virtual void WriteTo(object entity, IHttpEntity response, string[] parameters)
        {
            var responseStream = response.Stream;
            using (Writer = XmlWriter.Create(responseStream, 
                                             new XmlWriterSettings
                                             {
                                                 ConformanceLevel =
                                                     ConformanceLevel.Document, 
                                                 Indent = true, 
                                                 Encoding = new UTF8Encoding(false),
                                                 NewLineOnAttributes = true, 
                                                 OmitXmlDeclaration = false, 
                                                 CloseOutput = true, 
                                                 CheckCharacters = true
                                             }))
            {
                WriteToCore(entity, response);
            }
        }
    }
}
