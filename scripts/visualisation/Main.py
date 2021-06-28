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
    compound_plots = False
    metrics = True
    logfileloc = "C:/Projects/BlackSP/scripts/experiments/results"
    metricExperiment = 'job-6-cp-2-10s-1k-(0)'
    
    job1_targetExperiments = [
        ('job-1-cc-30s-50k-sourcefail', 'CC'),
        ('job-1-uc-30s-50k-sourcefail', 'UC'),
        ('job-1-cic-30s-50k-sourcefail', 'CIC')
    ]

    job1_nostate_targetExperiments = [
        ('job-1-cp-1-10s-30k-sink-fail(0)', 'CC'),
        ('job-1-cp-0-10s-30k-sink-fail(1)', 'UC'),
        ('job-1-cp-2-10s-30k-sink-fail(0)', 'CIC')
    ]

    job2_targetExperiments = [
        ('job-2-cc-30s-100k-filter-fail', 'CC'),
        ('job-2-uc-30s-100k-filter-fail', 'UC'),
        ('job-2-cic-30s-100k-filter-fail', 'CIC')
    ]

    job2_nostate_targetExperiments = [
        ('job-2-cp-1-10s-100k-sink-fail(1)', 'CC'),
        ('job-2-cp-0-10s-100k-sink-fail(0)-no-cp-dealignment', 'UC'),
        ('job-2-cp-2-10s-100k-sink-fail(0)', 'CIC')
    ]

    

    if(plots):
        produce_throughput_graphs_in_folder(logfileloc)
        produce_latency_graphs_in_folder(logfileloc)

    if(compound_plots):
        plotter = produce_throughput_compound_graph(logfileloc, job1_nostate_targetExperiments)
        plotter.xlim([fromSecond, toSecond])
        plotter.xlim([30, 230])
        plotter.ylim([0, 100000])
        #plotter.show_plot(experiment)
        plotter.save_plot(f"{logfileloc}/compound-throughput")
        
        plotter = produce_latency_compound_graph(logfileloc, job1_nostate_targetExperiments)
        plotter.xlim([30, 230])
        plotter.ylim([0, 10000])
        #plotter.show_plot(experiment)
        plotter.save_plot(f"{logfileloc}/compound-latency")

    if(metrics):
        produce_checkpoint_metrics(logfileloc, metricExperiment)
        produce_recovery_metrics(logfileloc, metricExperiment)
    
    
    print("Success")

if __name__ == "__main__":
    main()