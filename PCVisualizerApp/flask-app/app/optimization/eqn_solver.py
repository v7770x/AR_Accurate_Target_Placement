from scipy.optimize import fsolve
import numpy as np 
from itertools import combinations

#constants
DELTA_MAX_MARGIN = 10

'''
    function with all simultaneous equations set up
    output in form [x1, y1, z1, x2, y2, z2, z3, y3, z3]
    params: outputs = list of intial guesses for outputs
            target_vals = list with model distances, angles
            fixed_indices = list of indices assumed to be correct in set of points given
            fixed_vals = list of values corresponding to fixed indices
'''
def eqn_function(output, target_vals, fixed_indices, fixed_vals):
    m = output
    # print(output)
    F = np.empty((9))
    F[0] = get_distance(m[0:3], m[3:6] ) - target_vals[0]
    F[1] = get_distance(m[0:3], m[6:9]) - target_vals[1]
    F[2] = get_distance(m[3:6], m[6:9]) - target_vals[2]
    F[3] = get_angle(m[0:3] - m[6:9], m[3:6] - m[6:9]) - target_vals[3]
    F[4] = get_angle(m[6:9] - m[3:6], m[0:3] - m[3:6]) - target_vals[4]
    F[5] = m[fixed_indices[0]] - fixed_vals[0]
    F[6] = m[fixed_indices[1]] - fixed_vals[1]
    F[7] = m[fixed_indices[2]] - fixed_vals[2]
    F[8] = m[fixed_indices[3]] - fixed_vals[3]
    return F

def get_distance(p1, p2):
    return np.linalg.norm(p1-p2)

def get_angle(v1, v2):
    return (np.dot(v1,v2)/(np.linalg.norm(v1) * np.linalg.norm(v2)) )

'''
 Given set of points in list, find distances and angles between points
 returns in format [d12, d13, ... d1n, d23, d24, ...d2n ... dn-1n, th123, th132]
'''
def extract_target_vals(points):
    #extract properties of points
    n = len(points)
    points = np.array(points)
    target_vals = []

    #iterate over all points and get distances
    for i in range(n-1):
        for j in range(i+1,n):
            target_vals.append(get_distance(points[i],points[j]))

    #extract angles (TODO) scale to allow for any number of points and extract all angles
    target_vals.append(get_angle(points[0] - points[2], points[1] - points[2]))
    target_vals.append(get_angle(points[2] - points[1], points[0] - points[1]))

    return np.array(target_vals)


'''
    function to get all possible index combos for equation
'''
def get_all_eqn_combos():
    inds = list(range(9))
    return list(combinations(inds, 4))

def try_all_eqn_combos(model_points, space_points):
    #get all combos to iterate through
    eqn_combos = get_all_eqn_combos()

    #set initial guess and target values for equations
    initialGuess = np.array(space_points).reshape(9)
    model_target_vals = extract_target_vals(model_points)
    output_list = []
    print("model target values: ", model_target_vals)
    print("space points", space_points)

    #calculate target_values for the space points
    space_target_vals = extract_target_vals(space_points)
    delta_space_target_vals = space_target_vals - model_target_vals

    n = 0
    #iterate through combos
    for ind, inds in enumerate(eqn_combos):
        #extract fixed indices and vals
        fixed_indices = list(inds)
        fixed_vals = [initialGuess[i] for i in fixed_indices]

        #solve equations with given parameters and fixed points
        updated_vals_normal = fsolve(eqn_function, initialGuess, (model_target_vals, fixed_indices, fixed_vals))
        updated_vals = np.array(updated_vals_normal)

        #calculate new difference in the distances and angles, points
        delta_points = np.abs(initialGuess - updated_vals)
        # print(delta_points)
        if(not np.any(delta_points > DELTA_MAX_MARGIN) and not(np.all(delta_points < 0.001))):
            output_list.append(updated_vals_normal)
            n+=1
            # print(n, updated_vals.reshape(3,3))

        # delta_target = extract_target_vals(updated_vals) - model_target_vals
        # print("delta_target_vals ", ind, ": ", delta_target)
        # print("new points", updated_vals)
        # print("old points", space_points)
        # print("points diff = ", np.abs(np.array(space_points) - updated_vals))
        # print("")

        # print(fixed_indices, updated_vals, )
        
    # print(np.array(output_list))
    return ((np.array(output_list)/1000).reshape(len(output_list)*9).tolist(), len(output_list))

# model_points = [[0,0,0], [304.8,0,0] , [0, 1828.8, 0]]
# space_points = [[730.6, -1540.2, 2078.6], [958.3, -1538.1, 1867.9], [-500.1, -1520.3, 718]]
# output_points = try_all_eqn_combos(model_points, space_points)
#model and space points
# model_points = [[0.25, 0.25, 0 ], [0.25, -0.25, 0] , [-0.25, 0.25, 0]]
# space_points = [[-0.6659, -0.5421, -0.7766], [-0.2835, -0.5464, -0.5027], [-0.3628, -0.5376, -1.1833]]
# { { 0.25, 0.25, 0 }, { 0.25, -0.25, 0 }, { -0.25, 0.25, 0 } }
# { { -0.6659, -0.5421, -0.7766 }, { -0.2835, -0.5464, -0.5027 }, { -0.3628, -0.5376, -1.1833 } }

# model_points = [[0,0,0], [304.8,0,0] , [0, 1828.8, 0]]
# space_points = [[730.6, -1540.2, 2078.6], [958.3, -1538.1, 1867.9], [-500.1, -1520.3, 718]]
# target_vals = extract_target_vals(model_points)
# fixed_indices = [0,1,2,5]
# fixed_vals = [730.6, -1540.2, 2078.6, 1867.9]
# guess = [730.6, -1540.2, 2078.6, 958.3, -1538.1, 1867.9, -500.1, -1520.3, 718]

# print("possible combinations", len(get_all_eqn_combos()))

# print("target values model: ", target_vals)
# print("target values space: ", extract_target_vals(space_points) )

# updated_vals = fsolve(eqn_function, guess, (target_vals, fixed_indices, fixed_vals))
# print("output update values", updated_vals)



'''
Model
(x1, y1, z1)
(x2, y2, z2)
..

Space
(x1, y1, z1)
(x2, y2, z2)
..

delta_x12_model = delta_x12_space
delta_y12_model = delta_y12_space
...

->get 9 equations
assume 3 components (i.e x1_space, y_2_space, z_3_space) -> can get solns for rest of coords

'''