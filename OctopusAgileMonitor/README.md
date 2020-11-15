# OctopusAgileMonitor
This service is designed to poll the Octopus Agile Rate API and then publish the real-time current rate via an MQTT message. By default, it sends one message per minute, even though the rate only changes every thirty minutes. This is intended to improve reliability, in case messages are lost or services are restarted.

The service is configured using the following environment variables. If you use the supplied deployment YAML (in the /Deploy folder), you will see the corresponding Kubernetes ConfigMap properties that map to these values.

Kubernetes ConfigMap name: octopus-agile-monitor-configmap

Note that in the current implementation, MQTT credentials and port are not not used. This will be fixed before long.

|Environment Variable|ConfigMap Key|Description|
|--------------------|-------------|-----------|
|NODE_NAME           |             |Inherited by the Kubernetes host. Not actively used.|
|MQTT_BROKER    |mqtt_broker|The DNS name (or IP address) of the MQTT broker that will deliver the current rate messages (from the OctopusAgileMonitor service).|
|CURRENT_RATE_TOPIC  |mqtt_topic|The topic that the service will publish rate messages to. Default value is "agile/rate".|
|AGILE_PRODUCT_CODE |agile_product_code|The product code for your Agile tariff. You can find this via your Octopus account. At the time of writing, this should be "AGILE-18-02-21", which is the default value.|
|AGILE_TARIFF_CODE |agile_tariff_code|The specific tariff that applies to your location. You can find this from your Octopus account. The default value is "E-1R-AGILE-18-02-21-E", which is for the West Midlands.|
|AGILE_API_KEY |agile_api_key|Your API access key that acts as a credential for retrieving data from Octopus. Find this via your Octopus account.|
|RETRIEVE_INTERVAL_SECONDS|retrieve_interval_seconds|An integer (as a string). This is the number of seconds that we wait between calls to the Octopus Agile API to fetch rate data. The default is 3600 (1 hour).|
|RETRIEVE_DURATION_HOURS|retrieve_duration_hours|An integer (as a string). This is the number of hours' worth of rate data that we fetch at a time. By fetching more than the minimum amount of data, we help protect against failures, such as network errors. Default is 3 hours.|
|PUBLISH_INTERVAL_SECONDS|publish_interval_seconds|An integer (as a string). This is the interval, in seconds, between current rate messages that we publish to the MQTT topic. Default is 60 (1 minute).|
|LOOP_INTERVAL_SECONDS|loop_interval_seconds|An integer (as a string). We have a loop that pulls data from Octopus and publishes data to the MQTT broker. This value is the time in seconds that we wait before looping around again. It does not directly affect the amount of communication, but it used to balance CPU consumption with latency. Default is 10 seconds.|

