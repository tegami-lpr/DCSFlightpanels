using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;

namespace ClassLibraryCommon
{
    /// <summary>Class <c>Paths</c> used to determine paths to application and user data.</summary>
    public static class FPPaths
    {
        /// <summary>
        /// Enum of data path types
        /// </summary>
        public enum EDataPath
        {
            /// <summary>
            /// Path to mutable user's data
            /// </summary>
            dpUser = 0,
            /// <summary>
            /// Application install dir
            /// </summary>
            dpApp = 1,
            /// <summary>
            /// For automatic path determinics between user or app dirs.
            /// </summary>
            dpAuto = 2
        };


        /// <summary>
        /// Function prepend <paramref name="relativePath"/> relativePath by path where data is stored.
        /// </summary>
        /// <param name="relativePath">Path, relative to data dir</param>
        /// <param name="pathType">Type of data path</param>
        /// <returns>A string with path w/o ending delimeter.</returns>

        public static string PrependPath(string relativePath, EDataPath pathType = EDataPath.dpUser)
        {
            string dataPath = string.Empty;
            do
            {
                if (!string.IsNullOrEmpty(_staticPath))
                {
                    dataPath = _staticPath;
                    break;
                }

                if (_isAppDirWritable || pathType == EDataPath.dpApp)
                {
                    dataPath = GetApplicationPath();
                    break;
                }

                if (pathType == EDataPath.dpAuto)
                {
                    string userDirPath = PrependPath(relativePath, EDataPath.dpUser);
                    if (IsDir(userDirPath))
                    {
                        return userDirPath;
                    }

                    string appDirPath = PrependPath(relativePath, EDataPath.dpApp);
                    if (IsDir(appDirPath))
                    {
                        return appDirPath;
                    }

                    //if all fail, then return path relative to user dir
                    return userDirPath;
                }
            } while (false);

            if (string.IsNullOrEmpty(relativePath))
            {
                return dataPath;
            }

            dataPath = Path.Combine(dataPath, relativePath);
            return dataPath;
        }

        ///<summary>Determine and return path to root of application's constant data.</summary>
        ///<returns>A string with path w/o ending delimeter.</returns>
        public static string GetApplicationPath()
        {
            string appDir = RemoveEndingSeparator(AppDomain.CurrentDomain.BaseDirectory);
            return appDir;
        }

        ///<summary>Determine and return path to root of mutable user data, such as logs, configs, profiles, etc...</summary>
        ///<returns>A string with path w/o ending delimeter.</returns>
        public static string GetUserDataPath()
        {
            string appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return string.IsNullOrEmpty(appDataDir) ? string.Empty : appDataDir + "\\DCSFlightpanels";
        }


        public static void SetPath(string path)
        {
            _staticPath = path;
        }
        public static string GetPath()
        {
            return _staticPath;
        }

        /// <summary>
        /// Forcely set writable app dir flag to <paramref name="value"/>
        /// </summary>
        public static void SetPortable(bool value = true)
        {
            _isAppDirWritable = value;
            _portabilityChecked = true;
        }

        /// <summary>
        /// Check if application dir is writable
        /// </summary>
        /// <returns>True if app dir is writable and false - if not.</returns>
        public static bool CheckPortable()
        {
            if (_portabilityChecked)
            {
                return _isAppDirWritable;
            }

            _isAppDirWritable = false;

            string tmpFilePath = PrependPath("tmp.tmp", EDataPath.dpApp);
            try
            {
                FileStream tmpFile = File.Create(tmpFilePath, 100, FileOptions.DeleteOnClose);
                tmpFile.Close();
                _isAppDirWritable = true;
            }
            catch (Exception)
            {
                //No luck...
            }
            _portabilityChecked = true;
            return _isAppDirWritable;
        }

        /// <summary>
        /// Check, if necessary and return possibility to write to application dir.
        /// </summary>
        /// <returns>True is app can be portable or false if not.</returns>
        public static bool IsPortable()
        {
            return _portabilityChecked == false ? CheckPortable() : _isAppDirWritable;
        }


        public static bool IsFileExists(string path)
        {
            return File.Exists(path);
        }

        public static bool IsDir(string path)
        {
            return Directory.Exists(path);
        }

        /// <summary>
        /// Function remove ending separator from <paramref name="path"/>
        /// </summary>
        /// <param name="path"></param>
        /// <returns>A string with path w/o ending separator</returns>
        private static string RemoveEndingSeparator(string path)
        {
            if (path.EndsWith("\\") || path.EndsWith("/"))
            {
                path = path.Remove(path.Length - 1, 1);
            }
            return path;
        }

        /// <summary>
        /// Path, forcely used as app's and user's data root, instead of deducting
        /// </summary>
        private static string _staticPath = string.Empty;
        /// <summary>
        /// Sign that the application directory is writable.
        /// </summary>
        private static bool _isAppDirWritable = false;
        /// <summary>
        /// Sign that possibility to write to app dir is checked
        /// </summary>
        private static bool _portabilityChecked = false;
    }
}
