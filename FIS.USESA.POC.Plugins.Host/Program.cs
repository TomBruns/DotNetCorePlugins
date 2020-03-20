using System;
using System.Collections.Generic;
using System.Composition;
using System.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

using FIS.USESA.POC.Plugins.Interfaces;

namespace FIS.USESA.POC.Plugins.Host
{
    /// <summary>
    /// This is a sample of a hosting program that will load plug-in assys and dynamically invoke a method on them.
    /// </summary>
    /// <remarks>
    /// Notes: This assy DOES NOT have any static compile time references to the plug-in assys!
    ///        A post build step for this project copies all the plug-in assy from a known location
    /// </remarks>
    class Program
    {
        // this the subfolder where are plug-in will be located
        const string PLUGIN_FOLDER = @"Plugins";

        static void Main(string[] args)
        {
            Console.WriteLine("Plugin Demo");
            Console.WriteLine(" 1. loading plug-in assys from a subfolder on start-up");
            Console.WriteLine(" 2. dynamically pick a specific plug-in based on runtime supplied criteria");
            Console.WriteLine(" 3. invoke a method on the selected plug-in");
            Console.WriteLine("This is all accomplished without compile-time refererences to the plug-in assys");
            Console.WriteLine("-------------------------------------------------------------------------------");

            // upfront, load the plug-in assys 
            Compose();
            Console.WriteLine($"Step 1: Loaded [{MessageSenders.Count()}] plug-in assys");
            Console.WriteLine();

            // build a list of event types to process
            List<string> eventTypes = new List<string> { @"ExampleA", @"ExampleB" };
            Console.WriteLine($"Step 2: Process some messages dynamically calling the correct plug-in");
            Run(eventTypes);
        }

        static void Run(List<string> eventTypes)
        {
            foreach(string eventType in eventTypes)
            {
                // dynamically get the correct plug-in assy
                IMessageSender messageSender = GetMessageSender(eventType);

                // call the method on the assy
                messageSender.Send("Test");
            }

            System.Console.WriteLine("Hit any key to exit...");
            System.Console.ReadLine();
        }

        /// <summary>
        /// Dynamically pick the correct plug-in from the ones we loaded
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>IMessageSender.</returns>
        private static IMessageSender GetMessageSender(string name)
        {
            return MessageSenders
              .Where(ms => ms.Metadata.Name.Equals(name))
              .Select(ms => ms.Value)
              .FirstOrDefault();
        }
     
        /// <summary>
        /// This collection holds the dynamically loaded assys    
        /// </summary>
        /// <value>The message senders.</value>
        [ImportMany()]
        public static IEnumerable<Lazy<IMessageSender, MessageSenderType>> MessageSenders { get; set; }

        /// <summary>
        /// Build the conposition host from assys that are dynamically loaded from a specific subfolder.
        /// </summary>
        private static void Compose()
        {
            // build the correct path to load the plug-in assys from
            var executableLocation = Assembly.GetEntryAssembly().Location;
            var path = Path.Combine(Path.GetDirectoryName(executableLocation), PLUGIN_FOLDER);

            // get a list of all the assys from that path
            var assemblies = Directory
                        .GetFiles(path, "*.dll", SearchOption.AllDirectories)
                        .Select(AssemblyLoadContext.Default.LoadFromAssemblyPath)
                        .ToList();
            var configuration = new ContainerConfiguration()
                        .WithAssemblies(assemblies);

            // load the plug-in assys that export the correct attribute
            using (var container = configuration.CreateContainer())
            {
                MessageSenders = container.GetExports<Lazy<IMessageSender, MessageSenderType>>();
            }
        }
    }
}
