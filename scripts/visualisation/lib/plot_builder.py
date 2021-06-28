
import matplotlib.pyplot as plt
import pandas as pd

class Plotter:

    #fig

    def start_plot(self):
        self.fig = plt.figure(figsize=[8,4.5], dpi=150)
        plt.grid(axis="y")
        self.c = 0


    def add_checkpoint_data(self, series: pd.DataFrame):
        self.c = self.c + 1
        
        plt.boxplot(series.values, autorange=True, labels=["wauw"])
        #color='random',
        
        plt.xlabel("Experiment Time (s)")
        plt.ylabel("Throughput (e/s)")
        plt.legend(prop={'size': 16})
        self.fig.tight_layout(pad=0.05)

    def add_throughput_data(self, timestamps: pd.DataFrame, throughputs: pd.DataFrame, instanceName: str):
        self.c = self.c + 1
        
        plt.plot(timestamps, 
                throughputs, 
                #(0, self.c), 
                label=instanceName, 
                alpha=0.8,
                linestyle='-',
                marker='.',
                linewidth=1,
                markersize=4)
        #color='random',
        
        plt.xlabel("Experiment Time (s)")
        plt.ylabel("Throughput (e/s)")
        plt.legend(prop={'size': 12})
        self.fig.tight_layout(pad=0.05)
        
    def add_latency_data(self, timestamps: pd.DataFrame, latencies: pd.DataFrame, label: str):
        plt.plot(timestamps, 
                latencies, 
                #(0, self.c), 
                label=label, 
                alpha=0.8,
                linestyle='',
                marker='.',
                linewidth=1,
                markersize=4)
        #color='random',
        
        plt.xlabel("Experiment Time (s)")
        plt.ylabel("Latency (ms)")
        plt.legend(prop={'size': 12})
        #self.fig.tight_layout(pad=0.05)

    def add_kill_line(self, killtime, label):
        plt.axvline((killtime) / 1000, color="r", linestyle="--")

    def xlim(self, arg: list):
        plt.xlim(arg)

    def ylim(self, arg: list):
        plt.ylim(arg)

    def show_plot(self, title = 'plot'):
        self.fig.canvas.set_window_title(title)
        plt.show()
        self.fig = None

    def save_plot(self, path):
        plt.savefig(path + ".pdf",
            bbox_inches="tight",
            # pad_inches=0.05,
            # transparent=True
            )
        plt.close()
        self.fig = None