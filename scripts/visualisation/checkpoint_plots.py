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


    
def produce_compound_checkpoint_metrics(location: str):
    output = pd.DataFrame(columns = ['query', 'protocol', 'interval', '#total', '#regular', '#forced', 'size (Kb)', 'time (ms)'])
    i = 0
    for query_folder in get_experiments_at_location(location):
        query_folder_path = os.path.join(location, query_folder)
        
        print(query_folder)
        frame = pd.DataFrame()
        frame['key'] = get_experiments_at_location(query_folder_path);
        frame['protocol'] = frame['key'].apply(lambda key: key.split('-')[3])
        frame['interval'] = frame['key'].apply(lambda key: key.split('-')[4][:2])
        for _, group in frame.groupby(['protocol', 'interval']):
            checkpoints = pd.DataFrame()
            group_size = 0
            for key in group['key']:
                checkpoints = group_checkpoint_data(query_folder_path, key, checkpoints)
                group_size += 1
            checkpoints = checkpoints.reset_index()        

            cp_total = round(np.count_nonzero(checkpoints['timestamp'])/group_size)
            cp_regular = round(np.count_nonzero(checkpoints['forced'] == False)/group_size)
            cp_forced = cp_total - cp_regular
            size_mean = round(checkpoints['kbytes'].mean())
            size_stdef = round(checkpoints['kbytes'].std())
            time_mean = round(checkpoints['taken_ms'].mean())
            time_stdef = round(checkpoints['taken_ms'].std())
            output.loc[i] = [str(query_folder), group['protocol'].iloc[0], group['interval'].iloc[0], cp_total, cp_regular, cp_forced, str(size_mean) + " ± " + str(size_stdef), str(time_mean) + " ± " + str(time_stdef)]
            i += 1
    return output

def group_checkpoint_data(location: str, experiment: str, frame: pd.DataFrame):
    initialTs = parse_time_to_timestamp(get_init_ts(location, experiment))
    for perf_file in get_checkpoint_files(location, experiment):
        instanceName = perf_file.split("-", 1)[0]
        data = get_checkpoint_file_content(location, experiment, perf_file)
        if(data.empty):
            continue
        #parse & normalize
        data = parse_checkpoint_data(data)
        data = normalize_timestamp_column(data, initialTs)
        frame = pd.concat([frame, data])
    frame['kbytes'] = (frame['bytes'] / 1000).round(0)
    return frame


def produce_compound_recovery_metrics(location: str):
    output = pd.DataFrame(columns = ['query', 'protocol', 'interval', '#total', '#recovered', 'recovery (ms)', 'restore (ms)', 'rollback (s)'])
    i = 0
    for query_folder in get_experiments_at_location(location):
        query_folder_path = os.path.join(location, query_folder)
    
        frame = pd.DataFrame()
        frame['key'] = get_experiments_at_location(query_folder_path);
        frame['protocol'] = frame['key'].apply(lambda key: key.split('-')[3])
        frame['interval'] = frame['key'].apply(lambda key: key.split('-')[4][:2])
        for _, group in frame.groupby(['protocol', 'interval']):
            recovery = pd.DataFrame()
            group_size = 0
            for key in group['key']:
                recovery = pd.concat([recovery, group_recovery_data(query_folder_path, key)])
                group_size += 1
            recovery = recovery.reset_index()        
            recovery['rollback_s'] = recovery['rollback_ms'].apply(lambda ms: ms/1000);
            #compute statistics
            total_workers = round(recovery['total_workers'].mean())
            recovered_workers = round(recovery['recovered_workers'].mean())
            recovery_mean = float('nan') #TODO: calculate somehow..
            restore_mean = round(recovery['restored_ms'].mean())
            restore_stdev = round(recovery['restored_ms'].std())
            rollback_mean = round(recovery['rollback_s'].mean(),2)
            rollback_stdev = round(recovery['rollback_s'].std(),2)
            output.loc[i] = [str(query_folder), group['protocol'].iloc[0], group['interval'].iloc[0], total_workers, recovered_workers, recovery_mean, str(restore_mean) + " ± " + str(restore_stdev), str(rollback_mean) + " ± " + str(rollback_stdev)]
            i += 1
    return output

def group_recovery_data(location: str, experiment: str):
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
        #add to plot
        frame = pd.concat([frame, data])
    total_workers -= 1 #subtract the coordinator
    

    if(frame.empty):
        print("no recovery from " + str(experiment))

    frame['total_workers'] = total_workers
    frame['recovered_workers'] = recovered_workers = np.count_nonzero(frame['timestamp'])
    if(recovered_workers > 24):
        print("double failure in " + experiment)

    return frame

    #print("Total workers:", total_workers)
    #print("Total recovered:", np.count_nonzero(frame['timestamp']))
    #print("Restore ms")
    #print("1th:", np.percentile(frame['restored_ms'], 1).round(2))
    #print("25th:", np.percentile(frame['restored_ms'], 25).round(2))
    #print("50th:", np.percentile(frame['restored_ms'], 50).round(2))
    #print("75th:", np.percentile(frame['restored_ms'], 75).round(2))
    #print("95th:", np.percentile(frame['restored_ms'], 95).round(2))
    #print("99th:", np.percentile(frame['restored_ms'], 99).round(2))
    #print("Rollback distance ms")
    #print("1th:", np.percentile(frame['rollback_ms'], 1).round(2))
    #print("25th:", np.percentile(frame['rollback_ms'], 25).round(2))
    #print("50th:", np.percentile(frame['rollback_ms'], 50).round(2))
    #print("75th:", np.percentile(frame['rollback_ms'], 75).round(2))
    #print("95th:", np.percentile(frame['rollback_ms'], 95).round(2))
    #print("99th:", np.percentile(frame['rollback_ms'], 99).round(2))