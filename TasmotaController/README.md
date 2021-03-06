# TasmotaController

You can find the container image on [Docker Hub](https://hub.docker.com/repository/docker/smorgo/tasmotacontroller).

This service communicates with a smart switch running the Tasmota firmware using MQTT. The switch must be configured (via the web interface) to use MQTT. 

The service is configured using the following environment variables. If you use the supplied deployment YAML (in the /Deploy folder), you will see the corresponding Kubernetes ConfigMap properties that map to these values.

Kubernetes ConfigMap name: tasmota-controller-configmap

Note that in the current implementation, MQTT credentials are not provided to the broker. This will be fixed before long.

|Environment Variable|ConfigMap Key|Description|
|--------------------|-------------|-----------|
|NODE_NAME           |             |Inherited by the Kubernetes host. Not actively used.|
|RATE_MQTT_BROKER    |rate_mqtt_broker|The DNS name (or IP address) of the MQTT broker that will deliver the current rate messages (from the OctopusAgileMonitor service).|
|RELAY_MQTT_BROKER   |relay_mqtt_broker|The DNS name (or IP address) of the MQTT broker that will deliver commands to the device. The relay must be configured to use the same broker.|
|CURRENT_RATE_TOPIC  |rate_mqtt_topic|The topic that carries the current rate messages from the OctopusAgileMonitor service. Default value is "agile/rate".|
|RELAY_CONTROL_TOPIC |relay_mqtt_topic|The topic that carries commands to the device. Find this from the device configuration.|
|ON_THRESHOLD        |on_threshold|A decimal number (as a string). When the current rate falls below this value, the switch is turned on. Default is 0p/kWh.|
|OFF_THRESHOLD       |off_threshold|A decimal number (as a string). When the current rate rises above this value, the switch is turned off. Default is to use the value of ON_THRESHOLD.|
|MINIMUM_STEADY_TIME_SECONDS|minimum_steady_time_seconds|An integer (as a string). This value is the minimum number of seconds that must elapse between state changes. It is intended to ensure that the relay is not toggled too quickly. Default is 300 (5 minutes)|

