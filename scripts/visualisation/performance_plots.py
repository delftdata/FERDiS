import json
from typing import List

import pandas as pd
import matplotlib.pyplot as plt
import os
import numpy as np

from lib.data_retriever import get_experiments_at_location, get_performance_files, get_throughput_file_content, get_latency_file_content, get_failure_file_content, get_init_ts
from lib.data_parser import normalize_timestamp_column, parse_throughput_data, parse_latency_data, parse_failures_data, parse_time_to_timestamp, lowpass, savitzky_golay
from lib.plot_builder import Plotter


def produce_throughput_graph(location: str, fromSec: int = 0, toSec: int = 9999):
    for experiment in get_experiments_at_location(location):
        plotter = Plotter()
        plotter.start_plot()
        for perf_file in get_performance_files(location, experiment):
            baseName = perf_file.split("-", 1)[0]
            if(baseName != 'throughput'):
                continue


            parsedData = parse_throughput_data(get_throughput_file_content(location, experiment, perf_file))
            initialTs = parse_time_to_timestamp(get_init_ts(location, experiment))

            data = normalize_timestamp_column(parsedData, initialTs)
            data['throughput'] = savitzky_golay(data['throughput'], 19, 2);
            data['throughput'] = data['throughput'].apply(lambda x: 0 if x < 0 else x)
            data = data[data["timestamp"] > fromSec*1000][data["timestamp"] < toSec*1000]
            data = data.reset_index()
            #print(data)
            #add to plot
            plotter.add_throughput_data(data['timestamp'].apply(lambda x: (x/1000)), data['throughput'], baseName)


            #try:
            #    initialTs = parse_time_to_timestamp(get_init_ts(location, experiment))
            #except Exception as e:
            #    initialTs = parsedData['timestamp'][0]

            #add failure-lines
            failures = parse_failures_data(get_failure_file_content(location, experiment))
            failures['timestamp'] = failures['timestamp'].transform(lambda t: t - initialTs)
            failures = failures[failures["timestamp"] > fromSec*1000][failures["timestamp"] < toSec*1000]
            for index, row in failures.iterrows():
                plotter.add_kill_line(row['timestamp'], "FAILURE")

        #plotter.show_plot(experiment)
        plotter.save_plot(f"{location}/{experiment}-throughput")


def produce_latency_graph(location: str, plotPerShard: bool = False, plotMeanPerTime = False, fromSec: int = 0, toSec: int = 9999):
    for experiment in get_experiments_at_location(location):
        plotter = Plotter()
        plotter.start_plot()
        for perf_file in get_performance_files(location, experiment):
            baseName = perf_file.split("-", 1)[0]
            if(baseName != 'latency'):
                continue

            parsedData = parse_latency_data(get_latency_file_content(location, experiment, perf_file).drop_duplicates())
            
            initialTs = parse_time_to_timestamp(get_init_ts(location, experiment))
            #initialTs = parsedData['timestamp'][0]
            
            data = normalize_timestamp_column(parsedData, initialTs)
            data = data[data["timestamp"] > fromSec*1000][data["timestamp"] < toSec*1000]

            if(plotPerShard):
                #add to plot per shard
                for key, values in data.groupby(["shard"]):
                    values = values.reset_index()
                    values['latency'] = savitzky_golay(values['latency'], 19, 2);
                    plotter.add_latency_data(values['timestamp'].apply(lambda x: x/1000), values['latency'], "shard-"+str(key))
            elif(plotMeanPerTime):
                #add to plot averaged among shards
                data = data.groupby(["timestamp"]).mean()
                data = data.reset_index()
                data['latency'] = savitzky_golay(data['latency'], 19, 2);
                plotter.add_latency_data(data['timestamp'].apply(lambda x: x/1000), data['latency'], baseName)
            else:
                plotter.add_latency_data(data['timestamp'].apply(lambda x: x/1000), data['latency'], baseName)

            #try:
            #    initialTs = parse_time_to_timestamp(get_init_ts(location, experiment))
            #except Exception as e:
            #    initialTs = parsedData['timestamp'][0]

            #add failure-lines
            failures = parse_failures_data(get_failure_file_content(location, experiment))
            failures['timestamp'] = failures['timestamp'].transform(lambda t: t - initialTs)
            failures = failures[failures["timestamp"] > fromSec*1000][failures["timestamp"] < toSec*1000]

            for index, row in failures.iterrows():
                plotter.add_kill_line(row['timestamp'], "FAILURE")

        #plotter.show_plot(experiment)
        plotter.save_plot(f"{location}/{experiment}-latency")
