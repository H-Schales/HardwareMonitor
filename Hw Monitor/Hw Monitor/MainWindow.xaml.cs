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
using System.Diagnostics;
using System.Windows.Threading;
using System.Net.NetworkInformation;
using OpenHardwareMonitor.Hardware;
using ZedGraph;
using System.Management;
using System.Globalization;
using System.Net;

namespace Hw_Monitor
{
    public partial class MainWindow : Window
    {
        Computer thisComputer             = new Computer();
        DispatcherTimer timer             = new DispatcherTimer();
        CultureInfo ci                    = CultureInfo.CurrentCulture;
        ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT totalphysicalmemory FROM Win32_ComputerSystem");
        IPHostEntry Host                  = Dns.GetHostEntry(Dns.GetHostName());

        //PointPairList for ZedGraph
        PointPairList cpuLoadList   = new PointPairList();
        PointPairList ramUsageList  = new PointPairList();
        PointPairList diskUsageList = new PointPairList();
        PointPairList networkList   = new PointPairList();

        //PerformanceCounter Netzwerkverkehr
        PerformanceCounter network     = new PerformanceCounter();      
        PerformanceCounter networkSent = new PerformanceCounter();

        //Werte der Hardware Komponenten um den Zed Graph zeichnen zu können
        double ramUsage = 0.0, diskUsage = 0.0, cpuLoad = 0.0;
        int x_time = 0, network_Data = 0;

        //Hardware Informationen
        double totalMemory = 0;
        int cpuTemperature = 0, diskTemperature = 0, networkDataSent = 0;

        String language     = "";
        String userName     = "";
        String computerName = "";

        public MainWindow()
        {
            InitializeComponent();
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Tick    += timer_Tick;
            timer.Start();

            thisComputer.CPUEnabled = true;
            thisComputer.RAMEnabled = true;
            thisComputer.HDDEnabled = true;
            thisComputer.GPUEnabled = true;
            thisComputer.Open();

            //zedGraph cpuLoad
            zedgraph_cpu.GraphPane.Title.Text       = "Prozessor";
            zedgraph_cpu.GraphPane.XAxis.Title.Text = "Zeit";
            zedgraph_cpu.GraphPane.YAxis.Title.Text = "Auslastung in %";
            zedgraph_cpu.GraphPane.CurveList.Clear();
            cpuLoadList.Clear();

            //zedGraph ramUsage
            zedgraph_ram.GraphPane.Title.Text       = "Arbeitsspeicher";
            zedgraph_ram.GraphPane.XAxis.Title.Text = "Zeit";
            zedgraph_ram.GraphPane.YAxis.Title.Text = "verwendeter Arbeitsspeicher in %";
            zedgraph_ram.GraphPane.CurveList.Clear();
            ramUsageList.Clear();

            //zedGraph diskUsage
            zedgraph_disk.GraphPane.Title.Text       = "Festplatte";
            zedgraph_disk.GraphPane.XAxis.Title.Text = "Zeit";
            zedgraph_disk.GraphPane.YAxis.Title.Text = "verwendeter Speicher";
            zedgraph_disk.GraphPane.CurveList.Clear();
            diskUsageList.Clear();

            //ZedGraph NetworkTraffic
            zedgraph_network.GraphPane.Title.Text       = "Netzwerkverkehr";
            zedgraph_network.GraphPane.XAxis.Title.Text = "Zeit";
            zedgraph_network.GraphPane.YAxis.Title.Text = "Gesamtanzahl Bytes/s";
            zedgraph_network.GraphPane.CurveList.Clear();
            networkList.Clear();

            //Benutzername, Computername und Systemsprache ermitteln
            userName     = Environment.UserName;
            computerName = Environment.MachineName;
            language = ci.Name;

            // Arbeitsspeichergröße ermitteln
            ManagementObjectCollection res = searcher.Get();
            foreach (ManagementObject mo in res)
            {
                totalMemory = long.Parse(mo["totalphysicalmemory"].ToString());
            }
            totalMemory = totalMemory / 1024 / 1024 / 1024;
            totalMemory = Math.Round(totalMemory, 1);

        }

        private void button_click(object sender, RoutedEventArgs e)
        {
            Button btn    = (Button)sender;
            String chosen = btn.Tag.ToString();

            everythingHidden();

            if (chosen == "Cpu")
            {
                textBox_cpuInfo.Visibility = Visibility.Visible;
                Zed_cpu.Visibility         = Visibility.Visible;
            }
            else if (chosen == "Ram")
            {
                textBox_ramInfo.Visibility = Visibility.Visible;
                Zed_ram.Visibility         = Visibility.Visible;
            }
            else if (chosen == "Disk")
            {
                textBox_diskInfo.Visibility = Visibility.Visible;
                Zed_disk.Visibility         = Visibility.Visible;
            }
            else if (chosen == "Network")
            {
                textBox_networkInfo.Visibility = Visibility.Visible;
                Zed_network.Visibility         = Visibility.Visible;
            }
        }

        public void timer_Tick(object sender, EventArgs e)
        {
            String cpuLoadString        = "";
            String ramUsageString       = "";
            String hddUsageString       = "";
            String cpuTempString        = "";
            String cpuName              = "";
            String diskInfoString       = "";
            String string_NetworkStatus = "";
            String string_MacAddress    = "";
            String string_Network       = "";
            String networkType          = "";
            String IPAddress            = "";

            //HardwareInformationen auslesen
            foreach (var hardwareItem in thisComputer.Hardware)
            {
                if (hardwareItem.HardwareType == HardwareType.CPU)
                {
                    hardwareItem.Update();
                    cpuName = hardwareItem.Name;
                    foreach (IHardware subHardware in hardwareItem.SubHardware)
                        subHardware.Update();
                    foreach (var sensor in hardwareItem.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Temperature)
                        {
                            cpuTemperature = (int)sensor.Value.Value;
                            cpuTempString += String.Format("{0} Temperature: {1} °C\r\n", sensor.Name, sensor.Value.HasValue ? sensor.Value.Value.ToString() : "no value");
                            checkCpuTemp(cpuTemperature);
                        }

                        if (sensor.SensorType == SensorType.Load)
                        {
                            cpuLoad = (double)sensor.Value.Value;
                            cpuLoadString += String.Format("{0} Load: {1} %\r\n", sensor.Name, sensor.Value.HasValue ? Math.Round(sensor.Value.Value, 1).ToString() : "no value");
                        }
                    }
                }
                else if (hardwareItem.HardwareType == HardwareType.RAM)
                {
                    hardwareItem.Update();
                    foreach (IHardware subHardware in hardwareItem.SubHardware)
                        subHardware.Update();

                    foreach (var sensor in hardwareItem.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Data)
                        {
                            ramUsageString += String.Format("{0} Ram:\t{1} GB\r\n", sensor.Name, sensor.Value.HasValue ? Math.Round(sensor.Value.Value, 1).ToString() : "no value");
                        }
                        else if (sensor.SensorType == SensorType.Load)
                        {
                            ramUsage = (double)sensor.Value.Value;
                        }
                    }
                }
                else if (hardwareItem.HardwareType == HardwareType.HDD)
                {
                    hardwareItem.Update();
                    diskInfoString = hardwareItem.Name;
                    foreach (IHardware subHardware in hardwareItem.SubHardware)
                        subHardware.Update();

                    foreach (var sensor in hardwareItem.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Load)
                        {
                            diskUsage = (double)sensor.Value.Value;
                            hddUsageString += String.Format("{0} Disk:\t\t{1} GB\r\n", sensor.Name, sensor.Value.HasValue ? Math.Round(sensor.Value.Value, 1).ToString() : "no value");
                        }
                        else if (sensor.SensorType == SensorType.Data)
                        {
                            hddUsageString += String.Format("{0} Disk:\t{1} GB\r\n", sensor.Name, Math.Round(sensor.Value.Value, 1).ToString());
                        }
                        else if (sensor.SensorType == SensorType.Temperature)
                        {
                            diskTemperature = (int)sensor.Value.Value;
                        }
                    }
                }

                foreach (IPAddress IP in Host.AddressList)
                {
                    IPAddress += (IP.ToString());
                    if (IsIP(IPAddress) == true)
                    {
                        break;
                    }
                    else
                    {
                        IPAddress = "";                      
                    }
                }

                    foreach (var networkItem in NetworkInterface.GetAllNetworkInterfaces())
                    {
                        string_Network       = networkItem.Description.ToString();
                        string_MacAddress    = networkItem.GetPhysicalAddress().ToString();
                        string_NetworkStatus = networkItem.OperationalStatus.ToString();
                        networkType          = networkItem.Name;

                    if (networkType == "WLAN" && string_NetworkStatus == "Up" || networkType == "Ethernet" && string_NetworkStatus == "Up")
                    {
                        string_Network = string_Network.Replace("(", "[").Replace(")", "]").Replace("/", "_");
                        break;
                    }
                    else
                    {
                        string_NetworkStatus = "";
                        string_Network = "";
                        string_MacAddress = "";
                        networkType = "";
                    }
                }

                //Falls alle Netze down sind
                if (IPAddress != "127.0.0.1")
                {
                    if (language == "de-DE")
                    {
                        network.CategoryName = "Netzwerkschnittstelle";
                        network.CounterName = "Gesamtanzahl Bytes/s";
                        network.InstanceName = string_Network;
                        network_Data = (int)network.NextValue();

                        networkSent.CategoryName = "Netzwerkschnittstelle";
                        networkSent.CounterName = "Bytes gesendet/s";
                        networkSent.InstanceName = string_Network;
                        networkDataSent = (int)networkSent.NextValue();
                    }
                    else if (language == "en-US")
                    {
                        network.CategoryName = "Network Interface";
                        network.CounterName = "Bytes Total/sec";
                        network.InstanceName = string_Network;
                        network_Data = (int)network.NextValue();

                        networkSent.CategoryName = "Network Interface";
                        networkSent.CounterName = "Bytes sent/sec";
                        networkSent.InstanceName = string_Network;
                        networkDataSent = (int)networkSent.NextValue();
                    }
                }
            }

            //Hardware Informationen
            textBox_cpuInfo.Text     = "Prozessor:  " + cpuName + "\n" + 
                                        cpuLoadString + "\n" + 
                                       "Prozessortemperatur:" +"\n" + 
                                        cpuTempString;
            textBox_ramInfo.Text     = "Arbeitsspeicher:" +"\n" + 
                                       "Total Memory:" + "\t\t" + totalMemory + " GB\n" + 
                                        ramUsageString;
            textBox_diskInfo.Text    = "Festplatte: " + "\n" +
                                       "Bezeichnung:\t\t" + diskInfoString + "\n" +
                                        hddUsageString +
                                       "Temperatur:\t\t" + diskTemperature +"°C";
            textBox_networkInfo.Text = "Netzwerkarte:" + "\t\t" + string_Network + "\n" +
                                       "Gesamtanzahl Daten:" + "\t" + network_Data.ToString() + " Bytes/s\n" +
                                       "Gesendete Daten:" + "\t\t" + networkDataSent.ToString() + " Bytes/s\n" +
                                       "Netzwerkstatus:" + "\t\t" + string_NetworkStatus + "\n" +
                                       "Mac-Adresse:" + "\t\t" + string_MacAddress + "\n" + 
                                       "IP-Adresse:" + "\t\t" + IPAddress;

            //Label Informationen übergeben
            LabelInfo.Content = "Benutzername: " + userName + "\n" +
                                "Computer: " + computerName + "\n" +
                                "Systemsprache: " + language;

            //clear graph every 60 seconds
            clearZed();

            //X und Y Werte List übergeben
            cpuLoadList.Add(x_time, cpuLoad);
            ramUsageList.Add(x_time, ramUsage);
            diskUsageList.Add(x_time, diskUsage);
            networkList.Add(x_time, network_Data);

            //Graph zeichnen
            LineItem myCurve_cpu = zedgraph_cpu.GraphPane.AddCurve("", cpuLoadList, System.Drawing.Color.Blue, SymbolType.None);
            zedgraph_cpu.AxisChange();
            zedgraph_cpu.Refresh();

            LineItem myCurve_ram = zedgraph_ram.GraphPane.AddCurve("", ramUsageList, System.Drawing.Color.DarkRed, SymbolType.None);
            zedgraph_ram.AxisChange();
            zedgraph_ram.Refresh();

            LineItem myCurve_disk = zedgraph_disk.GraphPane.AddCurve("", diskUsageList, System.Drawing.Color.Green, SymbolType.None);
            zedgraph_disk.AxisChange();
            zedgraph_disk.Refresh();

            LineItem myCurve_network = zedgraph_network.GraphPane.AddCurve("", networkList, System.Drawing.Color.DarkOrange, SymbolType.None);
            zedgraph_network.AxisChange();
            zedgraph_network.Refresh();

            LineItem myCurve_cpuMini = zedgraph_cpuMini.GraphPane.AddCurve("", cpuLoadList, System.Drawing.Color.Blue, SymbolType.None);
            zedgraph_cpuMini.AxisChange();
            zedgraph_cpuMini.Refresh();

            LineItem myCurve_ramMini = zedgraph_ramMini.GraphPane.AddCurve("", ramUsageList, System.Drawing.Color.DarkRed, SymbolType.None);
            zedgraph_ramMini.AxisChange();
            zedgraph_ramMini.Refresh();

            LineItem myCurve_diskMini = zedgraph_diskMini.GraphPane.AddCurve("", diskUsageList, System.Drawing.Color.Green, SymbolType.None);
            zedgraph_diskMini.AxisChange();
            zedgraph_diskMini.Refresh();

            LineItem myCurve_networkMini = zedgraph_networkMini.GraphPane.AddCurve("", networkList, System.Drawing.Color.DarkOrange, SymbolType.None);
            zedgraph_networkMini.AxisChange();
            zedgraph_networkMini.Refresh();
        }

        public void everythingHidden() {
            textBox_cpuInfo.Visibility      = Visibility.Hidden;
            textBox_ramInfo.Visibility      = Visibility.Hidden;
            textBox_diskInfo.Visibility     = Visibility.Hidden;
            textBox_networkInfo.Visibility  = Visibility.Hidden;
            Zed_cpu.Visibility              = Visibility.Hidden;
            Zed_ram.Visibility              = Visibility.Hidden;
            Zed_disk.Visibility             = Visibility.Hidden;
            Zed_network.Visibility          = Visibility.Hidden;
        }

        public void clearZed() {
            if (x_time == 60)
            {
                //CPU
                zedgraph_cpu.GraphPane.CurveList.Clear();
                zedgraph_cpuMini.GraphPane.CurveList.Clear();
                cpuLoadList.Clear();

                //Ram
                zedgraph_ram.GraphPane.CurveList.Clear();
                zedgraph_ramMini.GraphPane.CurveList.Clear();
                ramUsageList.Clear();

                //Disk 
                zedgraph_disk.GraphPane.CurveList.Clear();
                zedgraph_diskMini.GraphPane.CurveList.Clear();
                diskUsageList.Clear();

                //Network
                zedgraph_network.GraphPane.CurveList.Clear();
                zedgraph_networkMini.GraphPane.CurveList.Clear();
                networkList.Clear();

                x_time = 0;
            }
            x_time++;
        }

        public void checkCpuTemp(int cpuTemp)
        {
            if (cpuTemp >= 65)
            {
                Button_cpu.Background = Brushes.IndianRed;
            }
            else
            {
                Button_cpu.Background = Brushes.White;
            }
        }

        //Prüft, ob es sich um eine IPv4-Adresse handelt
        public bool IsIP(string IP)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(IP, @"\b((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$\b");
        }
    }
}



