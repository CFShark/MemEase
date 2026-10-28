using System;
using System.IO;

namespace MemEase
{
    internal static class IconGenerator
    {
        private static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("Usage: IconGen.exe <output.ico>");
                return 2;
            }

            try
            {
                string path = Path.GetFullPath(args[0]);
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                File.WriteAllBytes(path, IconFactory.CreateAppIcon());
                Console.WriteLine("Icon written: " + path);
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.ToString());
                return 1;
            }
        }
    }
}
