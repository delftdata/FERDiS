import json
from typing import List

import pandas as pd
import matplotlib.pyplot as plt
import os
import numpy as np

from performance_plots import produce_throughput_graph, produce_latency_graph
from checkpoint_plots import produce_checkpoint_plot

import warnings
warnings.filterwarnings("ignore")

def main():
    logfileloc = "C:/Projects/BlackSP/scripts/experiments/results"

    produce_throughput_graph(logfileloc, 0, 999)
    produce_latency_graph(logfileloc, 0, 999)
    #produce_combined_throughput_graph(logfileloc)
    #produce_checkpoint_plot(logfileloc)

    print("Success")

if __name__ == "__main__":
    main()