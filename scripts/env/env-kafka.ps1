param ([string] $template, [int] $brokers)

$env:KAFKA_BROKER_DNS_TEMPLATE = "$($template)"
$env:KAFKA_BROKER_COUNT = "$($brokers)"