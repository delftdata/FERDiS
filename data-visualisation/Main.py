import json
from typing import List

import pandas as pd
import matplotlib.pyplot as plt
import os
import numpy as np

from performance_plots import produce_throughput_graph, produce_combined_throughput_graph
from checkpoint_plots import produce_checkpoint_plot


def main():
    logfileloc = "C:/Users/marcd/LOGDUMP"
    #produce_throughput_graph(logfileloc)
    #produce_combined_throughput_graph(logfileloc)
    produce_checkpoint_plot(logfileloc)

if __name__ == "__main__":
    main()