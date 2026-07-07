using LegendBar.Extensibility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Windows.Storage;

namespace LegendBar.Helpers
{
    public static class ExtensionLoader
    {
        private static readonly List<(IWidgetExtension Instance, ExtensionLoadContext Context)> _loaded = new();

        public static string ExtensionsFolderPath =>
            Path.Combine(ApplicationData.Current.LocalFolder.Path, "Extensions");

        public static IEnumerable<IWidgetExtension> LoadAll()
        {
            Directory.CreateDirectory(ExtensionsFolderPath);

            foreach (var dll in Directory.GetFiles(ExtensionsFolderPath, "*.dll"))
            {
                IWidgetExtension? ext = TryLoadOne(dll);
                if (ext != null)
                    yield return ext;
            }
        }

        private static IWidgetExtension? TryLoadOne(string dllPath)
        {
            try
            {
                var context = new ExtensionLoadContext(dllPath);
                var asm = context.LoadFromAssemblyPath(dllPath);

                var type = asm.GetTypes().FirstOrDefault(t =>
                    typeof(IWidgetExtension).IsAssignableFrom(t) &&
                    !t.IsInterface && !t.IsAbstract);

                if (type == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[Extensions] No IWidgetExtension found in {dllPath}");
                    return null;
                }

                var instance = (IWidgetExtension)Activator.CreateInstance(type)!;
                _loaded.Add((instance, context));
                return instance;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Extensions] Failed to load {dllPath}: {ex.Message}");
                return null;
            }
        }

        public static void UnloadAll()
        {
            foreach (var (instance, context) in _loaded)
            {
                try { instance.OnUnload(); } catch { }
                context.Unload();
            }
            _loaded.Clear();
        }
    }
}