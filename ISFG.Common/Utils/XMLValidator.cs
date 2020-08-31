using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace ISFG.Common.Utils
{
    public static class XMLValidator
    {
        #region Static Methods

        /// <summary>
        /// Validates provided XML file againts provided XSD file.
        /// </summary>
        /// <param name="XMLPath">XML file to validate</param>
        /// <param name="XSDPath">XSD file use for validation</param>
        /// <param name="xsdNamespace">Namespace of XSD</param>
        /// <returns>ValidationResult IsOK true if validation went OK, otherwise returns false with error message providing futher description.</returns>
        /// <exception cref="ArgumentException">Throw when XMLPath or XSDPath files does not exists.</exception>
        public static ValidationResult ValidateXML(string xsdNamespace, string XMLPath, string XSDPath)
        {
            if (!File.Exists(XMLPath))
                throw new ArgumentException(nameof(XMLPath), "Provided XML file doesn't exists");

            if (!File.Exists(XSDPath))
                throw new ArgumentException(nameof(XSDPath), "Provided XSD file doesn't exists");

            XmlSchemaSet schema = new XmlSchemaSet();
            schema.Add(xsdNamespace, XSDPath);

            XmlReader rd = XmlReader.Create(XMLPath);
            XDocument doc = XDocument.Load(rd);

            try
            {
                doc.Validate(schema, ValidationEventHandler);
            }
            catch (Exception e)
            {
                return new ValidationResult { IsOK = false, ErrorMessage = e.Message };
            }

            return new ValidationResult { IsOK = true };
        }

        private static void ValidationEventHandler(object sender, ValidationEventArgs e)
        {
            XmlSeverityType type = XmlSeverityType.Warning;
            if (Enum.TryParse("Error", out type))
                if (type == XmlSeverityType.Error) throw new Exception(e.Message);
        }

        #endregion
    }
    public class ValidationResult
    {
        #region Properties

        public bool IsOK { get; set; }
        public string ErrorMessage { get; set; }

        #endregion
    }
}
