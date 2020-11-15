using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OctopusAgileMonitor.Extensions;
using OctopusAgileMonitor.Models;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Protocol;
using System.Linq;

namespace OctopusAgileMonitor.Services
{

    public class AgileTariffMonitor
    {
        private IMqttClient _mqttClient;
        private readonly String _mqttBroker; 
        private readonly String _apiUrlTemplate = "https://api.octopus.energy/v1/products/{0}/electricity-tariffs/{1}/standard-unit-rates/?period_from={2}&period_to={3}";
        
        private string _tariffCode; 
        private string _productCode; 
        private string _apiKey = "sk_live_cjRZwJzcOFUKtyT4SNFSMUoR";

        private string _nodeName;
        private string _currentRateTopic = "agile/rate";

        private SortedList<DateTime, decimal> _rates = new SortedList<DateTime, decimal>();

        private DateTime _lastRetrieved = DateTime.MinValue;
        private DateTime _lastPublished = DateTime.MinValue;

        private TimeSpan _retrieveInterval = TimeSpan.FromHours(1);
        private TimeSpan _retrieveTimeSpan = TimeSpan.FromHours(3);
        private TimeSpan _publishInterval = TimeSpan.FromMinutes(1);

        private TimeSpan _loopInterval = TimeSpan.FromSeconds(10);

        public AgileTariffMonitor()
        {
            _nodeName = Environment.GetEnvironmentVariable("NODE_NAME");

            String temp = Environment.GetEnvironmentVariable("CURRENT_RATE_TOPIC");

            if(!string.IsNullOrWhiteSpace(temp))
            {
                _currentRateTopic = temp;
            }

            temp = Environment.GetEnvironmentVariable("MQTT_BROKER");

            if(!string.IsNullOrWhiteSpace(temp))
            {
                _mqttBroker = temp;
            }

            temp = Environment.GetEnvironmentVariable("AGILE_TARIFF_CODE");

            if(!string.IsNullOrWhiteSpace(temp))
            {
                _tariffCode = temp;
            }

            temp = Environment.GetEnvironmentVariable("AGILE_PRODUCT_CODE");

            if(!string.IsNullOrWhiteSpace(temp))
            {
                _productCode = temp;
            }

            temp = Environment.GetEnvironmentVariable("AGILE_API_KEY");

            if(!string.IsNullOrWhiteSpace(temp))
            {
                _apiKey = temp;
            }

            temp = Environment.GetEnvironmentVariable("RETRIEVE_INTERVAL_SECONDS");

            if(!string.IsNullOrWhiteSpace(temp))
            {
                _retrieveInterval = TimeSpan.FromSeconds(int.Parse(temp));
            }        
        
            temp = Environment.GetEnvironmentVariable("RETRIEVE_DURATION_HOURS");

            if(!string.IsNullOrWhiteSpace(temp))
            {
                _retrieveTimeSpan = TimeSpan.FromHours(int.Parse(temp));
            }        
        
            temp = Environment.GetEnvironmentVariable("PUBLISH_INTERVAL_SECONDS");

            if(!string.IsNullOrWhiteSpace(temp))
            {
                _publishInterval = TimeSpan.FromSeconds(int.Parse(temp));
            }        
        
            temp = Environment.GetEnvironmentVariable("LOOP_INTERVAL_SECONDS");

            if(!string.IsNullOrWhiteSpace(temp))
            {
                _loopInterval = TimeSpan.FromSeconds(int.Parse(temp));
            }

            IntroduceYourself();        
        }

        private void IntroduceYourself()
        {
            Console.WriteLine("Agile Tariff Monitor is warming up");
            Console.WriteLine($"NODE_NAME: Running on Node {_nodeName}");
            Console.WriteLine($"MQTT_BROKER: MQTT Broker = {_mqttBroker}");
            Console.WriteLine($"CURRENT_RATE_TOPIC: MQTT Topic to Publish = {_currentRateTopic}");
            Console.WriteLine($"AGILE_PRODUCT_CODE: Agile Product Code = {_productCode}");
            Console.WriteLine($"AGILE_TARIFF_CODE: Agile Tariff Code = {_tariffCode}");
            Console.WriteLine("AGILE_API_KEY: Agile API Key = *hidden*");
            Console.WriteLine($"RETRIEVE_INTERVAL_SECONDS: Rates will be retrieved every {_retrieveInterval.TotalSeconds} seconds");
            Console.WriteLine($"RETRIEVE_DURATION_HOURS: We'll fetch {_retrieveTimeSpan.TotalHours} hours of rates at a time");
            Console.WriteLine($"PUBLISH_INTERVAL_SECONDS: The current rate will be published every {_publishInterval.TotalSeconds} seconds");
            Console.WriteLine($"LOOP_INTERVAL_SECONDS: Loop idle time is {_loopInterval.TotalSeconds} seconds");
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
                _mqttClient = factory.CreateMqttClient();

                var clientOptions = new MqttClientOptions
                {
                    ChannelOptions = new MqttClientTcpOptions
                    {
                        Server = _mqttBroker
                    }
                };

                _mqttClient.ConnectedHandler = new MqttClientConnectedHandlerDelegate(async e =>
                {
                    Console.WriteLine("### CONNECTED WITH SERVER ###");
                    await Task.Delay(0);
                });

                _mqttClient.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(async e =>
                {
                    Console.WriteLine("### DISCONNECTED FROM SERVER ###");
                    await Task.Delay(TimeSpan.FromSeconds(5));

                    try
                    {
                        await _mqttClient.ConnectAsync(clientOptions);
                    }
                    catch
                    {
                        Console.WriteLine("### RECONNECTING FAILED ###");
                    }
                });

                try
                {
                    await _mqttClient.ConnectAsync(clientOptions);
                }
                catch (Exception exception)
                {
                    Console.WriteLine("### CONNECTING FAILED ###" + Environment.NewLine + exception);
                }

                while(true)
                {
                    var now = DateTime.UtcNow;

                    if(now > _lastRetrieved + _retrieveInterval)
                    {
                        await RetrieveLatestRates();
                    }

                    if(now > _lastPublished + _publishInterval)
                    {
                        await PublishCurrentRate();
                    }

                    await Task.Delay(_loopInterval);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private async Task RetrieveLatestRates() 
        {
            var from = DateTime.UtcNow;
            var to = from + _retrieveTimeSpan;

            var url = String.Format(_apiUrlTemplate, _productCode, _tariffCode, from.ToIso8601(), to.ToIso8601());

            Console.WriteLine($"Fetching rates from {url}");

            try
            {
                var client = new HttpClient();

                var byteArray = Encoding.ASCII.GetBytes($"{_apiKey}:");

                client.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                var result = await client.GetStringAsync(url);

                Console.Write(result);

                // Parse results
                ParseRates(result);

                _lastRetrieved = DateTime.UtcNow;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void ParseRates(string rateData)
        {
            try
            {
                var rates = JsonSerializer.Deserialize<Rates>(rateData);

                foreach(var result in rates.Results)
                {
                    if(_rates.ContainsKey(result.ValidFrom))
                    {
                        _rates[result.ValidFrom] = result.ValueIncVAT;
                    }
                    else
                    {
                        _rates.Add(result.ValidFrom, result.ValueIncVAT);
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Could not deserialize rates:");
                Console.WriteLine(ex);
                throw;
            }
        }

        private async Task PublishCurrentRate()
        {
            TrimRates();

            var now = DateTime.UtcNow;
            var currentRate = _rates.LastOrDefault(x => x.Key <= now);

            if(!currentRate.Equals(default(KeyValuePair<DateTime,decimal>)))
            {
                Console.WriteLine($"Publishing current rate of {currentRate.Value}");

                var message = new CurrentRate
                                {
                                    Now = now,
                                    Rate = currentRate.Value
                                };

                var applicationMessage = new MqttApplicationMessageBuilder()
                        .WithTopic(_currentRateTopic)
                        .WithPayload(message.Payload)
                        .WithAtLeastOnceQoS()
                        .Build();

                Console.WriteLine("### SENDING APPLICATION MESSAGE ###");
                Console.WriteLine($"+ Topic = {_currentRateTopic}");
                Console.WriteLine($"+ Payload = {message.Payload}");
                Console.WriteLine();

                await _mqttClient.PublishAsync(applicationMessage);
                _lastPublished = now;
            }
            else
            {
                Console.WriteLine("No current rate available!");
            }
            await Task.Delay(0);
        }

        private void TrimRates()
        {
            var now = DateTime.UtcNow;
            bool removed;

            do
            {
                removed = false;
                if(_rates.Count > 1)
                {
                    var firstKey = _rates.Keys[0];
                    var secondKey = _rates.Keys[1];

                    if(secondKey <= now)
                    {
                        _rates.RemoveAt(0);
                        Console.WriteLine($"Purged rate data for {firstKey.ToIso8601()}");
                        removed = true;
                    }
                }
            }
            while(removed);
        }
    }
}