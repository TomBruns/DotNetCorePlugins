using System;
using System.Composition;

using FIS.USESA.POC.Plugins.Interfaces;

namespace FIS.USESA.POC.Plugins.ExampleA
{
    /// <summary>
    /// This is an example of a plug-in that can be dynamically loaded at runtime by a hosting process
    /// </summary>
    /// <remarks>
    /// A post build step for this project copies all the build outputs to a known (shared) location
    /// All classes of this type will have the same Export attribute to identify this as a plug-in
    /// Each class will have a unique value for the ExportMetadata attribute allows this particular plug-in to be selected at runtime.
    /// </remarks>
    [Export(typeof(IMessageSender))]
    [ExportMetadata(MessageSenderType.ATTRIBUTE_NAME, @"ExampleA")]
    public class ExampleASender : IMessageSender
    {
        public string Send(string message)
        {
            string response = $"Sending Message: [{message}] from [{nameof(ExampleASender)}]";
            Console.WriteLine(response);

            return response;
        }
    }
}
