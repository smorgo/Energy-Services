using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ImmersionController.Models;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Protocol;
using System.Linq;

namespace ImmersionController.Services
{

    public class RateMonitor
    {
        private enum ImmersionState
        {
            Unknown = -1,
            Off = 0,
            On = 2
        }

        private IMqttClient _mqttRateClient;
        private Shelly1PM _immersionHeater;
        private readonly String _mqttRateBroker; 
        private readonly String _mqttRelayBroker; 
        private string _nodeName;
        private string _currentRateTopic = "agile/rate";
        private string _relayControlTopic;
        private decimal _onThreshold = 0m;
        private decimal? _offThreshold = null;
        private DateTime _previousRateTime = DateTime.MinValue;
        private DateTime _previousStateChange = DateTime.MinValue;
        private TimeSpan _minimumStateInterval = TimeSpan.FromMinutes(5);
        private ImmersionState _currentState = ImmersionState.Unknown;
        public RateMonitor()
        {
            _nodeName = Environment.GetEnvironmentVariable("NODE_NAME");

            String temp = Environment.GetEnvironmentVariable("CURRENT_RATE_TOPIC");

            if(!string.IsNullOrWhiteSpace(temp))
            {
                _currentRateTopic = temp;
            }

            temp = Environment.GetEnvironmentVariable("RATE_MQTT_BROKER");

            if(!string.IsNullOrWhiteSpace(temp))
            {
                _mqttRateBroker = temp;
            }

            temp = Environment.GetEnvironmentVariable("RELAY_MQTT_BROKER");

            if(!string.IsNullOrWhiteSpace(temp))
            {
                _mqttRelayBroker = temp;
            }
        
            temp = Environment.GetEnvironmentVariable("RELAY_CONTROL_TOPIC");

            if(!string.IsNullOrWhiteSpace(temp))
            {
                _relayControlTopic = temp;
            }        
        
            temp = Environment.GetEnvironmentVariable("ON_THRESHOLD");

            if(!string.IsNullOrWhiteSpace(temp))
            {
                _onThreshold = decimal.Parse(temp);
            }        
        
            temp = Environment.GetEnvironmentVariable("OFF_THRESHOLD");

            if(!string.IsNullOrWhiteSpace(temp))
            {
                _offThreshold = decimal.Parse(temp);
            }        

            temp = Environment.GetEnvironmentVariable("MINIMUM_STEADY_TIME_SECONDS");

            if(!string.IsNullOrWhiteSpace(temp))
            {
                _minimumStateInterval = TimeSpan.FromSeconds(int.Parse(temp));
            }        

            IntroduceYourself();        
        }

        private void IntroduceYourself()
        {
            Console.WriteLine("Immersion Controller is warming up");
            Console.WriteLine($"NODE_NAME: Running on Node {_nodeName}");
            Console.WriteLine($"RATE_MQTT_BROKER: MQTT Broker = {_mqttRateBroker}");
            Console.WriteLine($"CURRENT_RATE_TOPIC: MQTT Topic to Publish = {_currentRateTopic}");
            Console.WriteLine($"RELAY_MQTT_BROKER: MQTT Broker = {_mqttRelayBroker}");
            Console.WriteLine($"RELAY_CONTROL_TOPIC: MQTT Topic to Publish = {_relayControlTopic}");
            Console.WriteLine($"ON_THRESHOLD: Immersion will turn on when rate is below {_onThreshold}p/kWh");
            Console.WriteLine($"OFF_THRESHOLD: Immersion will turn off when rate is above {(_offThreshold.HasValue ? _offThreshold.Value : _onThreshold)}p/kWh");
            Console.WriteLine($"MINIMUM_STEADY_TIME_SECONDS: State will not change more than once every {_minimumStateInterval.TotalSeconds} seconds");
        }
        public void Run() 
        {
            Task.Run(() => RunAsync()).Wait();
        }

        public async Task RunAsync()
        {
            try
            {
                MqttNetConsoleLogger.ForwardToConsole();

                var factory = new MqttFactory();

                _immersionHeater = new Shelly1PM(factory, _mqttRelayBroker, _relayControlTopic);
                await _immersionHeater.ConnectAsync();

                _mqttRateClient = factory.CreateMqttClient();

                var rateClientOptions = new MqttClientOptions
                {
                    ChannelOptions = new MqttClientTcpOptions
                    {
                        Server = _mqttRateBroker
                    }
                };

                _mqttRateClient.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(async e =>
                {
                    Console.WriteLine("### RECEIVED APPLICATION MESSAGE ###");
                    Console.WriteLine($"+ Topic = {e.ApplicationMessage.Topic}");

                    var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

                    Console.WriteLine($"+ Payload = {payload}");
                    Console.WriteLine($"+ QoS = {e.ApplicationMessage.QualityOfServiceLevel}");
                    Console.WriteLine($"+ Retain = {e.ApplicationMessage.Retain}");
                    Console.WriteLine();

                    var rateMessage = RateMessage.TryParse(payload);

                    if(rateMessage != null)
                    {
                        if(rateMessage.Time > _previousRateTime)
                        {
                            // Use rate
                            Console.WriteLine($"New rate: {rateMessage.Rate}p/kWh");
                            
                            var now = DateTime.UtcNow;
                            var offThreshold = _offThreshold.HasValue ? _offThreshold.Value : _onThreshold;

                            if(rateMessage.Rate < _onThreshold)
                            {
                                if(_currentState != ImmersionState.On || now > _previousStateChange + _minimumStateInterval)
                                {
                                    Console.WriteLine("Turn Immersion Heater ON");
                                    await _immersionHeater.TurnOnAsync();
                                    _previousStateChange = now;
                                    _currentState = ImmersionState.On;
                                }
                            }
                            else if(rateMessage.Rate > offThreshold)
                            {
                                if(_currentState != ImmersionState.Off || now > _previousStateChange + _minimumStateInterval)
                                {
                                    Console.WriteLine("Turn Immersion Heater OFF");
                                    await _immersionHeater.TurnOffAsync();
                                    _previousStateChange = now;
                                    _currentState = ImmersionState.Off;
                                }
                            }

                            _previousRateTime = rateMessage.Time;
                        }
                    }

                    await Task.Delay(0);
                });

                _mqttRateClient.ConnectedHandler = new MqttClientConnectedHandlerDelegate(async e =>
                {
                    Console.WriteLine("### RATE-CLIENT: CONNECTED WITH SERVER ###");
                
                    await _mqttRateClient.SubscribeAsync(
                    new MqttTopicFilter 
                    { 
                        Topic = _currentRateTopic, 
                        QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce 
                    });
                });

                _mqttRateClient.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(async e =>
                {
                    Console.WriteLine("### RATE-CLIENT: DISCONNECTED FROM SERVER ###");
                    await Task.Delay(TimeSpan.FromSeconds(5));

                    try
                    {
                        await _mqttRateClient.ConnectAsync(rateClientOptions);
                    }
                    catch
                    {
                        Console.WriteLine("### RATE-CLIENT: RECONNECTING FAILED ###");
                    }
                });

                try
                {
                    await _mqttRateClient.ConnectAsync(rateClientOptions);
                }
                catch (Exception exception)
                {
                    Console.WriteLine("### RATE-CLIENT: CONNECTING FAILED ###" + Environment.NewLine + exception);
                }

                Console.WriteLine("### WAITING FOR APPLICATION MESSAGES ###");
                

                
                SpinWait.SpinUntil(() => false);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }
    }
}