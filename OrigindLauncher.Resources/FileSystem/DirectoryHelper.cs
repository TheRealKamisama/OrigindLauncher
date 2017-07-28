﻿using System;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Security.AccessControl;
using System.Security.Permissions;
using System.Security.Principal;

namespace OrigindLauncher.Resources.FileSystem
{
    public static class DirectoryHelper
    {
        public static void EnsureDirectoryExists(string directoryName)
        {
            if (!Directory.Exists(directoryName)) Directory.CreateDirectory(directoryName);
        }

        public static bool IsCurrentDirectoryWritable
        {
            get
            {
                try
                {
                    return new DirectorySecurity(".", AccessControlSections.Access).AreAccessRulesProtected;
                }
                catch (UnauthorizedAccessException)
                {
                    return false;
                }
                catch (SystemException)
                {
                    return false;
                }
            }
        }

        public static void AddCurrentDirectoryWritePermission()
        {
            Process.Start(
                new ProcessStartInfo(Process.GetCurrentProcess().MainModule.FileName,
                    "AddPermissions") {Verb = "runas"})?.WaitForExit();
        }

        internal static void AddCurrentDirectoryWritePermissionInternal()
        {
            var info = new DirectoryInfo(".");
            var control = info.GetAccessControl();
            control.AddAccessRule(new FileSystemAccessRule("Everyone", FileSystemRights.FullControl,
                AccessControlType.Allow));
            info.SetAccessControl(control);
        }
    }
}