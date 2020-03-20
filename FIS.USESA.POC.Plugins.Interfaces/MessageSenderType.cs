using System;
using System.Collections.Generic;
using System.Text;

namespace FIS.USESA.POC.Plugins.Interfaces
{
    /// <summary>
    /// This class will hold the value of ExportMetadata attribute at runtime when the assy is loaded
    /// </summary>
    public class MessageSenderType
    {
        public const string ATTRIBUTE_NAME = "Name";

        public string Name { get; set; }
    }
}
