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
## DI and MEF

There is no direct connection between the Hosting process and the plug-ins.  They only both reference an independent assembly that defines an interface.

![docker-compose up -d Screenshot](images/assemblies.jpg?raw=true)

> **Note**: No Compile time references between the hosting program and the plug-ins! 

![docker-compose up -d Screenshot](images/solution2.jpg?raw=true)

At deployment time, all of the plug-in assemblies (dlls) are copied to a child folder that will be probed at start-up.

![docker-compose up -d Screenshot](images/folders.jpg?raw=true)