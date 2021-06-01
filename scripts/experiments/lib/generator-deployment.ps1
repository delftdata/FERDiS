param ([string] $type, [int] $shards)

$yaml = @"
kind: Deployment
apiVersion: apps/v1
metadata:
    namespace: default
    name: generator
    labels:
        app: generator
spec:
    replicas: $shards
    selector:
        matchLabels:
            app: generator
    template:
        metadata:
            labels:
                app: generator
        spec:
            containers:
            - name: generator
              image: mdzwart/benchmarks-net3.1:latest
              env:
                - name: KAFKA_BROKER_DNS_TEMPLATE
                  value: "$($env:KAFKA_BROKER_DNS_TEMPLATE)"
                - name: KAFKA_BROKER_COUNT
                  value: "$($env:KAFKA_BROKER_COUNT)"
                - name: KAFKA_TOPIC_PARTITION_COUNT
                  value: "$($env:KAFKA_TOPIC_PARTITION_COUNT)"
                - name: GENERATOR_TARGET_THROUGHPUT
                  value: "$($env:GENERATOR_TARGET_THROUGHPUT)"
                - name: GENERATOR_CALLS
                  value: "$($env:GENERATOR_CALLS)"
              args: ["$($type)"]
"@

$yaml | Out-File ./generators.yaml