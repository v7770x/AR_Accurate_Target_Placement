import pandas as pd
from datetime import datetime
import os
import json
import csv
DATA_PATH = os.path.join(os.path.dirname(os.path.abspath(__file__)),"test_data") 

def save_optimization_data(model_points, detected_points, distances, angles, normal_vectors):
    dt_string = datetime.now().strftime("%d/%m/%Y %H:%M:%S") 
    n = len(get_optimization_data())
    pd_dict = {'0_Index': n+1,'1_Timestamp': [dt_string], '2_Model_Points': [model_points], '3_Detected_Points':[detected_points], '4_Distances':[distances], '5_Angles':[angles], '6_Normal_Vectors':[normal_vectors] }
    df = pd.DataFrame(pd_dict)
    optimization_path = os.path.join(DATA_PATH, "optimization_data.csv")
    print("saving to", optimization_path)
    if(os.path.exists(optimization_path)):
        df.to_csv(optimization_path,  mode='a', header=False, index=False)
    else:
        df.to_csv(optimization_path,  mode='a', index=False)

def get_optimization_data():
    optimization_path = os.path.join(DATA_PATH, "optimization_data.csv")
    if(os.path.exists(optimization_path)):
        # return pd.read_csv(optimization_path).to_dict()
        csvfile =  csv.DictReader(open(optimization_path))
        return [row for row in csvfile]
    else:
        return []