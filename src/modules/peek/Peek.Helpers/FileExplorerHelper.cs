﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Peek.Common.Models;
using Peek.Helper.Extensions;
using SHDocVw;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using IServiceProvider = Peek.Common.Models.IServiceProvider;

namespace Peek.Helpers
{
    public static class FileExplorerHelper
    {
        public static IShellItemArray? GetSelectedItems(HWND foregroundWindowHandle)
        {
            return GetItemsInternal(foregroundWindowHandle, onlySelectedFiles: true);
        }

        public static IShellItemArray? GetItems(HWND foregroundWindowHandle)
        {
            return GetItemsInternal(foregroundWindowHandle, onlySelectedFiles: false);
        }

        public static IShellItemArray? GetItemsInternal(HWND foregroundWindowHandle, bool onlySelectedFiles)
        {
            if (foregroundWindowHandle.IsDesktopWindow())
            {
                return GetItemsFromDesktop(foregroundWindowHandle, onlySelectedFiles);
            }
            else
            {
                return GetItemsFromFileExplorer(foregroundWindowHandle, onlySelectedFiles);
            }
        }

        public static IShellItemArray? GetItemsFromDesktop(HWND foregroundWindowHandle, bool onlySelectedFiles)
        {
            const int SWC_DESKTOP = 8;
            const int SWFO_NEEDDISPATCH = 1;

            var shell = new Shell32.Shell();
            ShellWindows shellWindows = shell.Windows();

            object? oNull1 = null;
            object? oNull2 = null;

            var serviceProvider = (IServiceProvider)shellWindows.FindWindowSW(ref oNull1, ref oNull2, SWC_DESKTOP, out int pHWND, SWFO_NEEDDISPATCH);
            var shellBrowser = (IShellBrowser)serviceProvider.QueryService(PInvoke.SID_STopLevelBrowser, typeof(IShellBrowser).GUID);

            IShellItemArray? shellItemArray = GetShellItemArray(shellBrowser, onlySelectedFiles);
            return shellItemArray;
        }

        public static IShellItemArray? GetItemsFromFileExplorer(HWND foregroundWindowHandle, bool onlySelectedFiles)
        {
            IShellItemArray? shellItemArray = null;

            var activeTab = foregroundWindowHandle.GetActiveTab();

            var shell = new Shell32.Shell();
            ShellWindows shellWindows = shell.Windows();
            foreach (IWebBrowserApp webBrowserApp in shellWindows)
            {
                try
                {
                    if (webBrowserApp.Document is Shell32.IShellFolderViewDual2 shellFolderView)
                    {
                        var folderTitle = shellFolderView.Folder.Title;

                        if (webBrowserApp.HWND == foregroundWindowHandle)
                        {
                            var serviceProvider = (IServiceProvider)webBrowserApp;
                            var shellBrowser = (IShellBrowser)serviceProvider.QueryService(PInvoke.SID_STopLevelBrowser, typeof(IShellBrowser).GUID);
                            shellBrowser.GetWindow(out IntPtr shellBrowserHandle);

                            if (activeTab == shellBrowserHandle)
                            {
                                shellItemArray = GetShellItemArray(shellBrowser, onlySelectedFiles);
                                return shellItemArray;
                            }
                        }
                    }
                }
                catch (COMException)
                {
                       // Ignore the exception and continue to the next window
                }
            }

            return shellItemArray;
        }

        public static IShellItemArray? GetShellItemArray(IShellBrowser shellBrowser, bool onlySelectedFiles)
        {
            var shellViewObject = shellBrowser.QueryActiveShellView();
            var shellView = shellViewObject as IFolderView;
            if (shellView != null)
            {
                var selectionFlag = onlySelectedFiles ? (uint)_SVGIO.SVGIO_SELECTION : (uint)_SVGIO.SVGIO_ALLVIEW;
                shellView.ItemCount(selectionFlag, out var countItems);
                if (countItems > 0)
                {
                    shellView.Items(selectionFlag, typeof(IShellItemArray).GUID, out var items);
                    return items as IShellItemArray;
                }
            }

            return null;
        }
    }
}
