param ([int] $targetflags, [int] $level)


$env:LOG_TARGET_FLAGS = "$($targetflags)" # 1 console / 2 file / 4 azure blob
$env:LOG_EVENT_LEVEL = "$($level)" #0-5 (0 = verbose, 5 = fatal)