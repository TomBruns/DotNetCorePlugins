using System;
using System.Composition;

using FIS.USESA.POC.Plugins.Interfaces;

namespace FIS.USESA.POC.Plugins.ExampleB
{
    /// <summary>
    /// This is an example of a plug-in that can be dynamically loaded at runtime by a hosting process
    /// A post build step for this project copies all the build outputs to a known location
    /// </remarks>
    [Export(typeof(IMessageSender))]
    [ExportMetadata(MessageSenderType.ATTRIBUTE_NAME, @"ExampleB")]
    public class ExampleBSender : IMessageSender
    {
        public string Send(string message)
        {
            string response = $"Sending Message: [{message}] from [{nameof(ExampleBSender)}]";
            Console.WriteLine(response);

            return response;
        }
    }
}