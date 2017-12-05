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
using System.Management;
using OpenHardwareMonitor.Collections;
using OpenHardwareMonitor.Hardware;
using System.Windows.Threading;
using System.Diagnostics;

namespace HardwareMonitor
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        PerformanceCounter perfCPU_TotalCounter = new PerformanceCounter("Prozessorinformationen", "Prozessorauslastung", "_Total");
        Computer thisComputer;

        public MainWindow()
        {
            InitializeComponent();
            thisComputer = new Computer() { CPUEnabled = true };
            thisComputer.Open();

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e) {
            String temp = "";
            String usage = "";
            float cpu_total = (int)perfCPU_TotalCounter.NextValue();

            foreach (var hardwareItem in thisComputer.Hardware) {
                if (hardwareItem.HardwareType == HardwareType.CPU) {
                    hardwareItem.Update();
                    foreach (IHardware subHardware in hardwareItem.SubHardware)
                        subHardware.Update();

                    foreach (var sensor in hardwareItem.Sensors) {
                        if (sensor.SensorType == SensorType.Temperature) {
                            temp += String.Format("{0} Temperature = {1}\r\n", sensor.Name, sensor.Value.HasValue ? sensor.Value.Value.ToString() : "no value");
                        }
                        if (sensor.SensorType == SensorType.Load) {
                            usage += String.Format("{0} Load = {1}\r\n", sensor.Name, sensor.Value.HasValue ? sensor.Value.Value.ToString() : "no value");
                        }
                    }
                }
            }

            CPU_temp.Text = temp;
            CPU_usage.Text = usage;
            CPUTotal.Value = cpu_total;
        }
    }
}
