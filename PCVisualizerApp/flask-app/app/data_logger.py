import pandas as pd
from datetime import datetime
import os
import json
import csv
import numpy as np
DATA_PATH = os.path.join(os.path.dirname(os.path.abspath(__file__)),"test_data") 

def get_dist_ang_array(dists, angs):
    return [np.concatenate( (np.array(dists), np.array(angs)*180/np.pi) )]

def save_optimization_data(model_points, detected_points, model_distances, model_angles, distances, angles, normal_vectors, 
                            updated_points, updated_distances, updated_angles, orginal_ratio, updated_ratio):
    dt_string = datetime.now().strftime("%d/%m/%Y %H:%M:%S") 
    n = len(get_optimization_data())
    pd_dict = {'aa_Index': n+1,'ab_Timestamp': [dt_string], 'ba_Model_Points': [model_points], 'bb_Detected_Points':[detected_points], 
                'bc_Updated_Points': [updated_points],'ca_Model_Distances': get_dist_ang_array(model_distances, model_angles),
                'cb_Detected_Distances':get_dist_ang_array(distances, angles), 'cc_Updated_Distances': get_dist_ang_array(updated_distances, updated_angles),
                'da_Original_Ratio': [orginal_ratio], 'db_Updated_Ratio':[updated_ratio], 'ea_Normal_Vectors':[normal_vectors],}
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