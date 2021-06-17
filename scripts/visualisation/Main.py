import json
from typing import List

import pandas as pd
import matplotlib.pyplot as plt
import os
import numpy as np

from performance_plots import produce_throughput_graph, produce_latency_graph
from checkpoint_plots import produce_checkpoint_plot, produce_checkpoint_metrics, produce_recovery_metrics

import warnings
warnings.filterwarnings("ignore")

fromSecond = 30
toSecond = 270

def main():
    logfileloc = "C:/Projects/BlackSP/scripts/experiments/results"
    targetExperiment = 'job-1-uc-30s-45k-sourcefail'
    plots = True
    metrics = True

    if(plots):
        produce_throughput_graph(logfileloc, fromSec=fromSecond, toSec=toSecond,)
        produce_latency_graph(logfileloc, fromSec=fromSecond, toSec=toSecond, plotMeanPerTime=True)
        #produce_combined_throughput_graph(logfileloc)
        #produce_checkpoint_plot(logfileloc)
    
    if(metrics):
        produce_checkpoint_metrics(logfileloc, targetExperiment)
        produce_recovery_metrics(logfileloc, targetExperiment)
    
    
    print("Success")

if __name__ == "__main__":
    main()