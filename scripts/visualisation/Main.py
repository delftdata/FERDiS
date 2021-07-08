import json
from typing import List

import pandas as pd
import matplotlib.pyplot as plt
import os
import numpy as np

from performance_plots import produce_throughput_graphs_in_folder, produce_throughput_compound_graph, produce_latency_graphs_in_folder, produce_latency_compound_graph
from checkpoint_plots import produce_checkpoint_plot, produce_checkpoint_metrics, produce_recovery_metrics, print_checkpoint_metrics

import warnings
warnings.filterwarnings("ignore")

fromSecond = 30
toSecond = 270
plots = not False
compound_plots = False
metrics = False
logfileloc = "C:/Projects/BlackSP/scripts/experiments/results.bak/REAL_EXPERIMENTS_FILTERED/job 3/" #"C:/Projects/BlackSP/scripts/experiments/results" 

#metricExperiment = 'job-6-cp-0-10s-3.2k-(0)'

compound_keys = [
    ('job-0-cp-2-15s-18k-(0)', 'CIC 1'),
    ('job-0-cp-2-15s-18k-(1)', 'CIC 2'),
    ('job-0-cp-2-15s-18k-(2)', 'CIC 3')
]

compound_metric_keys = [
    ('job-0-cp-2-15s-18k-(0)'),
    ('job-0-cp-2-15s-18k-(1)'),
    ('job-0-cp-2-15s-18k-(2)')
]

def main():
    if(plots):
        produce_throughput_graphs_in_folder(logfileloc)
        produce_latency_graphs_in_folder(logfileloc)

    if(compound_plots):
        plotter = produce_throughput_compound_graph(logfileloc, compound_keys)
        plotter.xlim([fromSecond, toSecond])
        plotter.ylim([0, 1000])
        #plotter.show_plot(experiment)
        plotter.save_plot(f"{logfileloc}/compound-throughput")
        
        plotter = produce_latency_compound_graph(logfileloc, compound_keys)
        plotter.xlim([fromSecond, toSecond])
        plotter.ylim([0, 35000])
        #plotter.show_plot(experiment)
        plotter.save_plot(f"{logfileloc}/compound-latency")

    if(metrics):
        cpDataFrame = pd.DataFrame()
        for experiment in compound_metric_keys:
            cpDataFrame = produce_checkpoint_metrics(logfileloc, experiment, cpDataFrame)
        print_checkpoint_metrics(cpDataFrame)
        
        for experiment in compound_metric_keys:
            produce_recovery_metrics(logfileloc, experiment)
        
    
    
    print("Success")

if __name__ == "__main__":
    main()