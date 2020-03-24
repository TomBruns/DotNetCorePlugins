using System;
using System.Collections.Generic;
using System.Composition;
using System.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.FileExtensions;
using Microsoft.Extensions.Configuration.Json;

using FIS.USESA.POC.Plugins.Interfaces;
using FIS.USESA.POC.Plugins.Interfaces.Entities;

namespace FIS.USESA.POC.Plugins.Host
{
    /// <summary>
    /// This is a sample of a program that will load plug-in assys and dynamically invoke a method on them.
    /// The plug-in assys coudl be written independantly and only need a reference to a shared assy that defines an interface they must implement
    /// </summary>
    /// <remarks>
    /// Notes: 
    ///   This assy DOES NOT have any static compile time references to the plug-in assys!
    ///   A post build step for this project copies all the plug-in assy from a known location to a specific subfolder of the buidl target folder
    /// </remarks>
    class Program
    {
        // this is the subfolder where all the plug-ins will be loaded from
        const string PLUGIN_FOLDER = @"Plugins";

        // this collection holds the dynamically loaded assys   
        //  IEventPublisher is the common interface that all the sample plug-ins will implement
        //  MessageSenderType has a custom property that will allow us to pick a specific plug-in
        [ImportMany()]
        private static IEnumerable<Lazy<IEventPublisher, MessageSenderType>> MessageSenders { get; set; }

        static void Main(string[] args)
        {
            Console.WriteLine("Plugin Demo");
            Console.WriteLine(" 1. loading plug-in assys from a subfolder on start-up");
            Console.WriteLine(" 2. dynamically pick a specific plug-in based on runtime supplied criteria");
            Console.WriteLine(" 3. invoke a method on the selected plug-in");
            Console.WriteLine();
            Console.WriteLine("Note: This is all accomplished without compile-time (ie static) references to the plug-in assys");
            Console.WriteLine("-------------------------------------------------------------------------------");

            // ==========================
            // load some config info that we need to inject into all the plug-ins
            // ==========================
            IConfiguration config = new ConfigurationBuilder()
                                          .AddJsonFile("appsettings.json", true, true)
                                          .Build();

            var kafkaConfig = config.GetSection("kafkaConfig").Get<KafkaServiceConfigBE>();

            // ==========================
            // load the plug-in assys 
            // ==========================
            Compose();

            if (MessageSenders == null) return;
            Console.WriteLine($"Step 1: Loaded [{MessageSenders.Count()}] plug-in assys from subfolder: [.\\{PLUGIN_FOLDER}]");
            foreach (var plugin in MessageSenders)
            {
                Console.WriteLine($"==> {plugin.Value.GetType().Assembly.FullName}");
            }
            Console.WriteLine();

            // ==========================
            // for troubleshooting purposes, enumerate all of the loaded assys
            // ==========================
            Console.WriteLine("-------------------------------------------------------------------------------");
            Console.WriteLine("Loaded Assemblies:");
            Console.WriteLine();
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var loadedAssembly in loadedAssemblies.Where(a => !a.IsDynamic))
            {
                var nameParts = loadedAssembly.FullName.Split(",");
                Console.WriteLine($"==> [{nameParts[0]}] from [{loadedAssembly.Location}]");
            }

            // ==========================
            // on startup, inject the config info into all of the plug-ins
            // ==========================
            foreach (var plugin in MessageSenders)
            {
                plugin.Value.InjectConfig(kafkaConfig);
            }

            // ==========================
            // build a list of event to process
            // ==========================
            List<EventBE> events = new List<EventBE>
            {
                new EventBE() { EventType = @"EventTypeA", Message = @"Message 1" },
                new EventBE() { EventType = @"EventTypeB", Message = @"Message 2" },
                new EventBE() { EventType = @"EventTypeB", Message = @"Message 3" },
                new EventBE() { EventType = @"EventTypeA", Message = @"Message 4" },
                new EventBE() { EventType = @"EventTypeB", Message = @"Message 5" }
            };

            // ==========================
            // process the events
            // ==========================
            // declare outside loop to reduce gc pressure
            IEventPublisher eventPublisher = null;

            Console.WriteLine();
            Console.WriteLine("-------------------------------------------------------------------------------");
            Console.WriteLine($"Step 2&3: Process some messages dynamically calling the correct plug-in");
            Console.WriteLine();
            foreach (var eventInstance in events)
            {
                // use the string value of the EventType property to dynamically select the correct plug-in assy to use to process the event
                eventPublisher = GetEventPublisher(eventInstance.EventType);

                // call the method on the dynamically selected assy
                eventPublisher.PublishEvent(eventInstance.Message);
            }

            Console.WriteLine(@"=============================================================");
            Console.WriteLine(@"Hit any key to exit....");
            Console.ReadLine();

        }

        #region Helpers

        /// <summary>
        /// Dynamically pick the correct plug-in from the ones we loaded
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>IEventPublisher.</returns>
        private static IEventPublisher GetEventPublisher(string name)
        {
            var plugIn = MessageSenders
              .Where(ms => ms.Metadata.Name.Equals(name))
              .Select(ms => ms.Value);

            if (plugIn == null)
            {
                throw new ApplicationException($"No plug-in found for Event Type: [{name}]");
            }
            else if (plugIn.Count() != 1)
            {
                throw new ApplicationException($"Multiple plug-ins [{plugIn.Count()}] found for Event Type: [{name}]");
            }
            else
            {
                return plugIn.FirstOrDefault();
            }
        }

        /// <summary>
        /// Build the composition host from assys that are dynamically loaded from a specific subfolder.
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

            try
            {
                // load the plug-in assys that export the correct attribute
                using (var container = configuration.CreateContainer())
                {
                    MessageSenders = container.GetExports<Lazy<IEventPublisher, MessageSenderType>>();
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                StringBuilder sb = new StringBuilder();
                foreach (Exception exSub in ex.LoaderExceptions)
                {
                    sb.AppendLine(exSub.Message);
                    FileNotFoundException exFileNotFound = exSub as FileNotFoundException;
                    if (exFileNotFound != null)
                    {
                        if (!string.IsNullOrEmpty(exFileNotFound.FusionLog))
                        {
                            sb.AppendLine("Fusion Log:");
                            sb.AppendLine(exFileNotFound.FusionLog);
                        }
                    }
                    sb.AppendLine();
                }
                string errorMessage = sb.ToString();
                Console.WriteLine(errorMessage);
            }
        }

        #endregion
    }
}
