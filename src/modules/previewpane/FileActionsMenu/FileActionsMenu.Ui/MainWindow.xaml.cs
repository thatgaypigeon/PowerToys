﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows.Controls;
using FileActionsMenu.Ui.Actions;
using FileActionsMenu.Ui.Actions.CopyPath;
using FileActionsMenu.Ui.Actions.Hashes.Hashes;
using Wpf.Ui.Controls;
using MenuItem = Wpf.Ui.Controls.MenuItem;

namespace FileActionsMenu.Ui
{
    public partial class MainWindow : FluentWindow
    {
        private static readonly IAction[] Actions =
        [
            new CopyPath(),
            new Hashes(),
            new FileLocksmith(),
            new CopyImageToClipboard(),
            new CopyTo(),
            new PowerRename(),
            new ImageResizer(),
            new MoveTo(),
            new NewFolderWithSelection(),
            new Close(),
        ];

        public MainWindow(string[] selectedItems)
        {
            InitializeComponent();

            // WindowStyle = WindowStyle.None;
            // AllowsTransparency = true;

            // Wpf.Ui.Appearance.SystemThemeWatcher.Watch(this, WindowBackdropType.None);
            ContextMenu cm = (ContextMenu)FindResource("Menu");
            Array.Sort(Actions, (a, b) => a.Category.CompareTo(b.Category));

            int currentCategory = -1;

            foreach (IAction action in Actions)
            {
                action.SelectedItems = selectedItems;
                if (action.IsVisible)
                {
                    if (action.Category != currentCategory)
                    {
                        currentCategory = action.Category;
                        cm.Items.Add(new Separator());
                    }

                    MenuItem menuItem = new()
                    {
                        Header = action.Header,
                    };

                    if (action.Icon != null)
                    {
                        menuItem.Icon = action.Icon;
                        if (menuItem.Icon is FontIcon fontIcon)
                        {
                            fontIcon.FontFamily = new System.Windows.Media.FontFamily("Segoe MDL2 Assets");
                        }
                    }

                    if (action.HasSubMenu)
                    {
                        foreach (IAction subAction in action.SubMenuItems!)
                        {
                            subAction.SelectedItems = selectedItems;

                            if (subAction.IsVisible)
                            {
                                MenuItem subMenuItem = new()
                                {
                                    Header = subAction.Header,
                                };

                                if (action.Icon != null)
                                {
                                    menuItem.Icon = action.Icon;
                                    if (menuItem.Icon is FontIcon fontIcon)
                                    {
                                        fontIcon.FontFamily = new System.Windows.Media.FontFamily("Segoe MDL2 Assets");
                                    }
                                }

                                subMenuItem.Click += subAction.Execute;

                                menuItem.Items.Add(subMenuItem);
                            }
                        }
                    }
                    else
                    {
                        menuItem.Click += action.Execute;
                    }

                    cm.Items.Add(menuItem);
                }
            }

            cm.IsOpen = true;
            cm.Closed += (sender, args) => Close();
        }
    }
}
