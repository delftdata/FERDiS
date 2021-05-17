$yaml = @"
kind: Deployment
apiVersion: apps/v1
metadata:
    namespace: default
    name: latency-logger
    labels:
        app: latency-logger
spec:
    replicas: 1
    selector:
        matchLabels:
            app: latency-logger
    template:
        metadata:
            labels:
                app: latency-logger
        spec:
            containers:
            - name: latency-logger
              image: mdzwart/benchmarks-net3.1:latest
              env:
              - name: AZURE_STORAGE_CONN_STRING
                value: "$($env:AZURE_STORAGE_CONN_STRING)"
              - name: KAFKA_BROKER_DNS_TEMPLATE
                value: "$($env:KAFKA_BROKER_DNS_TEMPLATE)"
              - name: KAFKA_BROKER_COUNT
                value: "$($env:KAFKA_BROKER_COUNT)"
              - name: LOG_EVENT_LEVEL
                value: "$($env:LOG_EVENT_LEVEL)"
              - name: LOG_TARGET_FLAGS
                value: "$($env:LOG_TARGET_FLAGS)"  
              args: ["latency"]
---
kind: Deployment
apiVersion: apps/v1
metadata:
    namespace: default
    name: throughput-logger
    labels:
        app: throughput-logger
spec:
    replicas: 1
    selector:
        matchLabels:
            app: throughput-logger
    template:
        metadata:
            labels:
                app: throughput-logger
        spec:
            containers:
            - name: throughput-logger
              image: mdzwart/benchmarks-net3.1:latest
              env:
              - name: AZURE_STORAGE_CONN_STRING
                value: "$($env:AZURE_STORAGE_CONN_STRING)"
              - name: KAFKA_BROKER_DNS_TEMPLATE
                value: "$($env:KAFKA_BROKER_DNS_TEMPLATE)"
              - name: KAFKA_BROKER_COUNT
                value: "$($env:KAFKA_BROKER_COUNT)"
              - name: LOG_EVENT_LEVEL
                value: "$($env:LOG_EVENT_LEVEL)"
              - name: LOG_TARGET_FLAGS
                value: "$($env:LOG_TARGET_FLAGS)"  
              args: ["throughput"]
"@

$yaml | Out-File ./metric-loggers.yaml