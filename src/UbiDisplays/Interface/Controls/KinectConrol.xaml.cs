using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UbiDisplays.Interface.Controls
{
    /// <summary>
    /// Interaction logic for KinectConrol.xaml
    /// </summary>
    public partial class KinectConrol : UserControl
    {
        /// <summary>
        /// Create the kinect control.
        /// </summary>
        public KinectConrol()
        {
            InitializeComponent();
        }

        /// <summary>
        /// The device ID of the sensor connection.
        /// </summary>
        public String SensorDeviceConnectionID { get; set; }
    }
}
