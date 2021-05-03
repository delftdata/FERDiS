import json
from typing import List

import pandas as pd
import matplotlib.pyplot as plt
import os
import numpy as np

from lib.data_retriever import get_experiments_at_location, get_checkpoint_files, get_checkpoint_file_content
from lib.data_parser import parse_checkpoint_data, normalize_timestamp_column
from lib.plot_builder import Plotter

def produce_checkpoint_plot(location: str):
    for experiment in get_experiments_at_location(location):
        plotter = Plotter()
        plotter.start_plot()

        frame = pd.DataFrame()

        for perf_file in get_checkpoint_files(location, experiment):
            instanceName = perf_file.split("-", 1)[0]
            
            print(f"Processing datafile {perf_file} ({instanceName})")
            data = get_checkpoint_file_content(location, experiment, perf_file)
            if(data.empty):
                continue
            #parse & normalize
            data = normalize_timestamp_column(
                parse_checkpoint_data(data)
            )
            #print(data)
            #add to plot
            frame = pd.concat([frame, data['bytes']])

        frame.columns = ['bytes']
        print(frame)
        plotter.add_checkpoint_data(frame)
        plotter.show_plot()
        #plotter.save_plot(f"{location}/{experiment}-checkpoint-size")