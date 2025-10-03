using System.IO;
using System.Reflection;

namespace FolderCollections   // <— Root! nicht .Web
{
    internal static class Embedded
    {
        private static readonly Assembly Asm = typeof(Embedded).Assembly;

        internal static string ReadAllText(string logicalName)
        {
            using var s = Asm.GetManifestResourceStream(logicalName)
                ?? throw new InvalidDataException($"Embedded resource not found: {logicalName}");
            using var r = new StreamReader(s);
            return r.ReadToEnd();
        }
    }
}
