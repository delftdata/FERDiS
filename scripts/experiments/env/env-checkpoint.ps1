param ([int] $mode, [int] $interval)


$env:CHECKPOINT_COORDINATION_MODE = "$($mode)" #0 = uc, 1 = cc, 2 = cic
$env:CHECKPOINT_INTERVAL_SECONDS = "$($interval)"