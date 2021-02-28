import numpy as np 
from scipy.optimize import minimize
from scipy import optimize
from app.optimization.eqn_solver import get_distance, extract_target_vals



'''common methods'''
def extract_optimization_vars(space_vals, model_vals):
    n = len(model_vals)
    space_vals = np.array(space_vals)
    space_vals_flat = np.array(space_vals).reshape(n*3)
    model_vals = np.array(model_vals)
    return (n, space_vals, space_vals_flat, model_vals)

#Given set of points in list, find distances 
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


'''Methods for log function'''
#log functions of just distances
def log_distance_function(space_vals_flat, model_vals, space_vals_start_flat, tolerance_mm):
    space_vals = space_vals_flat.reshape(len(model_vals),3)
    space_vals_start = space_vals_start_flat.reshape(len(model_vals),3)
    if not ( np.absolute(np.subtract(space_vals, space_vals_start)) < tolerance_mm ).all():
        return 5
    space_dists = extract_distances(space_vals)
    model_dists = extract_distances(model_vals)
    return np.absolute( np.log( np.prod( np.divide(space_dists, model_dists) ) ) )

def minimize_nelder_log_dist_ratios(space_vals, model_vals, tolerance_mm):
    (n, space_vals, space_vals_flat, model_vals) = extract_optimization_vars(space_vals, model_vals)
    print("flat space", space_vals_flat)
    minimized_vals =  minimize(log_distance_function, space_vals_flat, args=(model_vals, space_vals_flat, tolerance_mm), method='nelder-mead',
               options={'xatol': 1e-2, 'disp': True}).x
    return np.array(minimized_vals).reshape((n, 3)).tolist()
    

'''methods for absolute diff functions'''
#absolute difference functions of ratios
def extract_abs_diff_ratios(vals, model_vals):
    target_vals = extract_target_vals(vals)
    model_target_vals = extract_target_vals(model_vals)
    return np.abs(1 - target_vals/model_target_vals) + 1

def abs_diff_ratio_function(space_vals_flat, model_vals, a0, n, space_vals_start_flat, tolerance_mm):
    space_vals = space_vals_flat.reshape(n,3)
    if not ( np.absolute(np.subtract(space_vals_flat, space_vals_start_flat)) < tolerance_mm ).all():
        # print(np.absolute(np.subtract(space_vals_flat, space_vals_start_flat)) )
        return 0
    r = a0/extract_abs_diff_ratios(space_vals, model_vals)
    f = np.product( (r>=1) * r )
    return -f

def abs_diff_ratio_function_no_bounds(space_vals_flat, model_vals, a0, n, space_vals_start_flat):
    space_vals = space_vals_flat.reshape(n,3)
    r = a0/extract_abs_diff_ratios(space_vals, model_vals)
    f = np.product( (r>=1) * r )
    return -f

def minimize_nelder_abs_diff(space_vals, model_vals, tolerance_mm):
    (n, space_vals, space_vals_flat, model_vals) = extract_optimization_vars(space_vals, model_vals)
    #find initial ratio
    a0 = extract_abs_diff_ratios(space_vals, model_vals)
    minimized_vals =  minimize(abs_diff_ratio_function, space_vals_flat, args=(model_vals, a0, n, space_vals_flat, tolerance_mm), method='nelder-mead',
               options={'xatol': 1e-2, 'disp': True}).x
    return np.array(minimized_vals).reshape(n, 3).tolist()


def minimize_BFGS_abs_diff(space_vals, model_vals, tolerance_mm):
    (n, space_vals, space_vals_flat, model_vals) = extract_optimization_vars(space_vals, model_vals)
    a0 = extract_abs_diff_ratios(space_vals, model_vals)
    minimized_vals =  minimize(abs_diff_ratio_function_no_bounds, space_vals_flat, args=(model_vals, a0, n, space_vals_flat, tolerance_mm), method='BFGS',
              options={ 'disp': True}).x
    return np.array(minimized_vals).reshape(n, 3).tolist()

def minimize_global_abs_diff(space_vals, model_vals, tolerance_mm):
    (n, space_vals, space_vals_flat, model_vals) = extract_optimization_vars(space_vals, model_vals)
    bounds=[(i-tolerance_mm, i+tolerance_mm) for i in space_vals_flat]
    a0 = extract_abs_diff_ratios(space_vals, model_vals)
    minimized_vals =  optimize.shgo(abs_diff_ratio_function_no_bounds, bounds, args=(model_vals, a0, n, space_vals_flat),
              options={ 'disp': True}).x
    return np.array(minimized_vals).reshape(n, 3).tolist()