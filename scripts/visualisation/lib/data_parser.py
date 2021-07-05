
from datetime import datetime
import pandas as pd

def lowpass(data: pd.Series, alpha: float):
    data2 = []
    for i, val in enumerate(data):
        if (i != 0 and i != len(data) - 1):
            try:
                data2.append(alpha * data[i - 1] + (1 - 2 * alpha) * val + alpha * data[i + 1])
            except:
                data2.append(val)
    return pd.Series(data2)

def savitzky_golay(y, window_size, order, deriv=0, rate=1):
    r"""Smooth (and optionally differentiate) data with a Savitzky-Golay filter.
    The Savitzky-Golay filter removes high frequency noise from data.
    It has the advantage of preserving the original shape and
    features of the signal better than other types of filtering
    approaches, such as moving averages techniques.
    Parameters
    ----------
    y : array_like, shape (N,)
        the values of the time history of the signal.
    window_size : int
        the length of the window. Must be an odd integer number.
    order : int
        the order of the polynomial used in the filtering.
        Must be less then `window_size` - 1.
    deriv: int
        the order of the derivative to compute (default = 0 means only smoothing)
    Returns
    -------
    ys : ndarray, shape (N)
        the smoothed signal (or it's n-th derivative).
    Notes
    -----
    The Savitzky-Golay is a type of low-pass filter, particularly
    suited for smoothing noisy data. The main idea behind this
    approach is to make for each point a least-square fit with a
    polynomial of high order over a odd-sized window centered at
    the point.
    Examples
    --------
    t = np.linspace(-4, 4, 500)
    y = np.exp( -t**2 ) + np.random.normal(0, 0.05, t.shape)
    ysg = savitzky_golay(y, window_size=31, order=4)
    import matplotlib.pyplot as plt
    plt.plot(t, y, label='Noisy signal')
    plt.plot(t, np.exp(-t**2), 'k', lw=1.5, label='Original signal')
    plt.plot(t, ysg, 'r', label='Filtered signal')
    plt.legend()
    plt.show()
    References
    ----------
    .. [1] A. Savitzky, M. J. E. Golay, Smoothing and Differentiation of
       Data by Simplified Least Squares Procedures. Analytical
       Chemistry, 1964, 36 (8), pp 1627-1639.
    .. [2] Numerical Recipes 3rd Edition: The Art of Scientific Computing
       W.H. Press, S.A. Teukolsky, W.T. Vetterling, B.P. Flannery
       Cambridge University Press ISBN-13: 9780521880688
    """
    import numpy as np
    from math import factorial
    
    y[-1] = 0
    try:
        window_size = np.abs(np.int(window_size))
        order = np.abs(np.int(order))
    except ValueError as msg:
        raise ValueError("window_size and order have to be of type int")
    if window_size % 2 != 1 or window_size < 1:
        raise TypeError("window_size size must be a positive odd number")
    if window_size < order + 2:
        raise TypeError("window_size is too small for the polynomials order")
    order_range = range(order+1)
    half_window = (window_size -1) // 2
    # precompute coefficients
    b = np.mat([[k**i for i in order_range] for k in range(-half_window, half_window+1)])
    m = np.linalg.pinv(b).A[deriv] * rate**deriv * factorial(deriv)
    # pad the signal at the extremes with
    # values taken from the signal itself
    firstvals = y[0] - np.abs( y[1:half_window+1][::-1] - y[0] )
    lastvals = y[-1] + np.abs(y[-half_window-1:-1][::-1] - y[-1])
    y = np.concatenate((firstvals, y, lastvals))
    res = np.convolve( m[::-1], y, mode='valid')
    return np.delete(res, -1)

def parse_time_to_timestamp(timestr: str):
    dt_obj = datetime.strptime(f'01.01.2000 {timestr}', '%d.%m.%Y %H:%M:%S:%f')
    millisec = dt_obj.timestamp() * 1000
    return millisec

def normalize_timestamp_column(datapoints: pd.DataFrame, initialTs: float):
    datapoints['timestamp'] = datapoints['timestamp'].transform(lambda dp: dp - initialTs)
    return datapoints


def parse_throughput_data(datapoints: pd.DataFrame):
    datapoints = datapoints.transform([parse_time_to_timestamp, int])
    datapoints.columns = ['timestamp', 'throughput']
    return datapoints

def parse_latency_data(datapoints: pd.DataFrame):
    #print(datapoints)
    #datapoints = datapoints.transform([parse_time_to_timestamp, str, int])
    #print(datapoints)

    datapoints['timestamp'] = datapoints['timestamp'].transform(parse_time_to_timestamp)
    datapoints['latency'] = datapoints['latency'].apply(lambda x: float(str(x).strip()))
    datapoints['shard'] = datapoints['shard'].transform(int)

    datapoints.columns = ['timestamp', 'latency', 'shard']
    return datapoints

def parse_failures_data(datapoints: pd.DataFrame):
    datapoints = datapoints.transform([parse_time_to_timestamp])
    datapoints.columns = ['timestamp']
    return datapoints

def parse_checkpoint_data(datapoints: pd.DataFrame):
    datapoints['timestamp'] = datapoints['timestamp'].transform(parse_time_to_timestamp)
    datapoints['forced'] = datapoints['forced'].transform(lambda e: str(e) == "True")
    datapoints['taken_ms'] = datapoints['taken_ms'].transform(float)
    datapoints['bytes'] = datapoints['bytes'].transform(int)
    return datapoints

def parse_recovery_data(datapoints: pd.DataFrame):
    datapoints['timestamp'] = datapoints['timestamp'].transform(parse_time_to_timestamp)
    datapoints['restored_ms'] = datapoints['restored_ms'].transform(float)
    datapoints['rollback_ms'] = datapoints['rollback_ms'].transform(float)
    return datapoints
