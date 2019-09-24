using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Musegician
{
    public class FileUtility
    {
        public static string GetDataPath()
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            if (!Directory.Exists(path))
            {
                path = "C:\\";

                if (!Directory.Exists(path))
                {
                    throw new IOException("Can't find proper location for application data");
                }
            }

            path = Path.Combine(path, "Musegician");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);

                if (!Directory.Exists(path))
                {
                    throw new IOException("Failed to create Musegician directory?");
                }
            }

            return path;
        }

        //public static string GetIRPath()
        //{
        //    string path = Environment.GetFolderPath(Environment.SpecialFolder.);

        //    path = Path.Combine(path, "IRFiles");
        //    if (!Directory.Exists(path))
        //    {

        //    }

        //    return path;
        //}
    }
}
