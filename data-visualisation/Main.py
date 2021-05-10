import json
from typing import List

import pandas as pd
import matplotlib.pyplot as plt
import os
import numpy as np

from performance_plots import produce_throughput_graph, produce_latency_graph
from checkpoint_plots import produce_checkpoint_plot


def main():
    logfileloc = "C:/Projects/BlackSP/scripts/results"

    produce_throughput_graph(logfileloc)
    produce_latency_graph(logfileloc)
    
    #produce_combined_throughput_graph(logfileloc)
    #produce_checkpoint_plot(logfileloc)

if __name__ == "__main__":
    main()