# [.NET Plug-In Architecture](https://github.com/TomBruns/DotNetCorePlugins)

.NET includes a built-in framework to help you to build applications that are lightweight and extensible by adopting a loosely-coupled plugin-like architecture.

> **Note**: Originally in .NET Framework 4.x, this was called MEF (Managed Extensibility Framework).  More recently this functionality has been migrated to the System.Composition namespace.

---
## DI and MEF

DI (Dependency Injection) and MEF are similar but have a fundamental difference.

They both support the ability to vary the implementation of a contract.

DI is the ability to provide the necessary implementation of a dependency when asked for, but the implementation (assembly) must be known at compile time (ie it is a static dependency).

MEF is the ability to provide the necessary implementation of a dependency when asked for, but the implementation (assembly) can be loaded dynamically at runtime and does not need to be known at compile time.  This is a very effective approach to enable a ***plug-in*** architecture.

MEF takes advantage of an attribute based discovery mechanism to promote extensibility without the need of configuring the components. You don't need any registration -- you just need to mark your types with the Export attribute and it does all for you. 

---
## Implementation

The plug-ins are implemented as independent assemblies.  There is no direct connection between the Hosting process and the plug-ins.  They both only reference a common assembly that defines an interface.

![Assembly Diagram Screenshot](images/assemblies2.jpg?raw=true)

> **Note**: No Compile time references between the hosting program and the plug-ins! 

![Solution Structure Screenshot](images/solutionstructure.jpg?raw=true)

At deployment (or post-build) time, all of the plug-in assemblies (dlls) are copied to unique subfolders under a parent folder that is probed at start-up.

![PlugInFolders](images/pluginfolders.jpg?raw=true)

---
## Building

1. Do a rebuild all *(this makes sure there are no compile errors)*.
2. In each of the plugin project folders *(this copies the plug-in assys to the **PlugInStaging** folder)*...  
  .1 Open a command window  
  .2 execute: `dotnet publish --runtime win-x64 --self-contained true`  
  .3 execute: `dotnet build -target:CopyToStaging`  
3. Build the Host.exe project *(this copies the plug-in folders to the exe's child folder)*
---
## Running

Open a dos windows in the `\bin\Debug\netcoreapp3.1>` folder of each of the projects below and execute the associated exe:
* FIS.USESA.POC.Plugins.Host.exe
* FIS.USA.POC.EventTypeA.Consumer.exe
* FIS.USA.POC.EventTypeB.Consumer.exe
* FIS.USA.POC.EventTypeC.Consumer.exe

Below you can see the results of starting each of the four (4) executables in their own window.


![Running Screenshot](images/running.jpg?raw=true)

---
## Kafka Topics

Each Event Type plug-in publishes to a unique Kafka Topic:

![Kafka Topics Screenshot](images/kafkatopics.jpg?raw=true)

* The topic name is defined in the Event Type publisher
```csharp
[Export(typeof(IEventPublisher))]
[ExportMetadata(MessageSenderType.ATTRIBUTE_NAME, @"EventTypeC")]
public class EventTypeCPublisher : IEventPublisher
{
    public static readonly string TOPIC_NAME = @"EVENT_TYPE_C_TOPIC";
```
## Kafka Consumer Groups
Each Event Type subscriber has a unique Kafka Consumer Group.

![Kafka Consumer Groups](images/kafkaconsumergroups.jpg?raw=true)

* The consumer group name is defined in the Event Type publisher:

```csharp
namespace FIS.USA.POC.EventTypeC.Consumer
{
    /// <summary>
    /// This is an example topic consumer.
    /// </summary>
    class Program
    {
        private const string CONSUMER_GROUP_NAME = @"EVENT_TYPE_C_CONSUMER_GROUP";
```
---
## Plug-in csproj changes

Each plug-in csproj needs to have the following changes:

![CSProj Changes](images/plugincsprojchgs.jpg?raw=true)

1. Configure so the publish command will copy all of the nuget package assemblies AND any unmanaged assemblies they reference.
2. Filter the build output and copy to a unique folder under .\PluginsStaging\\\<project-name>