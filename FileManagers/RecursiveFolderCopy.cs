using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace QuizPersistence
{
    internal record class RecursiveFolderCopy(string folderPath)
    {
        DirectoryInfo sourceInf = new DirectoryInfo(folderPath);
        private const int MaxRecursionDepth = 100;
         
        public string CopyTo(string DestinationPath, bool recursive)
        {
            return CopyTo(DestinationPath, recursive, 0);
        }

        private string CopyTo(string DestinationPath, bool recursive, int depth)
        {
            // Prevent stack overflow from excessive recursion depth
            if (depth > MaxRecursionDepth)
                throw new InvalidOperationException($"Maximum recursion depth ({MaxRecursionDepth}) exceeded. Possible circular directory structure or excessively deep folder hierarchy.");

            // Get information about the source directory
            var dir = sourceInf;

            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

            // Cache directories before we start copying
            DirectoryInfo[] dirs = dir.GetDirectories();

            // Create the destination directory
            Directory.CreateDirectory(DestinationPath + dir.Name);

            // Get the files in the source directory and copy to the destination directory
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(DestinationPath, file.Name);
                file.CopyTo(targetFilePath);
            }

            // If recursive and copying subdirectories, recursively call this method
            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(DestinationPath, subDir.Name);
                    new RecursiveFolderCopy(subDir.FullName).CopyTo(newDestinationDir, true, depth + 1);
                }
            }

            return Path.Combine(DestinationPath, Path.GetFileName(folderPath));
        }
    }
}
