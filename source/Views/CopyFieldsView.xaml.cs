﻿using DuplicateHider.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DuplicateHider.Views
{
    /// <summary>
    /// Interaktionslogik für CopyFieldsView.xaml
    /// </summary>
    public partial class CopyFieldsView : UserControl
    {
        public CopyFieldsView()
        {
            InitializeComponent();
        }

        public CopyFieldsView(CopyFieldsViewModel model) : this()
        {
            DataContext = model;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (Parent is Window window)
            {
                window.Close();
            }
        }
    }
}
