﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FileActionsMenu.Ui.Actions
{
    internal abstract class ICheckableAction : IAction
    {
        public IAction.ItemType Type => IAction.ItemType.Checkable;

        public IAction[]? SubMenuItems => null;

        public int Category => 0;

        public Task Execute(object sender, RoutedEventArgs e)
        {
            throw new InvalidOperationException();
        }

        public abstract bool IsChecked { get; set; }

        public abstract bool IsCheckedByDefault { get; }

        public abstract string? CheckableGroupUUID { get; }

        public abstract string[] SelectedItems { get; set; }

        public abstract string Header { get; }

        public abstract Wpf.Ui.Controls.IconElement? Icon { get; }

        public abstract bool IsVisible { get; }
    }
}