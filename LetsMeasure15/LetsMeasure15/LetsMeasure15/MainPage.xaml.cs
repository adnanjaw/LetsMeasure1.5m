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
                "MacBook",
                "[AV] Samsung",
                "Charger",
                "JBL",
                "Mi"
            };
            BluetoothStatusChecker();
            CheckForAnyNewDevice();

            notificationManager = DependencyService.Get<INotificationManager>();
            notificationManager.NotificationReceived += (sender, eventArgs) =>
            {
                var evtData = (NotificationEventArgs)eventArgs;
                ShowNotification(evtData.Title, evtData.Message);
            };
            LbStatus.Text = "Setup..";
        }

        private void BluetoothStatusChecker()
        {
            ble.StateChanged += async (s, e) =>
            {
                if (e.NewState == BluetoothState.Off || e.NewState == BluetoothState.Unavailable || e.NewState == BluetoothState.Unknown)
                {
                    await DisplayAlert("Bluetooth Status", $"Blutooth is: {e.NewState}", "OK");
                };
            };
        }

        private void CheckForAnyNewDevice()
        {
            
            Device.StartTimer(new TimeSpan(0, 0, 20), () =>
            {
                
                // do something every 10ms
                Device.BeginInvokeOnMainThread(async () =>
                {
                    devices.Clear();
                    Console.WriteLine("----------------------------devices cleard------------------------------------\n");
                    LbStatus.Text = "devices cleard";
                    adapter.DeviceDiscovered += (s, a) =>
                    {
                        IDevice device = a.Device;
                        VDevice vDevice = new VDevice() { Device = device, Distance = 0};
                        if (!devices.Contains(vDevice) && !FilterDevices(vDevice))
                        {
                            vDevice.Distance = Math.Round(CalculateDistance(vDevice),2);
                            devices.Add(vDevice);
                            Console.WriteLine("----------------------------Device Added------------------------------------\n");
                        }
                    };
                    
                    if (!ble.Adapter.IsScanning)
                    {
                        Console.WriteLine("----------------------------Scan started------------------------------------\n");
                        adapter.ScanMode = ScanMode.LowLatency;
                        LbStatus.Text = "Scaning..";
                        await adapter.StartScanningForDevicesAsync();                        
                    }
                    DevicesListView.ItemsSource = devices;
                    LbStatus.Text = "Rescaning in 30s";
                });

                return true; // runs again, or false to stop
            });
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
            device.Device.UpdateRssiAsync();
            int MeasuredPower = -65;
            int RSSI = device.Device.Rssi;
            int N = 2;
            double one = MeasuredPower - RSSI;
            double two = 10 * N;
            double resutltPartOne = one / two;
            double distance = Math.Pow(10,resutltPartOne);
            if (distance <= 1.5)
            {
                Console.WriteLine("--------Alert--------------------#" + RSSI + "#: " + distance + "-----------------------------------\n");
                SendNotification(device, distance);
                SendVibrateAlert();
            }
            return distance;
        }

        void ShowNotification(string title, string message)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                var msg = new Label()
                {
                    Text = $"Notification Received:\nTitle: {title}\nMessage: {message}"
                };
            });
        }

        void SendNotification(VDevice device,double distance)
        {
            string title = $"1.5m Alert #{device.Device.NativeDevice}";
            string message = $"You are in a danger zone! a 1.5m";
            notificationManager.ScheduleNotification(title, message);
        }
    }
}
