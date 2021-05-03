import json
from typing import List

import pandas as pd
import matplotlib.pyplot as plt
import os
import numpy as np

from lib.data_retriever import get_experiments_at_location, get_performance_files, get_performance_file_content
from lib.data_parser import parse_performance_data, normalize_timestamp_column
from lib.plot_builder import Plotter


def produce_throughput_graph(location: str):
    for experiment in get_experiments_at_location(location):
        plotter = Plotter()
        plotter.start_plot()
        for perf_file in get_performance_files(location, experiment):
            instanceName = perf_file.split("-", 1)[0]
            
            #TODO: check if this is ok?
            if(instanceName.endswith(("09", "10", "11")) is False):
                continue #Test: only consider sinks?
            
            print(f"Processing datafile {perf_file} ({instanceName})")
            data = get_performance_file_content(location, experiment, perf_file)
            if(data.empty):
                continue
            #parse & normalize
            data = normalize_timestamp_column(
                parse_performance_data(data)
            )
            #print(data)
            #add to plot
            plotter.add_throughput_data(data['timestamp'], data['throughput'], instanceName)
            #add kill-line if necessary
            if(instanceName == 'crainst02'):
                fakeindex = round(len(data['timestamp'])/2) #somewhere halfway..
                killtime = data['timestamp'][fakeindex] #TODO: get kill line timestamp from elsewhere
                plotter.add_kill_line(killtime, instanceName)
        #plotter.show_plot()
        plotter.save_plot(f"{location}/{experiment}-throughput")

def produce_combined_throughput_graph(location: str):
    plotter = Plotter()
    plotter.start_plot()
    for experiment in get_experiments_at_location(location):
        graphData = pd.DataFrame()
        for perf_file in get_performance_files(location, experiment):
            instanceName = perf_file.split("-", 1)[0]
            #TODO: only consider SINKS?!
            #if(instanceName.endswith(("09", "10", "11")) is False):
            #    continue
            print(f"Processing datafile {perf_file} ({instanceName})")
            data = get_performance_file_content(location, experiment, perf_file)
            if(data.empty):
                continue
            #parse & normalize
            data = normalize_timestamp_column(
                parse_performance_data(data)
            )
            #print(data)
            graphData = pd.concat([graphData, data]) #merge into experiment results
            #add kill-line if necessary
            if(instanceName == 'crainst02'):
                fakeindex = round(len(data['timestamp'])/2) #somewhere halfway..
                killtime = data['timestamp'][fakeindex] #TODO: get kill line timestamp from elsewhere
                plotter.add_kill_line(killtime, instanceName)
        #TODO: is binning ok?
        graphData['timestamp'] = graphData['timestamp'].transform(lambda dp: dp/1000)
        graphData['time_ranges'] = pd.cut(graphData['timestamp'], 60).apply(lambda inter: inter.right)       
        graphData['tp_sums'] = graphData['throughput'].groupby(graphData['time_ranges']).sum()
        #print(graphData)
        #add to plot
        plotter.add_throughput_data(graphData['tp_sums'].keys(), graphData['tp_sums'].values, f"{experiment}")
    #plotter.show_plot()
    plotter.save_plot(f"{location}/throughput")