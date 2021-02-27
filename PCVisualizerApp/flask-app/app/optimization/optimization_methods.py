import numpy as np 
from scipy.optimize import minimize
from app.optimization.eqn_solver import get_distance, extract_target_vals

'''
 Given set of points in list, find distances 
'''
def extract_distances(points, flat_vals = False):
    #extract properties of points
    n = len(points)
    points = np.array(points)
    if flat_vals:
        n = int(n/3)
        points = points.reshape(( n, 3))
    target_vals = []

    #iterate over all points and get distances
    for i in range(n-1):
        for j in range(i+1,n):
            target_vals.append(get_distance(points[i],points[j]))
    
    return np.array(target_vals)


def log_distance_function(space_vals_flat, model_vals, space_vals_start_flat):
    space_vals = space_vals_flat.reshape(len(model_vals),3)
    space_vals_start = space_vals_start_flat.reshape(len(model_vals),3)
    if not ( np.absolute(np.subtract(space_vals, space_vals_start)) < 5 ).all():
        return 5
    space_dists = extract_distances(space_vals)
    model_dists = extract_distances(model_vals)
    return np.absolute( np.log( np.prod( np.divide(space_dists, model_dists) ) ) )

def extract_abs_diff_ratios(vals, model_vals):
    target_vals = extract_target_vals(vals)
    model_target_vals = extract_target_vals(model_vals)
    return np.abs(1 - target_vals/model_target_vals) + 1

def abs_diff_ratio_function(space_vals_flat, model_vals, a0, n, space_vals_start_flat):
    space_vals = space_vals_flat.reshape(n,3)
    if not ( np.absolute(np.subtract(space_vals_flat, space_vals_start_flat)) < 5 ).all():
        # print(np.absolute(np.subtract(space_vals_flat, space_vals_start_flat)) )
        return 0
    r = a0/extract_abs_diff_ratios(space_vals, model_vals)
    f = np.product( (r>=1) * r )
    return -f

def minimize_nelder(space_vals, model_vals):
    space_vals_flat = np.array(space_vals).reshape(len(model_vals)*3)
    print("flat space", space_vals_flat)
    minimized_vals =  minimize(log_distance_function, space_vals_flat, args=(model_vals, space_vals_flat), method='nelder-mead',
               options={'xatol': 1e-2, 'disp': True}, bounds=[(i-5, i+5) for i in space_vals_flat]).x
    return np.array(minimized_vals).reshape((len(model_vals), 3)).tolist()
    
def minimize_nelder_abs_diff(space_vals, model_vals):
    n = len(model_vals)
    space_vals = np.array(space_vals)
    space_vals_flat = np.array(space_vals).reshape(n*3)
    model_vals = np.array(model_vals)
    a0 = extract_abs_diff_ratios(space_vals, model_vals)
    print("a0 = ", a0)
    space_vals_flat = np.array(space_vals).reshape(n*3)
    minimized_vals =  minimize(abs_diff_ratio_function, space_vals_flat, args=(model_vals, a0, n, space_vals_flat), method='nelder-mead',
               options={'xatol': 1e-2, 'disp': True}, bounds=[(i-5, i+5) for i in space_vals_flat]).x
    return np.array(minimized_vals).reshape(n, 3).tolist()