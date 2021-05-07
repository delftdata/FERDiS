param ([int] $throughput, [int] $gencalls)


$env:GENERATOR_TARGET_THROUGHPUT = "$($throughput)" # per second
$env:GENERATOR_CALLS = "$($gencalls)" # nexmark stuff