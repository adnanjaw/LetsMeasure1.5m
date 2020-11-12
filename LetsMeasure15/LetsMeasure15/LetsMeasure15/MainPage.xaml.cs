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
        readonly ObservableCollection<IDevice> devices;
        readonly List<String> DeviceType;
        readonly INotificationManager notificationManager;
        
        public MainPage()
        {
            InitializeComponent();
            devices = new ObservableCollection<IDevice>();
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
                        if (!devices.Contains(device) && !FilterDevices(device))
                        {
                            devices.Add(device);
                            Console.WriteLine("----------------------------Device Added------------------------------------\n");
                            CalculateDistance(device);
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
                    LbStatus.Text = "Rescaning in 20s";
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

        private bool FilterDevices(IDevice device)
        {
            if (String.IsNullOrEmpty(device.Name))
                return false;

            foreach (string type in DeviceType)
            {
                if (device.Name.Contains(type))
                {
                    return true;
                }
            }
            
            return false;
        }

        private void CalculateDistance(IDevice device)
        {
            device.UpdateRssiAsync();
            int MeasuredPower = -69;
            int RSSI = device.Rssi;
            int N = 2;
            double distance = Math.Pow(10, (MeasuredPower - (RSSI)) / (10 * N));
            Console.WriteLine("----------------------------" + RSSI + ": " + distance + "-----------------------------------\n");
            if (distance <= 1.5)
            {
                SendNotification(device, distance);
                SendVibrateAlert();
            }
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

        void SendNotification(IDevice device,double distance)
        {
            string title = $"1.5m Alert #{distance} #{device.NativeDevice}";
            string message = $"You are in a danger zone! \n please keep a 1.5m distance";
            notificationManager.ScheduleNotification(title, message);
        }
    }
}
