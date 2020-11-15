# Energy-Services
A variety of microservices (mostly written in C# using .NET Core) for management of energy consumption using Octopus Agile.

I've designed these microservices to run under Kubernetes. I run them on a 10-node Raspberry Pi cluster. Each service is configured through environment variables and I've created Kubernetes ConfigMaps so that I don't need to include potentially sensitive data in the deployment YAML files. Be sure to read the corresponding README.md for each of the services.

The services are loosely-coupled and generally communicate with each over over MQTT. You'll need an MQTT broker to handle these messages. I run Mosquitto directly on my cluster.

## [OctopusAgileMonitor](/OctopusAgileMonitor)
This service is designed to poll the Octopus Agile Rate API and then publish the real-time current rate via an MQTT message. By default, it sends one message per minute, even though the rate only changes every thirty minutes. This is intended to improve reliability, in case messages are lost or services are restarted.

## [ImmersionController](/ImmersionController)
I designed this service to automatically control my electric water heater, turning it on when the electricity rate drops before a threshold value (usually 0p/kWh) and turning it off when the rate rises again.
The service is more generic than it sounds. It drives a Shelley 1PM WiFi relay and warrants being renamed.

## Deployment
The Dockerfiles included are to build 32-bit ARM containers, which will run under Kubernetes (actually, k3s). If you're targeting a different platform, you'll likely need different Dockerfiles.

The [Deploy](/Deploy) folder contains the YAML files for deploying to a cluster. 
Apply the files in the following order:
* Namespace.yaml
* OctopusAgileMonitorConfigMap.yaml
* OctopusAgileMonitor.yaml
* ImmersionControllerConfigMap.yaml
* ImmersionController.yaml

