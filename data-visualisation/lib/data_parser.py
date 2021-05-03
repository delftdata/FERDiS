
from datetime import datetime
import pandas as pd


def parse_time_to_timestamp(timestr: str):
    dt_obj = datetime.strptime(f'01.01.2000 {timestr}', '%d.%m.%Y %H:%M:%S:%f')
    millisec = dt_obj.timestamp() * 1000
    return millisec

def normalize_timestamp_column(datapoints: pd.DataFrame):
    init_ts = datapoints['timestamp'][0]
    datapoints['timestamp'] = datapoints['timestamp'].transform(lambda dp: dp - init_ts)
    return datapoints


def parse_performance_data(datapoints: pd.DataFrame):
    datapoints = datapoints.transform([parse_time_to_timestamp, int, int, int, int])
    datapoints.columns = ['timestamp', 'throughput', 'latency_min', 'latency_avg', 'latency_max']
    return datapoints

def parse_checkpoint_data(datapoints: pd.DataFrame):
    print(datapoints)
    datapoints['timestamp'] = datapoints['timestamp'].transform(parse_time_to_timestamp)
    datapoints['forced'] = datapoints['forced'].transform(lambda s: s == "True")
    datapoints['taken_ms'] = datapoints['taken_ms'].transform(float)
    datapoints['bytes'] = datapoints['bytes'].transform(int)

    print(datapoints)
    return datapoints



