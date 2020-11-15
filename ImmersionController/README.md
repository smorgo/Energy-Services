# ImmersionController

The service is configured using the following environment variables. If you use the supplied deployment YAML (in the root /Deploy folder), you will see the corresponding Kubernetes ConfigMap properties that map to these values.

Kubernetes ConfigMap name: immersion-controller-configmap

Note that in the current implementation, MQTT credentials are not provided to the broker. This will be fixed before long.

|Environment Variable|ConfigMap Key|Description|
|--------------------|-------------|-----------|
|NODE_NAME           |N/A          |Inherited by the Kubernetes host. Not actively used.|
|RATE_MQTT_BROKER    |rate_mqtt_broker|The DNS name (or IP address) of the MQTT broker that will deliver the current rate messages (from the OctopusAgileMonitor service).|
|RELAY_MQTT_BROKER   |relay_mqtt_broker|The DNS name (or IP address) of the MQTT broker that will deliver commands to the Shelley relay. The relay must be configured to use the same broker.|
|CURRENT_RATE_TOPIC  |rate_mqtt_topic|The topic that carries the current rate messages from the OctopusAgileMonitor service.|
|RELAY_CONTROL_TOPIC |relay_mqtt_topic|The topic that carries commands to the Shelley relay. Find this from the relay configuration.|
|ON_THRESHOLD        |on_threshold|A decimal number (as a string). When the current rate falls below this value, the relay is turned on.|
|OFF_THRESHOLD       |off_threshold|A decimal number (as a string). When the current rate rises above this value, the relay is turned off.|
|MINIMUM_STEADY_TIME_SECONDS|minimum_steady_time_seconds|To ensure that the relay does not trigger too quickly, this value is the minimum number of seconds that must elapse between state changes.|

