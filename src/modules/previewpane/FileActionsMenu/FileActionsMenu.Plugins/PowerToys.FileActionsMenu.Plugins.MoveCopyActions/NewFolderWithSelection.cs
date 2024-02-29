﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using FileActionsMenu.Helpers;
using FileActionsMenu.Interfaces;
using FileActionsMenu.Ui.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace PowerToys.FileActionsMenu.Plugins.MoveCopyActions
{
    internal sealed class NewFolderWithSelection : IAction
    {
        private string[]? _selectedItems;

        public string[] SelectedItems { get => _selectedItems.GetOrArgumentNullException(); set => _selectedItems = value; }

        public string Title => "New folder with selection";

        public IAction.ItemType Type => IAction.ItemType.SingleItem;

        public IAction[]? SubMenuItems => null;

        public int Category => 1;

        public IconElement? Icon => new FontIcon { Glyph = "\uE8F4" };

        public bool IsVisible => true;

        public Task Execute(object sender, RoutedEventArgs e)
        {
            string path = Path.Combine(Path.GetDirectoryName(SelectedItems[0]) ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "New folder with selection");

            int i = 0;
            while (Directory.Exists(path))
            {
                if (path.EndsWith(')'))
                {
                    path = path[..^(3 + i.ToString(CultureInfo.InvariantCulture).Length)];
                }

                i++;
                path += " (" + i + ")";
            }

            Directory.CreateDirectory(path);

            bool cancelled = false;
            FileActionProgressHelper fileActionProgressHelper = new("Moving files to new folder", SelectedItems.Length, () => { cancelled = true; });

            string append = string.Empty;
            int appendCount = 0;
            string directoryName = "New folder with selection";
            while (Directory.Exists(Path.Combine(Path.GetDirectoryName(SelectedItems[0]) ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop), directoryName)))
            {
                appendCount++;
                append = " (" + appendCount + ")";
                directoryName = "New folder with selection" + append;
            }

            int count = -1;
            foreach (string item in SelectedItems)
            {
                if (cancelled)
                {
                    return Task.CompletedTask;
                }

                i++;
                if (File.Exists(item))
                {
                    fileActionProgressHelper.UpdateProgress(count, Path.GetFileName(item));

                    File.Move(item, Path.Combine(Path.GetDirectoryName(SelectedItems[0]) ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop), directoryName));
                }
                else if (Directory.Exists(item))
                {
                    fileActionProgressHelper.UpdateProgress(count, Path.GetFileName(item));

                    Directory.Move(item, Path.Combine(Path.GetDirectoryName(SelectedItems[0]) ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop), directoryName));
                }
            }

            return Task.CompletedTask;
        }
    }
}
