import json
from typing import List

import pandas as pd
import matplotlib.pyplot as plt
import os
import numpy as np

from lib.data_retriever import get_experiments_at_location, get_performance_files, get_throughput_file_content, get_latency_file_content, get_failure_file_content, get_init_ts
from lib.data_parser import normalize_timestamp_column, parse_throughput_data, parse_latency_data, parse_failures_data, parse_time_to_timestamp, lowpass, savitzky_golay
from lib.plot_builder import Plotter

def produce_throughput_graphs_in_folder(location: str):
    for experiment in get_experiments_at_location(location):
        plotter = Plotter()
        plotter.start_plot()
        produce_throughput_graph(location, experiment, plotter)
        #plotter.show_plot(experiment)
        plotter.save_plot(f"{location}/{experiment}-throughput")


def produce_throughput_compound_graph(location: str, experiments: list):
    plotter = Plotter()
    plotter.start_plot()
    for experiment in experiments:
        produce_throughput_graph(location, experiment[0], plotter, label=experiment[1])
    
    return plotter
    
        

def produce_throughput_graph(location: str, experiment: str, plotter: Plotter, fromSec: int = 0, toSec: int = 9999, label = 'throughput'):
    for perf_file in get_performance_files(location, experiment):
        baseName = perf_file.split("-", 1)[0]
        if(baseName != 'throughput'):
            continue
        parsedData = parse_throughput_data(get_throughput_file_content(location, experiment, perf_file))
        initialTs = parse_time_to_timestamp(get_init_ts(location, experiment))

        data = normalize_timestamp_column(parsedData, initialTs)
        data['throughput'] = savitzky_golay(data['throughput'], 11, 1);
        data['throughput'] = data['throughput'].apply(lambda x: 0 if x < 0 else x)
        data = data[data["timestamp"] > fromSec*1000][data["timestamp"] < toSec*1000]
        data = data.reset_index()
        #add to plot
        plotter.add_throughput_data(data['timestamp'].apply(lambda x: (x/1000)), data['throughput'], label)

        #add failure-lines
        failures = parse_failures_data(get_failure_file_content(location, experiment))
        failures['timestamp'] = failures['timestamp'].transform(lambda t: t - initialTs)
        failures = failures[failures["timestamp"] > fromSec*1000][failures["timestamp"] < toSec*1000]
        for index, row in failures.iterrows():
            plotter.add_kill_line(row['timestamp'], "FAILURE")


def produce_latency_graphs_in_folder(location: str):
    for experiment in get_experiments_at_location(location):
        plotter = Plotter()
        plotter.start_plot()
        produce_latency_graph(location, experiment, plotter, plotPerShard=True)
        #plotter.show_plot(experiment)
        plotter.save_plot(f"{location}/{experiment}-latency")

def produce_latency_compound_graph(location: str, experiments: list):
    plotter = Plotter()
    plotter.start_plot()
    for experiment in experiments:
        produce_latency_graph(location, experiment[0], plotter, label=experiment[1])
    return plotter

def produce_latency_graph(location: str, experiment: str, plotter: Plotter, fromSec: int = 0, toSec: int = 9999, label = 'latency', plotPerShard: bool = False, plotMeanPerTime = False):
    for perf_file in get_performance_files(location, experiment):
        baseName = perf_file.split("-", 1)[0]
        if(baseName != 'latency'):
            continue
        parsedData = parse_latency_data(get_latency_file_content(location, experiment, perf_file).drop_duplicates())
        initialTs = parse_time_to_timestamp(get_init_ts(location, experiment))
        data = normalize_timestamp_column(parsedData, initialTs)
        data = data[data["timestamp"] > fromSec*1000][data["timestamp"] < toSec*1000]
        if(plotPerShard):
            #add to plot per shard
            for key, values in data.groupby(["shard"]):
                values = values.reset_index()
                #values['latency'] = savitzky_golay(values['latency'], 19, 2);
                values['latency'] = values['latency'].apply(lambda x: 0 if x < 0 else x)
                plotter.add_latency_data(values['timestamp'].apply(lambda x: x/1000), values['latency'], str(label) + " shard-"+str(key))
        elif(plotMeanPerTime):
            #add to plot averaged among shards
            data = data.groupby(["timestamp"]).mean()
            data = data.reset_index()
            #data['latency'] = savitzky_golay(data['latency'], 11, 1);
            data['latency'] = data['latency'].apply(lambda x: 0 if x < 0 else x)
            plotter.add_latency_data(data['timestamp'].apply(lambda x: x/1000), data['latency'], label)
        else:
            plotter.add_latency_data(data['timestamp'].apply(lambda x: x/1000), data['latency'], label)

        #add failure-lines
        failures = parse_failures_data(get_failure_file_content(location, experiment))
        failures['timestamp'] = failures['timestamp'].transform(lambda t: t - initialTs)
        failures = failures[failures["timestamp"] > fromSec*1000][failures["timestamp"] < toSec*1000]
        for index, row in failures.iterrows():
            plotter.add_kill_line(row['timestamp'], "FAILURE")


def produce_compound_latency_metrics(location: str, fromSec: int = 0, toSec: int = 9999):
    frame = pd.DataFrame()
    frame['key'] = get_experiments_at_location(location);
    frame['protocol'] = frame['key'].apply(lambda key: key.split('-')[3])
    frame['interval'] = frame['key'].apply(lambda key: key.split('-')[4][:2])
    
    output = pd.DataFrame(columns = ['protocol', 'interval', 'min', 'max', 'mean', '90th', '95th', '99th', 'std var'])
    i = 0
    for _, group in frame.groupby(['protocol', 'interval']):
        latencies = pd.DataFrame()
        for key in group['key']:
            for perf_file in get_performance_files(location, key):
                if(perf_file.split("-", 1)[0] != 'latency'):
                    continue
                new_latencies = parse_latency_data(get_latency_file_content(location, key, perf_file).drop_duplicates())
                initialTs = parse_time_to_timestamp(get_init_ts(location, key))
                new_latencies = normalize_timestamp_column(new_latencies, initialTs)
                new_latencies = new_latencies[new_latencies["timestamp"] > fromSec*1000][new_latencies["timestamp"] < toSec*1000]
                latencies = pd.concat([latencies, new_latencies])
        latencies = latencies.reset_index()        
        output.loc[i] = [group['protocol'].iloc[0], group['interval'].iloc[0], latencies['latency'].min(), latencies['latency'].max(), np.round(latencies['latency'].mean(), 2), latencies['latency'].quantile(.9), latencies['latency'].quantile(.95), latencies['latency'].quantile(.99), np.round(latencies['latency'].std(), 2)]
        i = i + 1
    return output