using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using System;
using System.Collections.ObjectModel;
using Xamarin.Forms;
using Xamarin.Essentials;
using Plugin.BLE.Abstractions;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;

namespace LetsMeasure15
{
    public partial class MainPage : ContentPage
    {
        readonly IBluetoothLE ble;
        readonly IAdapter adapter;
        readonly BluetoothState state;
        readonly ObservableCollection<VDevice> devices;
        readonly List<String> DeviceType;
        readonly INotificationManager notificationManager;
        
        public MainPage()
        {
            InitializeComponent();
            devices = new ObservableCollection<VDevice>();
            ble = CrossBluetoothLE.Current;
            adapter = CrossBluetoothLE.Current.Adapter;
            state = ble.State;
            DevicesListView.ItemsSource = devices;
            DeviceType = new List<string>()
            {
                "TV",
                "PC",
                "Labtop",
                "MacBook",
                "[AV] Samsung",
                "Charger",
                "JBL",
                "Mi"
            };
          
            notificationManager = DependencyService.Get<INotificationManager>();
            notificationManager.NotificationReceived += (sender, eventArgs) =>
            {
                var evtData = (NotificationEventArgs)eventArgs;
            };
            LbStatus.Text = "Setup";
            BluetoothStatusChecker();
            ScanAndCheckAsync();
            CheckForAnyNewDevice();
        }

        private void BluetoothStatusChecker()
        {
            ble.StateChanged += async (s, e) =>
            {
                if (e.NewState == BluetoothState.Off || 
                    e.NewState == BluetoothState.Unavailable ||
                    e.NewState == BluetoothState.Unknown)
                {
                    await DisplayAlert("Bluetooth Status", 
                                        $"Blutooth is: {e.NewState}",
                                        "OK");
                };
            };
        }

        private void CheckForAnyNewDevice()
        {
            Device.StartTimer(new TimeSpan(0, 0, 10), () =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    ScanAndCheckAsync();
                });

                return true; // runs again, or false to stop
            });
        }

        private async void ScanAndCheckAsync() 
        {
            StatusActivityIndicator.IsRunning = true;
            StatusActivityIndicator.IsVisible = true;
            devices.Clear();
            LbStatus.Text = "devices cleard";
            Console.WriteLine("----------------------------devices cleard------------------------------------\n");
            adapter.DeviceDiscovered += (s, a) =>
            {
                IDevice device = a.Device;
                VDevice vDevice = new VDevice() { Device = device, Distance = 0 };
                if (!devices.Contains(vDevice) && !FilterDevices(vDevice))
                {
                    vDevice.Distance = CalculateDistance(vDevice);
                    devices.Add(vDevice);
                    Console.WriteLine("----------------------------Device Added------------------------------------\n");
                }
            };

            if (!ble.Adapter.IsScanning)
            {
                Console.WriteLine("----------------------------Scan started------------------------------------\n");
                adapter.ScanMode = ScanMode.Balanced;
                LbStatus.Text = "Scaning";
                adapter.ScanTimeout = 10000;
                await adapter.StartScanningForDevicesAsync();
            }

            DevicesListView.ItemsSource = devices;
            StatusActivityIndicator.IsRunning = false;
            StatusActivityIndicator.IsVisible = false;
            LbStatus.Text = "Rescaning in 10s";
        }

        private void SendVibrateAlert()
        {
            try
            {
                // Use default vibration length
                Vibration.Vibrate();

                // Or use specified time
                var duration = TimeSpan.FromSeconds(1);
                Vibration.Vibrate(duration);

            }
            catch (FeatureNotSupportedException ex)
            {
                throw new FeatureNotEnabledException(ex.Message);
            }
            catch (Exception ex)
            {
                // Other error has occurred.
                throw new Exception(ex.Message);
            }
        }

        private bool FilterDevices(VDevice device)
        {
            if (String.IsNullOrEmpty(device.Device.Name))
                return false;

            foreach (string type in DeviceType)
            {
                if (device.Device.Name.Contains(type))
                {
                    return true;
                }
            }
            
            return false;
        }

        private double CalculateDistance(VDevice device)
        {
            int RSSI = device.Device.Rssi;
            double MeasuredPower = -65;
            double N = 2.4;
            double one = MeasuredPower - RSSI;
            double two = 10 * N;
            double resutltPartOne = one / two;
            double distance = Math.Pow(10,resutltPartOne);
            distance = Math.Round(distance, 2);
            if (distance <= 1.50)
            {
                SendNotification(device.Device.NativeDevice + ",  dis:" + distance);
                SendVibrateAlert();
                Console.WriteLine("--------Alert--------------------#" + RSSI + "#: " + distance + "-----------------------------------\n");
            }
            return distance;
        }

        void SendNotification(String msg)
        {
            string title = $"1.5m Alert";
            string message = $"{msg}"; //$"You are in a danger zone! Keep a 1.5m";
            notificationManager.ScheduleNotification(title, message);
        }
    }
}
