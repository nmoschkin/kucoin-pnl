using System;
using System.Collections.Generic;
using System.Windows;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using System.Drawing;
using System.Windows.Forms;

namespace PNLTool
{

    public static class Settings
    {
        public static string ConfigRootKey { get; set; } = @"Software\DataTools\PNLTool";

        public const int DefaultMaxRecentFiles = 40;

        public static void AddRecentFile(string fileName, Guid projectId)
        {
            AddRecentFile(new RecentFile() { FileName = fileName, ProjectId = projectId });
        }

        public static void AddRecentFile(RecentFile file)
        {
            var newList = new List<RecentFile>();
            newList.AddRange(RecentFiles);

            int i, c = newList.Count;

            for (i = c - 1; i >= 0; i--)
            {
                var f = newList[i];

                if (f.FileName.ToLower() == file.FileName.ToLower() && file.ProjectId == f.ProjectId)
                {
                    newList.RemoveAt(i);
                    continue;
                }
            }

            newList.Insert(0, file);
            RecentFiles = newList.ToArray();
        }

        public static void RemoveRecentFile(RecentFile file)
        {
            var newList = new List<RecentFile>();
            newList.AddRange(RecentFiles);

            int i, c = newList.Count;

            for (i = c - 1; i >= 0; i--)
            {
                var f = newList[i];

                if (f.FileName.ToLower() == file.FileName.ToLower() && file.ProjectId == f.ProjectId)
                {
                    newList.RemoveAt(i);
                    continue;
                }
            }

            RecentFiles = newList.ToArray();
        }

        public static void ClearRecentFiles(Guid? projectId = null)
        {
            var newList = new List<RecentFile>();
            newList.AddRange(RecentFiles);

            int i, c = newList.Count;

            for (i = c - 1; i >= 0; i--)
            {
                var f = newList[i];

                if (projectId == null || projectId == f.ProjectId)
                {
                    newList.RemoveAt(i);
                    continue;
                }
            }

            RecentFiles = newList.ToArray();
        }

        public static void CheckRecentFiles()
        {
            RecentFile[] s = RecentFiles;
            RecentFiles = s;
        }


        public static RecentFile[] RecentFiles
        {
            get
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(ConfigRootKey + @"\RecentFiles", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryOptions.None))
                {
                    string[] entries = key.GetValueNames();

                    List<RecentFile> recents = new List<RecentFile>();

                    int idx;
                    RecentFile rfObj;
                    int ncount = 0;
                    int gcount = 0;

                    foreach (string entry in entries)
                    {
                        if (entry == null) continue;

                        if (int.TryParse(entry, out idx))
                        {
                            ncount++;

                            if (RecentFile.TryParse((string)key.GetValue(entry), out rfObj))
                            {
                                if (File.Exists(rfObj.FileName))
                                {
                                    gcount++;
                                    recents.Add(rfObj);
                                }
                            }
                        }
                    }


                    if ((gcount != ncount))
                        SetRecentFilesList(recents.ToArray());

                    return recents.ToArray();
                }
            }
            set
            {
                SetRecentFilesList(value);
            }
        }

        private static void SetRecentFilesList(RecentFile[] files)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(ConfigRootKey + @"\RecentFiles", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryOptions.None))
            {
                string[] values = key.GetValueNames();

                int c = MaxRecentFiles;
                int i;
                int n;

                foreach (string value in values) key.DeleteValue(value);

                if (files == null || files.Length == 0)
                {
                    key.Close();
                    return;
                }

                c = c < files.Length ? c : files.Length;
                n = 0;

                for (i = 0; i <= c - 1; i++)
                {
                    if (files[i] == null) continue;
                    key.SetValue(n++.ToString(), files[i].ToString(), RegistryValueKind.String);
                }

            }
                
        }

        public static int MaxRecentFiles
        {
            get
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(ConfigRootKey, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryOptions.None))
                {
                    int? count = (int)key.GetValue("MaxRecentFiles", DefaultMaxRecentFiles);
                    key.Close();
                    return count ?? 0;
                }
            }
            set
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(ConfigRootKey, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryOptions.None))
                {
                    key.SetValue("MaxRecentFiles", value, RegistryValueKind.DWord);
                    key.Close();
                }
            }
        }

        private static RecentFile[] internalFilterRecentList(Guid? projectId = null, string directory = null, string fileName = null, string nameOnly = null, string extension = null)
        {
            RecentFile[] items = RecentFiles;
            var itemsOut = new List<RecentFile>();

            int i;
            int c = items.Count() - 1;

            string fn;

            int score;
            int passingScore = 0;

            bool pid = false;
            bool ext = false;
            bool name = false;
            bool file = false;
            bool dir = false;

            // get the passing score

            if (projectId != null) { passingScore++; pid = true; }
            if (directory != null) { passingScore++; dir = true; }
            if (nameOnly != null) { passingScore++; name = true; }
            if (fileName != null) { passingScore++; file = true; }
            if (extension != null) { passingScore++; ext = true; }

            for (i = 0; i <= c; i++)
            {
                score = 0;
                fn = items[i].FileName;

                if (pid && items[i].ProjectId == projectId) score++;

                if (ext && Path.GetExtension(fn).ToLower() == extension.ToLower()) score++;

                if (name && Path.GetFileNameWithoutExtension(fn).ToLower() == nameOnly.ToLower()) score++;

                if (file && Path.GetFileName(fn).ToLower() == fileName.ToLower()) score++;

                if (dir && Path.GetDirectoryName(fn).ToLower() == directory.ToLower()) score++;

                if (score == passingScore) itemsOut.Add(items[i]);
            }

            return itemsOut.ToArray();
        }

        public static RecentFile[] FilterRecentList(Guid projectId)
        {
            return internalFilterRecentList(projectId);
        }

        public static RecentFile[] FilterRecentList(Guid projectId, string extension)
        {
            return internalFilterRecentList(projectId, null, null, null, extension);
        }

        public static RecentFile[] FilterRecentList(string extension)
        {
            return internalFilterRecentList(null, null, null, null, extension);
        }

        public static string LastDirectory
        {
            get
            {
                RecentFile[] s = RecentFiles;
                if (s == null || s.Length == 0)
                    return Directory.GetCurrentDirectory();

                return Path.GetDirectoryName(s[0].FileName);
            }
        }

        public static RecentFile LastProjectFile
        {
            get
            {
                RecentFile[] s = RecentFiles;

                if (s == null)
                    return null;

                foreach (var f in s)
                {
                    if (f == null)
                        continue;
                    switch (Path.GetExtension(f.FileName).ToLower())
                    {
                        case ".json":
                            return f;
                    }
                }

                return null;
            }
        }



        public static bool OpenLastOnStartup
        {
            get
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(ConfigRootKey, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryOptions.None))
                {
                    bool res = GetBoolVal(key, "OpenLastOnStartup", true);
                    return res;
                }

            }
            set
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(ConfigRootKey, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryOptions.None))
                {
                    SetBoolVal(key, "OpenLastOnStartup", value);
                }
            }
        }


        public static Guid LastActiveProfileId
        {
            get
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(ConfigRootKey, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryOptions.None))
                {
                    var res = (Guid)(key.GetValue("LastActiveProfileId", Guid.Empty));
                    return res;
                }
            }
            set
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(ConfigRootKey, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryOptions.None))
                {
                    key.SetValue("LastActiveProfileId", value);
                }
            }
        }

        public static string LastBrowseFolder
        {
            get
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(ConfigRootKey, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryOptions.None))
                {
                    var res = (string)(key.GetValue("LastBrowseFolder", string.Empty));
                    return res;
                }
            }
            set
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(ConfigRootKey, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryOptions.None))
                {
                    if (value == null)
                    {
                        key.DeleteValue("LastBrowseFolder");
                    }
                    else 
                    {
                        key.SetValue("LastBrowseFolder", value);
                    }
                }
            }
        }

        public static string[] ActiveFiles
        {
            get
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(ConfigRootKey + "\\ActiveFiles", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryOptions.None))
                {
                    var values = key.GetValueNames();
                    var slist = new List<string>();

                    foreach(var value in values)
                    {
                        var obj = key.GetValue(value);
                        if (obj is string s && File.Exists(s))
                        {
                            slist.Add(s);
                        }
                    }

                    return slist.ToArray();
                }
            }
            set
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(ConfigRootKey + "\\ActiveFiles", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryOptions.None))
                {
                    var values = key.GetValueNames();
                    var newlist = new List<string>(value);

                    foreach (var cvalue in values)
                    {
                        key.DeleteValue(cvalue);
                    }

                    int c = 0;
                    foreach (var newval in newlist)
                    {
                        key.SetValue(c++.ToString(), newval);
                    }
                }
            }
        }

        public static Size LastWindowSize
        {
            get
            {
                // all this just to make a default value
                // all this just to make a default value
                Form wnd = null;

                if (Application.OpenForms?.Count > 0)
                {
                    wnd = Application.OpenForms[0] ?? null;
                }

                var def = new Size(wnd?.Width ?? 800, wnd?.Height ?? 600);

                var bytes = new List<byte>();

                bytes.AddRange(BitConverter.GetBytes(def.Width));
                bytes.AddRange(BitConverter.GetBytes(def.Height));
                // end default value creation

                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(ConfigRootKey, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryOptions.None))
                {
                    byte[] res = (byte[])(key.GetValue("LastWindowSize", bytes.ToArray()));

                    // this data is not correct
                    // overwrite the data with a correct entry
                    if (res.Length != 8)
                    {
                        key.SetValue("LastWindowSize", bytes.ToArray(), RegistryValueKind.Binary);
                        return def;
                    }

                    return new Size(BitConverter.ToInt32(res, 0), BitConverter.ToInt32(res, 4));
                }

            }
            set
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(ConfigRootKey, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryOptions.None))
                {
                    var bytes = new List<byte>();

                    bytes.AddRange(BitConverter.GetBytes(value.Width));
                    bytes.AddRange(BitConverter.GetBytes(value.Height));

                    key.SetValue("LastWindowSize", bytes.ToArray(), RegistryValueKind.Binary);
                }
            }
        }

        public static Point LastWindowLocation
        {
            get
            {
                // all this just to make a default value
                Form wnd = null;
                
                if (Application.OpenForms?.Count > 0)
                {
                    wnd = Application.OpenForms[0] ?? null;
                }

                var def = new Point(wnd?.Left ?? 200, wnd?.Top ?? 200);
                var bytes = new List<byte>();

                bytes.AddRange(BitConverter.GetBytes(def.X));
                bytes.AddRange(BitConverter.GetBytes(def.Y));
                // end default value creation

                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(ConfigRootKey, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryOptions.None))
                {

                    byte[] res = (byte[])(key.GetValue("LastWindowLocation", bytes.ToArray()));

                    // this data is not correct
                    // overwrite the data with a correct entry
                    if (res.Length != 8)
                    {
                        key.SetValue("LastWindowLocation", bytes.ToArray(), RegistryValueKind.Binary);
                        return def;
                    }

                    return new Point(BitConverter.ToInt32(res, 0), BitConverter.ToInt32(res, 4));
                }

            }
            set
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(ConfigRootKey, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryOptions.None))
                {
                    var bytes = new List<byte>();

                    bytes.AddRange(BitConverter.GetBytes(value.X));
                    bytes.AddRange(BitConverter.GetBytes(value.Y));

                    key.SetValue("LastWindowLocation", bytes.ToArray(), RegistryValueKind.Binary);
                }

            }
        }



        private static bool GetBoolVal(RegistryKey key, string valueName, bool? defaultValue)
        {
            object val;
            int defVal;

            if (defaultValue == null)
            {
                defVal = 1;
            }
            else
            {
                if (defaultValue == true) defVal = 1; else defVal = 0;
            }


            if (defaultValue == null)
            {
                val = key.GetValue(valueName);
            }
            else
            {
                val = key.GetValue(valueName, defVal);
            }
            bool res = ((byte[])val)[0] == 0 ? false : true;

            return res;
        }

        private static void SetBoolVal(RegistryKey key, string valueName, bool value)
        {
            int b = value ? 1 : 0;
            key.SetValue(valueName, b, RegistryValueKind.DWord);
        }

        static Settings()
        {
            CheckRecentFiles();
        }
    }
}
