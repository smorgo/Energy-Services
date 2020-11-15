# OctopusAgileMonitor
This service is designed to poll the Octopus Agile Rate API and then publish the real-time current rate via an MQTT message. By default, it sends one message per minute, even though the rate only changes every thirty minutes. This is intended to improve reliability, in case messages are lost or services are restarted.
