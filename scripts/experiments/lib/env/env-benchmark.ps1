param ([int] $job, [int] $size)

$env:BENCHMARK_INFRA = "1" # 0 = simulator / 1 cra (fixed cra.. sim for development)
$env:BENCHMARK_JOB = "$($job)" #0-6 (0 = wordcount, 6 = nhop)
$env:BENCHMARK_SIZE = "$($size)" #0-2 (s/m/l)