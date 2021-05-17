param ([int] $infra, [int] $job, [int] $size)

$env:BENCHMARK_INFRA = "$($infra)" # 0 = simulator / 1 cra
$env:BENCHMARK_JOB = "$($job)" #0-5 (0 = wordcount, 5 = nhop)
$env:BENCHMARK_SIZE = "$($size)" #0-2 (s/m/l)