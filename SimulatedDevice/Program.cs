namespace SimulatedDevice
{
    using System;
    using System.Collections.Generic;
    using System.IO.Ports;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Newtonsoft.Json;

    public class Program
    {
        private const string IotHubUri = "";
        private const string DeviceKey = "";
        private const string DeviceId = "";

        private static DeviceClient _deviceClient;
        private static int _messageId = 1;

        private static async void SendDeviceToCloudMessagesAsync()
        {

            var serial = new SerialPort
            {
                BaudRate = 9600,
                PortName = "COM5",
                DtrEnable = true,
                RtsEnable = true
            };

            serial.DataReceived += Serial_DataReceived;
            serial.Open();
        }

        private static void Serial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string indata = sp.ReadLine();

            var lines = indata.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var item in lines)
            {
                try
                {

                    var values = item.Split(';');

                    if (values.Length != 2) continue;

                    var Humidity = Convert.ToDouble(values[1]);
                    var Temperature = Convert.ToDouble(values[0]);

                    if (Humidity < 10 || Temperature < 10)
                        continue;

                    var value = new Value
                    {
                        Humidity = Humidity,
                        Temperature = Temperature,
                    };

                    SendAsync(value).Wait();

                }
                catch (Exception)
                {
                }
            }

        }

        private static async Task SendAsync(Value value)
        {
            var telemetryDataPoint = new
            {
                messageId = _messageId++,
                deviceId = DeviceId,
                temperature = value.Temperature,
                humidity = value.Humidity
            };

            var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
            var message = new Message(Encoding.ASCII.GetBytes(messageString));

            try
            {
                await _deviceClient.SendEventAsync(message);
            }
            catch (Exception ex)
            {

                Console.WriteLine("{0} > Error: {1}", DateTime.Now, ex.Message);
            }

            Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);
        }

        public class Value
        {
            public double Temperature { get; set; }
            public double Humidity { get; set; }
        }


        private static void Main(string[] args)
        {
            Console.WriteLine("Simulated device\n");
            _deviceClient = DeviceClient.Create(IotHubUri, new DeviceAuthenticationWithRegistrySymmetricKey(DeviceId, DeviceKey), TransportType.Mqtt);
            _deviceClient.ProductInfo = "HappyPath_Simulated-CSharp";

            SendDeviceToCloudMessagesAsync();
            Console.ReadLine();
        }
    }
}
