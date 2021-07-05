"""Contains data-retrieval functions (get files from disk)
"""

import os
import pandas as pd


"""Retrieves folders containing experiment data
the function looks up folders at the location base-path.
"""
def get_experiments_at_location(location: str):
    return sorted([e for e in os.listdir(location) if os.path.isdir(os.path.join(location, e))])

"""Retrieves a list of performance data filenames
"""
def get_performance_files(location: str, folder: str):
    return [e for e in os.listdir(f'{location}/{folder}/performance')]

"""Retrieves a list of checkpoint data filenames
"""
def get_checkpoint_files(location: str, folder: str):
    return [e for e in os.listdir(f'{location}/{folder}/checkpoint')]


"""Retrieves a list of recovery data filenames
"""
def get_recovery_files(location: str, folder: str):
    return [e for e in os.listdir(f'{location}/{folder}/recovery')]



"""Reads a throughput file and returns it as an untyped multi-dimensional array
format = timestamp, throughput, latmin, latavg, latmax
"""
def get_throughput_file_content(location: str, folder: str, filename: str):
    frame = pd.read_csv(f'{location}/{folder}/performance/{filename}', sep=", ")
    frame.columns = ['timestamp', 'throughput']
    return frame[frame.timestamp.astype(str).str.strip() != 'timestamp']

"""Reads a latency file and returns it as an untyped multi-dimensional array
format = timestamp, throughput, latmin, latavg, latmax
"""
def get_latency_file_content(location: str, folder: str, filename: str):
    frame = pd.read_csv(f'{location}/{folder}/performance/{filename}', sep=", ")
    frame.columns = ['timestamp', 'latency', 'shard']
    return frame[frame.timestamp.astype(str).str.strip() != 'timestamp']

"""Reads a failures file and returns it as an untyped multi-dimensional array
format = timestamp, throughput, latmin, latavg, latmax
"""
def get_failure_file_content(location: str, folder: str):
    frame = pd.read_csv(f'{location}/{folder}/failures.log', sep=", ")
    frame.columns = ['timestamp']
    return frame[frame.timestamp.astype(str).str.strip() != 'timestamp']

"""Reads a checkpoint file and returns it as an untyped multi-dimensional array
format = timestamp, forced, taken_ms, bytes
"""
def get_checkpoint_file_content(location: str, folder: str, filename: str):
    frame = pd.read_csv(f'{location}/{folder}/checkpoint/{filename}', sep=", ")
    frame.columns = ['timestamp', 'forced', 'taken_ms', 'bytes']
    return frame[frame.timestamp.astype(str).str.strip() != 'timestamp']

"""Reads a recovery file and returns it as an untyped multi-dimensional array
format = timestamp, forced, taken_ms, bytes
"""
def get_recovery_file_content(location: str, folder: str, filename: str):
    frame = pd.read_csv(f'{location}/{folder}/recovery/{filename}', sep=", ")
    frame.columns = ['timestamp', 'restored_ms', 'rollback_ms']
    return frame[frame.timestamp.astype(str).str.strip() != 'timestamp']


"""Reads a init timestamp file and returns it as an untyped multi-dimensional array
format = timestamp, forced, taken_ms, bytes
"""
def get_init_ts(location: str, folder: str):
    ts = open(f'{location}/{folder}/init_timestamp.log', encoding='UTF-8').read()
    return ''.join(c for c in ts if (c.isdigit() or c == ':'))