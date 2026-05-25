using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace WordCountServer
{
    class FileSearcher
    {
        private string _rootFolder;

        public FileSearcher(string rootFolder)
        {
            _rootFolder = rootFolder;
        }
        public string FindFile(string fileName)
        {
            string[] foundFiles = Directory.GetFiles(
                _rootFolder,
                fileName,
                SearchOption.AllDirectories
            );

            if (foundFiles.Length == 0)
                return null;

            return foundFiles[0];
        }
        public Task<string> FindFileAsync(string fileName)
        {
            return Task.Run(() =>
            {
                string[] foundFiles = Directory.GetFiles(
                    _rootFolder,
                    fileName,
                    SearchOption.AllDirectories
                );

                if (foundFiles.Length == 0)
                    return null;

                return foundFiles[0];
            });
        }

        public string ReadFile(string fullPath)
        {
            return File.ReadAllText(fullPath, Encoding.UTF8);
        }

        public async Task<string> ReadFileAsync(string fullPath)
        {
            return await File.ReadAllTextAsync(fullPath, Encoding.UTF8);
        }
    }
}