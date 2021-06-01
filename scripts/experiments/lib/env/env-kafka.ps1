param ([string] $template, [int] $brokers, [int] $partitions)

$env:KAFKA_BROKER_DNS_TEMPLATE = "$($template)"
$env:KAFKA_BROKER_COUNT = "$($brokers)"
$env:KAFKA_TOPIC_PARTITION_COUNT = "$($partitions)"