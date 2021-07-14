import json
from typing import List

import pandas as pd
import matplotlib.pyplot as plt
import os
import sys
import numpy as np

from performance_plots import produce_throughput_graphs_in_folder, produce_throughput_compound_graph, produce_latency_graphs_in_folder, produce_latency_compound_graph, produce_compound_latency_metrics
from checkpoint_plots import produce_checkpoint_plot, produce_compound_checkpoint_metrics, produce_compound_recovery_metrics

import warnings
warnings.filterwarnings("ignore")


print_metrics = True

plot_data_folder = "C:/Users/MarcZwart/Desktop/SF_EXP_DATA/NEXMark 3 L" #"C:/Projects/BlackSP/scripts/experiments/results" 
#logfileloc = "C:/projects/BlackSP/scripts/experiments/results"

metric_data_folder = "C:/projects/BlackSP/scripts/experiments/results"#"C:/Users/MarcZwart/Desktop/SF_EXP_DATA"

fromSecond = 30
toSecond = 240

compound_plot_keys = [
    ('job-1-cp-0-10s-18k-(3)', 'UC'),
    ('job-1-cp-1-10s-18k-(3)', 'CC'),
    ('job-1-cp-2-10s-18k-(2)', 'CIC')
]

def main():
    args = sys.argv[1:]
    if(len(args) == 0) :
        print("give argument")
        exit()

    action = args[0];

    if(action == 'plot_throughput'):
        produce_throughput_graphs_in_folder(plot_data_folder)
    if(action == 'plot_latency'):
        produce_latency_graphs_in_folder(plot_data_folder)

    if(action == 'plot_compound'):
        plotter = produce_throughput_compound_graph(plot_data_folder, compound_plot_keys)
        plotter.xlim([fromSecond, toSecond])
        plotter.ylim([0, 75000])
        #plotter.show_plot(experiment)
        plotter.save_plot(f"{plot_data_folder}/compound-throughput")
        
        plotter = produce_latency_compound_graph(plot_data_folder, compound_plot_keys)
        plotter.xlim([fromSecond, toSecond])
        plotter.ylim([0, 25000])
        #plotter.show_plot(experiment)
        plotter.save_plot(f"{plot_data_folder}/compound-latency")

    if(action == 'metric_latency'):
        metrics = produce_compound_latency_metrics(metric_data_folder, fromSecond, toSecond)
        metrics['protocol'] = metrics['protocol'].apply(lambda s: 'UC' if s == '0' else 'CC' if s == '1' else 'CIC')
        metrics['protocol'] = metrics['protocol'] + " @ " + metrics['interval']+"s"
        metrics.drop('interval', axis='columns', inplace=True)
        print(metrics.to_latex(index = False))

    if(action == 'metric_checkpoint'):  
        metrics = produce_compound_checkpoint_metrics(metric_data_folder)
        metrics['protocol'] = metrics['protocol'].apply(lambda s: 'UC' if s == '0' else 'CC' if s == '1' else 'CIC')
        metrics['protocol'] = metrics['protocol'] + " @ " + metrics['interval']+"s"
        metrics.drop('interval',axis='columns', inplace=True)
        print(metrics.to_latex(index = False))
        
    if(action == 'metric_recovery'):
        metrics = produce_compound_recovery_metrics(metric_data_folder)
        metrics['protocol'] = metrics['protocol'].apply(lambda s: 'UC' if s == '0' else 'CC' if s == '1' else 'CIC')
        metrics['protocol'] = metrics['protocol'] + " @ " + metrics['interval']+"s"
        metrics.drop('interval',axis='columns', inplace=True)
        print(metrics.to_latex(index = False))
        
    print("Done, exiting")

if __name__ == "__main__":
    main()