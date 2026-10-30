using System;
using System.IO;
using System.Reflection;
using System.Threading;

namespace Brainless.Internal.Updates
{
    internal enum InstallOutcome
    {
        Installed,
        AlreadyPresent,
        Busy
    }

    internal static class UpdateFiles
    {
        internal static InstallOutcome Install(string targetPath, string assemblyName, Version version, string checksum, byte[] bytes, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            if (bytes == null || bytes.Length == 0 || bytes.Length > UpdatePolicy.MaxDllBytes || !UpdatePolicy.Sha256(bytes).Equals(checksum, StringComparison.OrdinalIgnoreCase))
                throw new UpdateException("Downloaded DLL failed its size or SHA-256 check; the installed file was not replaced.");
            targetPath = Path.GetFullPath(targetPath);
            if (!File.Exists(targetPath))
                throw new UpdateException("The loaded plugin's installed file could not be found; automatic installation was skipped.");
            FileStream fileLock;
            try
            {
                fileLock = new FileStream(targetPath + ".brainless-lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                return InstallOutcome.Busy;
            }

            using (fileLock)
            {
                string pending = targetPath + "." + Guid.NewGuid().ToString("N") + ".pending";
                try
                {
                    var disk = AssemblyName.GetAssemblyName(targetPath);
                    if (disk.Name != assemblyName)
                        throw new UpdateException("Installed DLL identity changed; refusing to replace another assembly.");
                    if (UpdatePolicy.Normalize(disk.Version) >= UpdatePolicy.Normalize(version))
                        return InstallOutcome.AlreadyPresent;
                    using (var stream = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        stream.Write(bytes, 0, bytes.Length);
                        stream.Flush(true);
                    }

                    AssemblyName candidate;
                    try
                    {
                        candidate = AssemblyName.GetAssemblyName(pending);
                    }
                    catch (Exception exception)when (exception is BadImageFormatException || exception is FileLoadException)
                    {
                        throw new UpdateException("Downloaded file is not a valid managed DLL; the installed file was not replaced.");
                    }

                    if (candidate.Name != assemblyName || UpdatePolicy.Normalize(candidate.Version) != UpdatePolicy.Normalize(version))
                        throw new UpdateException("Downloaded DLL identity/version does not match the release manifest; the installed file was not replaced.");
                    cancellation.ThrowIfCancellationRequested();
                    // Same-directory atomic replacement. Never delete the working DLL first.
                    // EXILED loads plugin bytes, so the running assembly stays in memory.
                    File.Replace(pending, targetPath, targetPath + ".previous", true);
                    return InstallOutcome.Installed;
                }
                finally
                {
                    if (File.Exists(pending))
                        File.Delete(pending);
                }
            }
        }
    }
}
