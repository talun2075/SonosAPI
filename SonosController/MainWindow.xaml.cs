using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using SonosUPNP;

namespace SonosController
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly SonosDiscovery _sonos;
        //string uuid = "RINCON_B8E937EE969801400";
        private string uuid = "";
        readonly Dictionary<String, String> sdNameUUID = new Dictionary<string, string>();
        public MainWindow()
        {
            InitializeComponent();
            _sonos = new SonosDiscovery();
            _sonos.StartScan();
            _sonos.TopologyChanged += _sonos_TopologyChanged;
        }

        void _sonos_TopologyChanged()
        {
            if (String.IsNullOrEmpty(uuid))
            {
                //Es wurde noch kein Player definiert

                //Gruppenänderungen feststellen.
                Dispatcher.BeginInvoke(new Action(() => listBox1.ItemsSource = null));
                //Nur Player, später auf Zonen gehen.
                foreach (var sdp in _sonos.Players)
                {
                    if(!sdNameUUID.ContainsValue(sdp.UUID))
                    {
                        sdNameUUID.Add(sdp.Name, sdp.UUID);
                    }
                }
                Dispatcher.BeginInvoke(new Action(() => listBox1.ItemsSource = sdNameUUID.Keys));
                Dispatcher.BeginInvoke(new Action(() => listBox1.Visibility= Visibility.Visible));
                //throw new NotImplementedException();
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            //Testen ob man den Player bekommen kann
            
            if (_sonos.Players.Any())
            {
                SonosPlayer pl = _sonos.Players.First(x => x.UUID == uuid);
                MessageBox.Show(pl.Name);
            }

        }

        private void listBox1_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            string k;
            if (listBox1.Items.Count > 0)
            {
                sdNameUUID.TryGetValue(listBox1.SelectedValue.ToString(), out k);
                uuid = k;
                listBox1.Visibility= Visibility.Hidden;
            }
        }
    }
}
