using System;

namespace FIS.USESA.POC.Plugins.ExampleADependency
{
    /// <summary>
    /// This class tests if assys that are downstream dependencies of the plug-in assy can be loadedat runtime.
    /// </summary>
    public static class ExampleAChildDependency
    {
        public static string Test()
        {
            return $"Hello from [{nameof(ExampleAChildDependency)}]";
        }
    }
}
