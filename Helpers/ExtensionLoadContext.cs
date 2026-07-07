using System;
using System.Reflection;
using System.Runtime.Loader;

namespace LegendBar.Helpers
{
    public class ExtensionLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        public ExtensionLoadContext(string pluginDllPath) : base(isCollectible: true)
        {
            _resolver = new AssemblyDependencyResolver(pluginDllPath);
        }

        protected override Assembly? Load(AssemblyName name)
        {
            // Let shared framework/host assemblies (Microsoft.UI.Xaml,
            // WindowsAppSDK, LegendBar.Extensibility, System.*) resolve
            // against the host's already-loaded copies instead of
            // loading a second copy — avoids type-identity mismatches.
            if (name.Name != null &&
                (name.Name.StartsWith("Microsoft.UI") ||
                 name.Name.StartsWith("Microsoft.WindowsAppSDK") ||
                 name.Name.StartsWith("Microsoft.Windows") ||
                 name.Name == "LegendBar.Extensibility" ||
                 name.Name.StartsWith("System.") ||
                 name.Name.StartsWith("WinRT")))
            {
                return null; // null = defer to default/host context
            }

            string? path = _resolver.ResolveAssemblyToPath(name);
            return path != null ? LoadFromAssemblyPath(path) : null;
        }
    }
}