using System.IO;

namespace CalradicExchange
{
    /// <summary>Resolves folders inside Modules\CalradicExchange from the location of our DLL.</summary>
    public static class ModulePaths
    {
        /// <summary>Modules\CalradicExchange (our DLL lives in bin\Win64_Shipping_Client below it).</summary>
        public static string ModuleRoot
        {
            get
            {
                string dllDir = Path.GetDirectoryName(typeof(ModulePaths).Assembly.Location);
                return Path.GetFullPath(Path.Combine(dllDir, "..", ".."));
            }
        }

        public static string LogsFolder => Path.Combine(ModuleRoot, "Logs");
    }
}
