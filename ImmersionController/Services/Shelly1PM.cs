using System;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Protocol;

namespace ImmersionController.Services
{
    public class Shelly1PM
    {
        private IMqttFactory _factory;
        private string _broker;
        private readonly string _controlTopic;
        private IMqttClient _client;
        public Shelly1PM(IMqttFactory factory, string broker, string controlTopic)
        {
            _factory = factory;
            _broker = broker;
            _controlTopic =  controlTopic;

        }
       public async Task ConnectAsync()
        {
            _client = _factory.CreateMqttClient();

            var clientOptions = new MqttClientOptions
            {
                ChannelOptions = new MqttClientTcpOptions
                {
                    Server = _broker
                }
            };

            _client.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(async e =>
            {
                Console.WriteLine("### RECEIVED MESSAGE FROM SHELLY 1PM ###");
                Console.WriteLine($"+ Topic = {e.ApplicationMessage.Topic}");

                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

                Console.WriteLine($"+ Payload = {payload}");
                Console.WriteLine($"+ QoS = {e.ApplicationMessage.QualityOfServiceLevel}");
                Console.WriteLine($"+ Retain = {e.ApplicationMessage.Retain}");
                Console.WriteLine();

                await Task.Delay(0);
            });

            _client.ConnectedHandler = new MqttClientConnectedHandlerDelegate(async e =>
            {
                Console.WriteLine("### RELAY-CLIENT: CONNECTED WITH SERVER ###");
                await Task.Delay(0);
            });

            _client.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(async e =>
            {
                Console.WriteLine("### RELAY-CLIENT: DISCONNECTED FROM SERVER ###");
                await Task.Delay(TimeSpan.FromSeconds(5));

                try
                {
                    await _client.ConnectAsync(clientOptions);
                }
                catch
                {
                    Console.WriteLine("### RELAY-CLIENT: RECONNECTING FAILED ###");
                }
            });

            try
            {
                await _client.ConnectAsync(clientOptions);
            }
            catch (Exception exception)
            {
                Console.WriteLine("### RELAY-CLIENT: CONNECTING FAILED ###" + Environment.NewLine + exception);
            }
        }

        public async Task TurnOnAsync()
        {
            await SendCommand("on");
        }

        public async Task TurnOffAsync()
        {
            await SendCommand("off");
        }

        private async Task SendCommand(string command)
        {
            var applicationMessage = new MqttApplicationMessageBuilder()
                                    .WithTopic(_controlTopic)
                                    .WithPayload(command)
                                    .WithAtLeastOnceQoS()
                                    .Build();

            Console.WriteLine("### SENDING APPLICATION MESSAGE ###");
            Console.WriteLine($"+ Topic = {_controlTopic}");
            Console.WriteLine($"+ Payload = {command}");
            Console.WriteLine();

            await _client.PublishAsync(applicationMessage);
        }
    }
}