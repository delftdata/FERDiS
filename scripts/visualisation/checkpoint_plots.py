import json
from typing import List

import pandas as pd
import matplotlib.pyplot as plt
import os
import numpy as np

from lib.data_retriever import get_experiments_at_location, get_checkpoint_files, get_checkpoint_file_content, get_recovery_files, get_recovery_file_content, get_init_ts
from lib.data_parser import parse_checkpoint_data, normalize_timestamp_column, parse_time_to_timestamp,parse_recovery_data
from lib.plot_builder import Plotter

def produce_checkpoint_plot(location: str):
    for experiment in get_experiments_at_location(location):
        plotter = Plotter()
        plotter.start_plot()

        frame = pd.DataFrame()

        for perf_file in get_checkpoint_files(location, experiment):
            instanceName = perf_file.split("-", 1)[0]
            
            print(f"Processing datafile {perf_file} ({instanceName})")
            data = get_checkpoint_file_content(location, experiment, perf_file)
            if(data.empty):
                continue
            #parse & normalize
            data = parse_checkpoint_data(data);
            initialTs = parse_time_to_timestamp(get_init_ts(location, experiment))
            data = normalize_timestamp_column(data, initialTs)
            #print(data)
            #add to plot
            frame = pd.concat([frame, data['bytes']])

        frame.columns = ['bytes']
        print(frame)
        plotter.add_checkpoint_data(frame.apply(lambda b: b/1000))
        plotter.show_plot()
        #plotter.save_plot(f"{location}/{experiment}-checkpoint-size")


def produce_checkpoint_metrics(location: str, experiment: str):
    initialTs = parse_time_to_timestamp(get_init_ts(location, experiment))

    frame = pd.DataFrame()
    for perf_file in get_checkpoint_files(location, experiment):
        instanceName = perf_file.split("-", 1)[0]
        data = get_checkpoint_file_content(location, experiment, perf_file)
        if(data.empty):
            continue
        #parse & normalize
        data = parse_checkpoint_data(data)
        data = normalize_timestamp_column(data, initialTs)
        #print(data)
        #add to plot
        frame = pd.concat([frame, data])
    frame['kbytes'] = (frame['bytes'] / 1000).round(0)

    #frame.columns = ['bytes']
    print("Total checkpoints:", np.count_nonzero(frame['timestamp']))
    print("Forced:", np.count_nonzero(frame['forced'] == True))
    print("Regular:", np.count_nonzero(frame['forced'] == False))

    print("Checkpoint take millis")
    print("1th:", np.percentile(frame['taken_ms'], 1).round(2))
    print("25th:", np.percentile(frame['taken_ms'], 25).round(2))
    print("Mean:", np.percentile(frame['taken_ms'], 50).round(2))
    print("75th:", np.percentile(frame['taken_ms'], 75).round(2))
    print("95th:", np.percentile(frame['taken_ms'], 95).round(2))
    print("99th:", np.percentile(frame['taken_ms'], 99).round(2))

    print("Checkpoint size (kbytes)")
    print("1th:", np.percentile(frame['kbytes'], 1).round(2))
    print("25th:", np.percentile(frame['kbytes'], 25).round(2))
    print("Mean:", np.percentile(frame['kbytes'], 50).round(2))
    print("75th:", np.percentile(frame['kbytes'], 75).round(2))
    print("95th:", np.percentile(frame['kbytes'], 95).round(2))
    print("99th:", np.percentile(frame['kbytes'], 99).round(2))

def produce_recovery_metrics(location: str, experiment: str):
    initialTs = parse_time_to_timestamp(get_init_ts(location, experiment))

    frame = pd.DataFrame()
    total_workers = 0;
    for perf_file in get_recovery_files(location, experiment):
        instanceName = perf_file.split("-", 1)[0]
        total_workers += 1

        data = get_recovery_file_content(location, experiment, perf_file)
        if(data.empty):
            continue
        #parse & normalize
        data = parse_recovery_data(data)
        data = normalize_timestamp_column(data, initialTs)
        #print(data)
        #add to plot
        frame = pd.concat([frame, data])
    total_workers -= 1 #subtract the coordinator
    
    print("Total workers:", total_workers)
    print("Total recovered workers:", np.count_nonzero(frame['timestamp']))
    

    print("Recovery millis")
    print("1th:", np.percentile(frame['restored_ms'], 1).round(2))
    print("25th:", np.percentile(frame['restored_ms'], 25).round(2))
    print("Mean:", np.percentile(frame['restored_ms'], 50).round(2))
    print("75th:", np.percentile(frame['restored_ms'], 75).round(2))
    print("95th:", np.percentile(frame['restored_ms'], 95).round(2))
    print("99th:", np.percentile(frame['restored_ms'], 99).round(2))

    print("Rollback distance millis")
    print("1th:", np.percentile(frame['rollback_ms'], 1).round(2))
    print("25th:", np.percentile(frame['rollback_ms'], 25).round(2))
    print("Mean:", np.percentile(frame['rollback_ms'], 50).round(2))
    print("75th:", np.percentile(frame['rollback_ms'], 75).round(2))
    print("95th:", np.percentile(frame['rollback_ms'], 95).round(2))
    print("99th:", np.percentile(frame['rollback_ms'], 99).round(2))