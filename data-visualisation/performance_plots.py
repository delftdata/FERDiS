import json
from typing import List

import pandas as pd
import matplotlib.pyplot as plt
import os
import numpy as np

from lib.data_retriever import get_experiments_at_location, get_performance_files, get_throughput_file_content, get_latency_file_content, get_failure_file_content, get_init_ts
from lib.data_parser import normalize_timestamp_column, parse_throughput_data, parse_latency_data, parse_failures_data, parse_time_to_timestamp
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
            initialTs = parsedData['timestamp'][0]


            data = normalize_timestamp_column(parsedData, initialTs)
            data = data[data["timestamp"] > fromSec*1000][data["timestamp"] < toSec*1000]

            #print(data)
            #add to plot
            plotter.add_throughput_data(data['timestamp'].apply(lambda x: (x/1000)), data['throughput'], baseName)


            try:
                initialTs = parse_time_to_timestamp(get_init_ts(location, experiment))
                print(experiment)
            except Exception as e:
                print(experiment, "fail", e)
                initialTs = parsedData['timestamp'][0]
            #add failure-lines
            failures = parse_failures_data(get_failure_file_content(location, experiment))
            failures['timestamp'] = failures['timestamp'].transform(lambda t: t - initialTs)
            failures = failures[failures["timestamp"] > fromSec*1000][failures["timestamp"] < toSec*1000]
            for index, row in failures.iterrows():
                plotter.add_kill_line(row['timestamp'], "FAILURE")

        #plotter.show_plot(experiment)
        plotter.save_plot(f"{location}/{experiment}-throughput")


def produce_latency_graph(location: str, fromSec: int = 0, toSec: int = 9999):
    for experiment in get_experiments_at_location(location):
        plotter = Plotter()
        plotter.start_plot()
        for perf_file in get_performance_files(location, experiment):
            baseName = perf_file.split("-", 1)[0]
            if(baseName != 'latency'):
                continue

            parsedData = parse_latency_data(get_latency_file_content(location, experiment, perf_file).drop_duplicates())
            
            initialTs = parsedData['timestamp'][0]
            
            data = normalize_timestamp_column(parsedData, initialTs)
            data = data[data["timestamp"] > fromSec*1000][data["timestamp"] < toSec*1000]

            #add to plot
            plotter.add_latency_data(data['timestamp'].apply(lambda x: x/1000), data['latency'], baseName)

            try:
                initialTs = parse_time_to_timestamp(get_init_ts(location, experiment))
                print(experiment)
            except Exception as e:
                print(experiment, "fail", e)
                initialTs = parsedData['timestamp'][0]

            #add failure-lines
            failures = parse_failures_data(get_failure_file_content(location, experiment))
            failures['timestamp'] = failures['timestamp'].transform(lambda t: t - initialTs)
            failures = failures[failures["timestamp"] > fromSec*1000][failures["timestamp"] < toSec*1000]

            for index, row in failures.iterrows():
                plotter.add_kill_line(row['timestamp'], "FAILURE")

        #plotter.show_plot(experiment)
        plotter.save_plot(f"{location}/{experiment}-latency")
