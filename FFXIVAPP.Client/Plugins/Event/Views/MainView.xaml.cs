﻿// FFXIVAPP.Client
// MainView.xaml.cs
// 
// © 2013 Ryan Wilson

namespace FFXIVAPP.Client.Plugins.Event.Views
{
    /// <summary>
    ///     Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView
    {
        public MainView()
        {
            InitializeComponent();
            View = this;
        }

        public static MainView View { get; set; }
    }
}