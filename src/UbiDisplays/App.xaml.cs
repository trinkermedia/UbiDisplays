using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

namespace UbiDisplays
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// A reference to the command line arguments we were passed when the app started.
        /// </summary>
        public static String[] CommandLineArgs = new String[0];

        /// <summary>
        /// Store a reference to any command line arguments we are given.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            CommandLineArgs = e.Args;
        }
    }
}
