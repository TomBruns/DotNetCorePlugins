Notes:
=====================================================================
This folder contains the FIS.USESA.POC.Plugins.Interfaces.dll assy

All plug in projects will need a assy (not project) reference to this dll that defines an interface and config data class they need.

Plug-In projects will likely be built by independant teams and NOT be part of the Scheduler Service Visual Studio Solution.
This assy is a good candidate to be packaged as a NuGet package for the projects that need to reference it