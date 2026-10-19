using libplctag.NativeImport;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace PlcGhostBuster.Services
{
    /// <summary>
    /// libplctag.NativeImport extracts the native library into AppContext.BaseDirectory (the PowerShell install folder),
    /// which is not writable on Windows without admin rights.
    /// On Windows we extract it to a per user folder, load it from there and tell libplctag it is already initialized.
    /// </summary>
    internal static class NativeLibraryLoader
    {
        private const string LibraryFileName = "plctag.dll";

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string fileName);

        public static void Init()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                //keep the default libplctag behaviour on other platforms
                plctag.ForceExtractLibrary = false;
                return;
            }

            var nativeImport = typeof(plctag).Assembly;
            var resourceName = $"libplctag.NativeImport.runtime.{GetWindowsRuntime()}.{LibraryFileName}";

            //version the folder so an upgrade never collides with a dll loaded by another session
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                         "PlcGhostBuster",
                                         nativeImport.GetName().Version.ToString(),
                                         GetWindowsRuntime());
            var libraryPath = Path.Combine(directory, LibraryFileName);

            using (var stream = nativeImport.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new TypeLoadException($"Native library {resourceName} is not embedded in libplctag.NativeImport");

                if (!File.Exists(libraryPath) || new FileInfo(libraryPath).Length != stream.Length)
                {
                    Directory.CreateDirectory(directory);
                    using (var file = File.Create(libraryPath))
                        stream.CopyTo(file);
                }
            }

            //once loaded, DllImport("plctag") resolves to this module
            if (LoadLibrary(libraryPath) == IntPtr.Zero)
                throw new DllNotFoundException($"Failed to load {libraryPath} (error {Marshal.GetLastWin32Error()})");

            //skip libplctag's own extraction to the PowerShell install folder
            var initializedField = typeof(plctag).GetField("libraryAlreadyInitialized", BindingFlags.NonPublic | BindingFlags.Static);
            if (initializedField == null)
                throw new MissingFieldException("libplctag.NativeImport.plctag", "libraryAlreadyInitialized");
            initializedField.SetValue(null, true);
        }

        private static string GetWindowsRuntime()
        {
            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.X86: return "win_x86";
                case Architecture.X64: return "win_x64";
                case Architecture.Arm: return "win_ARM";
                case Architecture.Arm64: return "win_ARM64";
                default: throw new PlatformNotSupportedException($"Unsupported architecture {RuntimeInformation.ProcessArchitecture}");
            }
        }
    }
}
