import json
from typing import List

import pandas as pd
import matplotlib.pyplot as plt
import os
import numpy as np

from performance_plots import produce_throughput_graphs_in_folder, produce_throughput_compound_graph, produce_latency_graphs_in_folder, produce_latency_compound_graph
from checkpoint_plots import produce_checkpoint_plot, produce_checkpoint_metrics, produce_recovery_metrics

import warnings
warnings.filterwarnings("ignore")

fromSecond = 30
toSecond = 270

def main():
    plots = True
    compound_plots = True
    metrics = True
    logfileloc = "C:/Projects/BlackSP/scripts/experiments/results"
    metricExperiment = 'job-6-cp-0-10s-3.2k-(0)'
    
    compound_keys = [
        ('job-6-cp-0-10s-4k-(0)', 'Run 1'),
        ('job-6-cp-0-10s-4k-(1)', 'Run 2'),
        ('job-6-cp-0-10s-4k-(2)', 'Run 3')
    ]

    if(plots):
        produce_throughput_graphs_in_folder(logfileloc)
        produce_latency_graphs_in_folder(logfileloc)

    if(compound_plots):
        plotter = produce_throughput_compound_graph(logfileloc, compound_keys)
        plotter.xlim([fromSecond, toSecond])
        plotter.xlim([30, 230])
        plotter.ylim([0, 100000])
        #plotter.show_plot(experiment)
        plotter.save_plot(f"{logfileloc}/compound-throughput")
        
        plotter = produce_latency_compound_graph(logfileloc, compound_keys)
        plotter.xlim([30, 230])
        plotter.ylim([0, 50000])
        #plotter.show_plot(experiment)
        plotter.save_plot(f"{logfileloc}/compound-latency")

    if(metrics):
        produce_checkpoint_metrics(logfileloc, metricExperiment)
        produce_recovery_metrics(logfileloc, metricExperiment)
    
    
    print("Success")

if __name__ == "__main__":
    main()