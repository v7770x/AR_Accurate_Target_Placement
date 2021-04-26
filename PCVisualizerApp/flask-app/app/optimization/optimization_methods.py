import numpy as np 
from scipy.optimize import minimize
from scipy import optimize
from app.optimization.eqn_solver import get_distance, extract_target_vals
from geneticalgorithm import geneticalgorithm as ga

epsillon = 10
model_vals_ga = None
a0_ga = None
n_ga = None
space_vals_start_flat_ga = None
normal_vectors_ga = None

'''function to check that the new point is in the given plane'''
def check_in_plane(init_point, normal, new_point):
    d = np.dot(init_point, normal)
    d_p = np.dot(new_point, normal)
    if (np.abs(d-d_p) > epsillon):
        #print("failed",d,d_p)
        return False
    #print("passed",d,d_p)
    return True

def check_all_in_plane(init_points, normals, new_points, n):
    all_in_plane = True
    for i in range(n):
        all_in_plane = check_in_plane(init_points[i], normals[i], new_points[i])
        if not all_in_plane:
            return False
    return True


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

def extract_bounds(space_vals_flat, tolerance_mm, for_ga = False):
    if for_ga:
        return np.array([[val-tolerance_mm[i], val+tolerance_mm[i]] for i, val in enumerate(space_vals_flat)])
    return [(val-tolerance_mm[i], val+tolerance_mm[i]) for i, val in enumerate(space_vals_flat)]


'''Methods for log function'''
#log functions of just distances
def log_distance_function(space_vals_flat, model_vals, space_vals_start_flat, tolerance_mm, normal_vectors):
    n = len(model_vals)
    space_vals = space_vals_flat.reshape(n,3)
    space_vals_start = space_vals_start_flat.reshape(n,3)
    if not ( np.absolute(np.subtract(space_vals_flat, space_vals_start_flat)) - tolerance_mm < 0 ).all():
        return 5
    if not check_all_in_plane(space_vals_start, normal_vectors, space_vals, n):
        return 5
    space_dists = extract_distances(space_vals)
    model_dists = extract_distances(model_vals)
    f = np.absolute( np.log( np.prod( np.divide(space_dists, model_dists) ) ) )
    print(f)
    return f

def minimize_nelder_log_dist_ratios(space_vals, model_vals, tolerance_mm, normal_vectors):
    (n, space_vals, space_vals_flat, model_vals) = extract_optimization_vars(space_vals, model_vals)
    #print("flat space", space_vals_flat)
    minimized_vals =  minimize(log_distance_function, space_vals_flat, args=(model_vals, space_vals_flat, tolerance_mm, normal_vectors), method='nelder-mead',
               options={'xatol': 1e-2, 'disp': True}).x
    return np.array(minimized_vals).reshape((n, 3)).tolist()
    

'''methods for absolute diff functions'''
#absolute difference functions of ratios
def extract_abs_diff_ratios(vals, model_vals):
    target_vals = extract_target_vals(vals)
    model_target_vals = extract_target_vals(model_vals)
    return np.abs(1 - target_vals/model_target_vals) + 1

def abs_diff_ratio_function_ga(space_vals_flat):
    global model_vals_ga, a0_ga, n_ga, space_vals_start_flat_ga, normal_vectors_ga
    return abs_diff_ratio_function_no_bounds(space_vals_flat, model_vals_ga, a0_ga, n_ga, space_vals_start_flat_ga, normal_vectors_ga)

def abs_diff_ratio_function(space_vals_flat, model_vals, a0, n, space_vals_start_flat, tolerance_mm, normal_vectors):
    if not ( np.absolute(np.subtract(space_vals_flat, space_vals_start_flat)) - tolerance_mm < 0 ).all():
        # print(np.absolute(np.subtract(space_vals_flat, space_vals_start_flat)) )
        return 0
    return abs_diff_ratio_function_no_bounds(space_vals_flat, model_vals, a0, n, space_vals_start_flat, normal_vectors)
    # space_vals = space_vals_flat.reshape(n,3)
    # space_vals_start = space_vals_start_flat.reshape(n,3)
    # if not check_all_in_plane(space_vals_start, normal_vectors, space_vals, n):
    #     return 0    
    # r = a0/extract_abs_diff_ratios(space_vals, model_vals)
    # f = np.product( (r>=1) * r )
    # return -f

def abs_diff_ratio_function_no_bounds(space_vals_flat, model_vals, a0, n, space_vals_start_flat, normal_vectors):
    space_vals = space_vals_flat.reshape(n,3)
    space_vals_start = space_vals_start_flat.reshape(n,3)
    if not check_all_in_plane(space_vals_start, normal_vectors, space_vals, n):
        return 0
    r = a0/extract_abs_diff_ratios(space_vals, model_vals)
    f = np.product( (r>=1) * r )
    #f = np.product(r)
    print(-f)
    return -f

def minimize_nelder_abs_diff(space_vals, model_vals, tolerance_mm, normal_vectors):
    (n, space_vals, space_vals_flat, model_vals) = extract_optimization_vars(space_vals, model_vals)
    #find initial ratio
    a0 = extract_abs_diff_ratios(space_vals, model_vals)
    minimized_vals =  minimize(abs_diff_ratio_function, space_vals_flat, 
                args=(model_vals, a0, n, space_vals_flat, tolerance_mm, normal_vectors), method='nelder-mead',
               options={'xatol': 1e-2, 'disp': True}).x
    return np.array(minimized_vals).reshape(n, 3).tolist()


def minimize_BFGS_abs_diff(space_vals, model_vals, tolerance_mm, normal_vectors):
    (n, space_vals, space_vals_flat, model_vals) = extract_optimization_vars(space_vals, model_vals)
    a0 = extract_abs_diff_ratios(space_vals, model_vals)
    minimized_vals =  minimize(abs_diff_ratio_function, space_vals_flat, 
                args=(model_vals, a0, n, space_vals_flat, tolerance_mm, normal_vectors), method='BFGS',
              options={ 'disp': True}).x
    return np.array(minimized_vals).reshape(n, 3).tolist()

def minimize_global_abs_diff(space_vals, model_vals, tolerance_mm, normal_vectors):
    (n, space_vals, space_vals_flat, model_vals) = extract_optimization_vars(space_vals, model_vals)
    bounds=extract_bounds(space_vals_flat, tolerance_mm)
    a0 = extract_abs_diff_ratios(space_vals, model_vals)
    minimized_vals =  optimize.shgo(abs_diff_ratio_function_no_bounds, bounds, args=(model_vals, a0, n, space_vals_flat,
                normal_vectors),
              options={ 'disp': True}).x
    return np.array(minimized_vals).reshape(n, 3).tolist()

def minimize_genetic_abs_diff(space_vals, model_vals, tolerance_mm, normal_vectors):
    (n, space_vals, space_vals_flat, model_vals) = extract_optimization_vars(space_vals, model_vals)
    a0 = extract_abs_diff_ratios(space_vals, model_vals)

    # get bounds
    varbound = np.around(extract_bounds(space_vals_flat, tolerance_mm, for_ga=True)).astype(int)

    # set up global vars used in ga function
    global model_vals_ga, a0_ga, n_ga, space_vals_start_flat_ga, normal_vectors_ga
    model_vals_ga = model_vals
    a0_ga = a0
    n_ga = n
    space_vals_start_flat_ga = space_vals_flat
    normal_vectors_ga = normal_vectors

    # initialize and run model
    model = ga(function = abs_diff_ratio_function_ga, dimension = 9, variable_type = 'int', variable_boundaries = varbound, convergence_curve=False)
    model.run()

    return model.output_dict['variable'].reshape(n, 3).tolist()
