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

using Serilog;

using FIS.USESA.POC.Plugins.Interfaces;
using FIS.USESA.POC.Plugins.Host.Entities;
using FIS.USESA.POC.Plugins.Interfaces.Entities.Config;
using Serilog.Extensions.Logging;

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
        const bool IS_DETAILED_DEBUG_MODE = false;

        // this collection holds the dynamically loaded assys   
        //  IEventPublisher is the common interface that all the sample plug-ins will implement
        //  MessageSenderType has a custom property that will allow us to pick a specific plug-in
        [ImportMany()]
        private static IEnumerable<Lazy<IEventPublisher, MessageSenderType>> MessageSenders { get; set; }

        static void Main(string[] args)
        {
            // configure logging
            Log.Logger = new LoggerConfiguration()
                                .WriteTo.Console()
                                .CreateLogger();

            var logger = new SerilogLoggerProvider(Log.Logger).CreateLogger(nameof(Program));

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
            //ComposeIsolated(new List<string>() { @"FIS.USESA.POC.Plugins.Interfaces.dll" } );

            if (MessageSenders == null) return;
            Console.WriteLine($"Step 1: Loaded [{MessageSenders.Count()}] plug-in assys from subfolder: [.\\{PLUGIN_FOLDER}]");
            foreach (var plugin in MessageSenders)
            {
                Console.WriteLine($"==> {plugin.Value.GetType().Assembly.FullName}");
            }
            Console.WriteLine();

            if (IS_DETAILED_DEBUG_MODE)
            {
                // ==========================
                // for troubleshooting purposes, enumerate all of the loaded assys
                // ==========================
                Console.WriteLine("-------------------------------------------------------------------------------");
                Console.WriteLine("Loaded Assemblies:");
                Console.WriteLine();
                var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var loadedAssembly in loadedAssemblies.Where(a => !a.IsDynamic))
                {
                    var loadContext = AssemblyLoadContext.GetLoadContext(loadedAssembly.GetType().Assembly);
                    var nameParts = loadedAssembly.FullName.Split(",");
                    Console.WriteLine($"==> [{nameParts[0]}] from [{loadedAssembly.Location}] in LoadContext: [{loadContext.Name}]");
                }
            }

            // ==========================
            // on startup, inject the config info into all of the plug-ins
            // ==========================
            foreach (var plugin in MessageSenders)
            {
                plugin.Value.InjectConfig(kafkaConfig, logger);
            }

            // ==========================
            // build a list of event to process
            // ==========================
            // assume EventId, EventType, EventData all come from the DB
            int eventId = 0;

            List<EventBE> events = new List<EventBE>
            {
                new EventBE() { EventId = ++eventId, EventType = @"EventTypeA", EventData = @"Message 1" },
                new EventBE() { EventId = ++eventId, EventType = @"EventTypeB", EventData = @"Message 2" },
                new EventBE() { EventId = ++eventId, EventType = @"EventTypeC", EventData = @"Message 3" },
                new EventBE() { EventId = ++eventId, EventType = @"EventTypeB", EventData = @"Message 4" },
                new EventBE() { EventId = ++eventId, EventType = @"EventTypeA", EventData = @"Message 5" }
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
                eventPublisher.PublishEvent(eventInstance.EventId, eventInstance.EventData);
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

            if (plugIn == null || plugIn.Count() == 0)
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

            // get a list of only the managed dlls
            var managedDlls = GetListOfManagedAssemblies(path, SearchOption.AllDirectories);

            // load the assys
            var assemblies = managedDlls
                        .Select(AssemblyLoadContext.Default.LoadFromAssemblyPath)
                        .ToList();

            // build a composition container
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

        /// <summary>
        /// Gets the list of managed assemblies.
        /// </summary>
        /// <param name="folderPath">The folder path.</param>
        /// <param name="searchOption">The search option.</param>
        /// <returns>List&lt;System.String&gt;.</returns>
        /// <remarks>
        /// Some of the dlls in the target folder may be unmanaged dlls referenced by managed dlls, we need to exclude those
        /// </remarks>
        private static List<string> GetListOfManagedAssemblies(string folderPath, SearchOption searchOption)
        {
            List<string> assyPathNames = new List<string>();

            var files = Directory.GetFiles(folderPath, "*.dll", searchOption);

            foreach (var filePathName in files)
            {
                try
                {
                    // this call will throw an exception if this is not a managed dll
                    System.Reflection.AssemblyName testAssembly = System.Reflection.AssemblyName.GetAssemblyName(filePathName);

                    assyPathNames.Add(filePathName);
                }
                catch 
                { 
                    // swallow the exception and continue
                }
            }

            return assyPathNames;
        }

        /// <summary>
        /// A research spike into loading each plugin's assys into an isolated AssemblyLoadContext
        /// </summary>
        /// <param name="assysToIgnore">The assys to ignore.</param>
        /// <remarks>
        /// assysToIgnore is a list of assys NOT to load into the plug-in AssemblyLoadContext so the types will
        /// match and the a GetExports call will recognize them as the same type
        /// </remarks>
        private static void ComposeIsolated(List<string> assysToIgnore)
        {
            // build the correct path to load the plug-in assys from
            var executableLocation = Assembly.GetEntryAssembly().Location;
            var path = Path.Combine(Path.GetDirectoryName(executableLocation), PLUGIN_FOLDER);

            // find the names of all the plug-in subfolders
            var plugInFolderPathNames = Directory.GetDirectories(path);

            Dictionary<string, IEnumerable<Lazy<IEventPublisher, MessageSenderType>>> test = new Dictionary<string, IEnumerable<Lazy<IEventPublisher, MessageSenderType>>>();

            // loop thru each plug-in subfolder and load the assys into a separate (isolated) load context.
            foreach (var plugInFolderPathName in plugInFolderPathNames)
            {
                var plugInFolderName = Path.GetFileName(plugInFolderPathName);

                var assyLoadContext = new AssemblyLoadContext(plugInFolderName);

                // get a list of all the assys from that path
                var assemblies = Directory
                            .GetFiles(plugInFolderPathName, "*.dll", SearchOption.AllDirectories)
                            .Where(f => !f.Contains(assysToIgnore[0]))
                            .Select(assyLoadContext.LoadFromAssemblyPath)
                            .ToList();

                var configuration = new ContainerConfiguration()
                            .WithAssemblies(assemblies);

                // load the plug-in assys that export the correct attribute
                using (var container = configuration.CreateContainer())
                {
                    MessageSenders = container.GetExports<Lazy<IEventPublisher, MessageSenderType>>();
                }

                test.Add(plugInFolderName, MessageSenders);

                MessageSenders = (IEnumerable<Lazy<IEventPublisher, MessageSenderType>>)test.Values.ToList();
            }
        }
        #endregion
    }
}
