import json
from typing import List

import pandas as pd
import matplotlib.pyplot as plt
import os
import numpy as np

from performance_plots import produce_throughput_graphs_in_folder, produce_throughput_compound_graph, produce_latency_graphs_in_folder, produce_latency_compound_graph, produce_compound_latency_metrics
from checkpoint_plots import produce_checkpoint_plot, produce_compound_checkpoint_metrics, produce_compound_recovery_metrics

import warnings
warnings.filterwarnings("ignore")


print_plots = False
print_compound_plots = False
print_metrics = True

logfileloc = "C:/Projects/BlackSP/scripts/experiments/results.bak/REAL_EXPERIMENTS_FILTERED/job 0 S/" #"C:/Projects/BlackSP/scripts/experiments/results" 

fromSecond = 30
toSecond = 999

compound_plot_keys = [
    ('job-0-cp-2-15s-18k-(0)', 'CIC 1'),
    ('job-0-cp-2-15s-18k-(1)', 'CIC 2'),
    ('job-0-cp-2-15s-18k-(2)', 'CIC 3')
]

def main():
        
    if(print_plots):
        produce_throughput_graphs_in_folder(logfileloc)
        produce_latency_graphs_in_folder(logfileloc)

    if(print_compound_plots):
        plotter = produce_throughput_compound_graph(logfileloc, compound_plot_keys)
        plotter.xlim([fromSecond, toSecond])
        plotter.ylim([0, 1000])
        #plotter.show_plot(experiment)
        plotter.save_plot(f"{logfileloc}/compound-throughput")
        
        plotter = produce_latency_compound_graph(logfileloc, compound_plot_keys)
        plotter.xlim([fromSecond, toSecond])
        plotter.ylim([0, 35000])
        #plotter.show_plot(experiment)
        plotter.save_plot(f"{logfileloc}/compound-latency")

    if(print_metrics):
        metrics = produce_compound_latency_metrics(logfileloc, fromSecond, toSecond)
        metrics['protocol'] = metrics['protocol'].apply(lambda s: 'UC' if s == '0' else 'CC' if s == '1' else 'CIC')
        metrics['protocol'] = metrics['protocol'] + " @ " + metrics['interval']+"s"
        metrics.drop('interval', axis='columns', inplace=True)
        print(metrics.to_latex(index = False))
        
        print("------------------------------")
        
        metrics = produce_compound_checkpoint_metrics(logfileloc)
        metrics['protocol'] = metrics['protocol'].apply(lambda s: 'UC' if s == '0' else 'CC' if s == '1' else 'CIC')
        metrics['protocol'] = metrics['protocol'] + " @ " + metrics['interval']+"s"
        metrics.drop('interval',axis='columns', inplace=True)
        print(metrics.to_latex(index = False))
        
        print("------------------------------")
        
        metrics = produce_compound_recovery_metrics(logfileloc)
        metrics['protocol'] = metrics['protocol'].apply(lambda s: 'UC' if s == '0' else 'CC' if s == '1' else 'CIC')
        metrics['protocol'] = metrics['protocol'] + " @ " + metrics['interval']+"s"
        metrics.drop('interval',axis='columns', inplace=True)
        print(metrics.to_latex(index = False))
        
    
    
    print("Success")

if __name__ == "__main__":
    main()