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
using OpenHardwareMonitor.Hardware;
using ZedGraph;

namespace Hw_Monitor
{

    public partial class MainWindow : Window
    {
        Computer thisComputer = new Computer();

        DispatcherTimer timer = new DispatcherTimer();

        PointPairList cpuLoadList = new PointPairList();
        PointPairList ramUsageList = new PointPairList();
        PointPairList diskUsageList = new PointPairList();

        double tmp_ram = 0.0, tmp_disk = 0.0, tmp_cpuLoad = 0.0;
        int tmp_cpuTemp = 0, x_time = 0;

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        public MainWindow()
        {
            InitializeComponent();
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Tick += timer_Tick;
            timer.Start();

            thisComputer.CPUEnabled = true;
            thisComputer.RAMEnabled = true;
            thisComputer.HDDEnabled = true;
            thisComputer.Open();

            //zedGraph cpuLoad
            zedgraph_cpu.GraphPane.XAxis.Title.Text = "Zeit";
            zedgraph_cpu.GraphPane.YAxis.Title.Text = "Auslastung in %";
            zedgraph_cpu.GraphPane.CurveList.Clear();
            cpuLoadList.Clear();

            //zedGraph ramUsage
            zedgraph_ram.GraphPane.XAxis.Title.Text = "Zeit";
            zedgraph_ram.GraphPane.YAxis.Title.Text = "Durchsatz";
            zedgraph_ram.GraphPane.CurveList.Clear();
            ramUsageList.Clear();

            //zedGraph diskUsage
            zedgraph_disk.GraphPane.XAxis.Title.Text = "Zeit";
            zedgraph_disk.GraphPane.YAxis.Title.Text = "Durchsatz";
            zedgraph_disk.GraphPane.CurveList.Clear();
            diskUsageList.Clear();
        }

        private void button_click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            String chosen = btn.Tag.ToString();
            if (chosen == "Cpu")
            {

                textBox_ramInfo.Visibility = Visibility.Hidden;
                textBox_diskInfo.Visibility = Visibility.Hidden;
                textBox_cpuInfo.Visibility = Visibility.Visible;
                Zed_cpu.Visibility = Visibility.Visible;
                Zed_ram.Visibility = Visibility.Hidden;
                Zed.Visibility = Visibility.Hidden;
                Zed_disk.Visibility = Visibility.Hidden;

            }
            if (chosen == "Ram")
            {
                textBox_cpuInfo.Visibility = Visibility.Hidden;
                textBox_diskInfo.Visibility = Visibility.Hidden;
                textBox_ramInfo.Visibility = Visibility.Visible;
                Zed_ram.Visibility = Visibility.Visible;
                Zed_cpu.Visibility = Visibility.Hidden;
                Zed.Visibility = Visibility.Hidden;
                Zed_disk.Visibility = Visibility.Hidden;
            }
            if (chosen == "Disk")
            {
                textBox_ramInfo.Visibility = Visibility.Hidden;
                textBox_cpuInfo.Visibility = Visibility.Hidden;
                textBox_diskInfo.Visibility = Visibility.Visible;
                Zed_cpu.Visibility = Visibility.Hidden;
                Zed_ram.Visibility = Visibility.Hidden;
                Zed.Visibility = Visibility.Hidden;
                Zed_disk.Visibility = Visibility.Visible;
            }
        }
        public void timer_Tick(object sender, EventArgs e)
        {
            String cpuLoadString = "";
            String ramUsageString = "";
            String hddUsageString = "";
            String usage = "";

            foreach (var hardwareItem in thisComputer.Hardware)
            {
                if (hardwareItem.HardwareType == HardwareType.CPU)
                {
                    hardwareItem.Update();
                    foreach (IHardware subHardware in hardwareItem.SubHardware)
                        subHardware.Update();

                    foreach (var sensor in hardwareItem.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Temperature)
                        {
                            tmp_cpuTemp = (int)sensor.Value.Value;
                            usage += String.Format("{0} Temperature = {1} °C\r\n", sensor.Name, sensor.Value.HasValue ? sensor.Value.Value.ToString() : "no value");
                            if (tmp_cpuTemp >= 65)
                            {
                                Button_cpu.Background = Brushes.IndianRed;
                            }
                        }

                        if (sensor.SensorType == SensorType.Load)
                        {
                            tmp_cpuLoad = (double)sensor.Value.Value;
                            cpuLoadString += String.Format("{0} Load = {1}\r\n", sensor.Name, sensor.Value.HasValue ? sensor.Value.Value.ToString() : "no value");
                        }
                    }
                } else if (hardwareItem.HardwareType == HardwareType.RAM) {
                    hardwareItem.Update();
                    foreach (IHardware subHardware in hardwareItem.SubHardware)
                        subHardware.Update();

                    foreach (var sensor in hardwareItem.Sensors)
                    {    
                        if (sensor.SensorType == SensorType.Data)
                        {
                            tmp_ram = (double)sensor.Value.Value;
                            ramUsageString += String.Format("{0} Ram = {1}\r\n", sensor.Name, sensor.Value.HasValue ? sensor.Value.Value.ToString() : "no value");
                        }
                    }
                }
                else if (hardwareItem.HardwareType == HardwareType.HDD)
                {
                    hardwareItem.Update();
                    foreach (IHardware subHardware in hardwareItem.SubHardware)
                        subHardware.Update();

                    foreach (var sensor in hardwareItem.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Load)
                        {
                            tmp_disk = (double)sensor.Value.Value;
                            hddUsageString += String.Format("{0} HDD = {1}\r\n", sensor.Name, sensor.Value.HasValue ? sensor.Value.Value.ToString() : "no value");
                        }
                    }
                }
            }

            //Hardware Informationen
            textBox_cpuInfo.Text = cpuLoadString + "\n" + usage;
            textBox_ramInfo.Text = ramUsageString;
            textBox_diskInfo.Text = hddUsageString;

            //clear graph every 60 seconds
            if (x_time == 60)
            {
                //CPU
                zedgraph_cpu.GraphPane.CurveList.Clear();
                zedgraph_cpuMini.GraphPane.CurveList.Clear();
                cpuLoadList.Clear();

                //Ram
                zedgraph_ram.GraphPane.CurveList.Clear();
                zedgraph.GraphPane.CurveList.Clear();
                zedgraph_ramMini.GraphPane.CurveList.Clear();
                ramUsageList.Clear();

                //Disk 
                zedgraph_disk.GraphPane.CurveList.Clear();
                zedgraph_diskMini.GraphPane.CurveList.Clear();
                diskUsageList.Clear();

                x_time = 0;
            }
            x_time++;

            //X und Y Werte List übergeben
            cpuLoadList.Add(x_time, tmp_cpuLoad);
            ramUsageList.Add(x_time, tmp_ram);
            diskUsageList.Add(x_time, tmp_disk);

            //Graph zeichnen
            LineItem myCurve_cpu = zedgraph_cpu.GraphPane.AddCurve("", cpuLoadList, System.Drawing.Color.Red, SymbolType.None);
            zedgraph_cpu.AxisChange();
            zedgraph_cpu.Refresh();

            LineItem myCurve_ram = zedgraph_ram.GraphPane.AddCurve("", ramUsageList, System.Drawing.Color.Blue, SymbolType.None);
            zedgraph_ram.AxisChange();
            zedgraph_ram.Refresh();

            LineItem myCurve_disk = zedgraph_disk.GraphPane.AddCurve("", diskUsageList, System.Drawing.Color.Green, SymbolType.None);
            zedgraph_disk.AxisChange();
            zedgraph_disk.Refresh();

            LineItem myCurve = zedgraph.GraphPane.AddCurve("", cpuLoadList, System.Drawing.Color.Red, SymbolType.None);
            LineItem myCurve2 = zedgraph.GraphPane.AddCurve("", ramUsageList, System.Drawing.Color.Blue, SymbolType.None);
            zedgraph.AxisChange();
            zedgraph.Refresh();

            LineItem myCurve_cpuMini = zedgraph_cpuMini.GraphPane.AddCurve("", cpuLoadList, System.Drawing.Color.Red, SymbolType.None);
            zedgraph_cpuMini.AxisChange();
            zedgraph_cpuMini.Refresh();

            LineItem myCurve_ramMini = zedgraph_ramMini.GraphPane.AddCurve("", ramUsageList, System.Drawing.Color.Blue, SymbolType.None);
            zedgraph_ramMini.AxisChange();
            zedgraph_ramMini.Refresh();

            LineItem myCurve_diskMini = zedgraph_diskMini.GraphPane.AddCurve("", diskUsageList, System.Drawing.Color.Green, SymbolType.None);
            zedgraph_diskMini.AxisChange();
            zedgraph_diskMini.Refresh();
        }
    }
}



