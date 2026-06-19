using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WordCountServer
{
    class FileSearcher
    {
        private string _rootFolder;

        //cuva po 1 SemaphoreSlim za svaki fajl
        //kljuc je putanja fajla, vrednost je lock za taj fajl
        private Dictionary<string, SemaphoreSlim> _fileLocks = new Dictionary<string, SemaphoreSlim>();
        private object _fileLocksLock = new object();

        public FileSearcher(string rootFolder)
        {
            _rootFolder = rootFolder;
        }

        //vraca postojeci lock za fajl ili pravi novi ako ne postoji
        private SemaphoreSlim GetFileLock(string fullPath)
        {
            lock (_fileLocksLock)
            {
                if (!_fileLocks.ContainsKey(fullPath))
                    _fileLocks[fullPath] = new SemaphoreSlim(1, 1);
                return _fileLocks[fullPath];
            }
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

        public async Task<string> ReadFileAsync (string fullPath)
        {
            SemaphoreSlim fileLock = GetFileLock(fullPath);

            await fileLock.WaitAsync(); //cekamo da se oslobodi lock za ovaj fajl
            try
            {
                return await File.ReadAllTextAsync(fullPath, Encoding.UTF8);
            }
            finally
            {
                //finally garantuje da se lock uvek oslobodi, cak i ako dodje do greske pri citanju
                fileLock.Release();
            }
        }
    }
}