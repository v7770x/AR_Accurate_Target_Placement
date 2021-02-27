from flask import render_template, Flask, request, jsonify, redirect, send_from_directory
from app import app
import app.optimization.eqn_solver as solver
from app.optimization.eqn_solver import extract_target_vals
from app.optimization.optimization_methods import minimize_nelder, extract_distances, minimize_nelder_abs_diff
import os
import numpy as np
import app.data_logger as logger

app.config["DEBUG"] = True
CURR_PTH = os.path.dirname(os.path.abspath(__file__))
OBJECTS_PTH = os.path.join(CURR_PTH, "static", "objects")
data = {"points":[[0,0,0], [0,0,0], [0,0,0]], "vectors":[[0,0,0], [0,0,0], [0,0,0]], "curr_stl_name":"Table.stl", "curr_obj_name":"Table.obj"}

@app.route('/')
@app.route('/index')
def index():
    user = {'username': 'meee'}
    return render_template('index.html', title='Home', user=user, curr_stl_name=data.get("curr_stl_name"))

@app.route('/view_data')
def view_data():
    viewable_data = logger.get_optimization_data()
    print(viewable_data)
    return render_template("view_data.html", title="View Data", viewable_data=viewable_data)


@app.route('/update_points', methods=[ 'POST'])
def update_points():
    req_data = request.get_json()
    data['points'] = req_data.get("points")
    print(data)
    return "success"

@app.route('/update_vectors', methods=[ 'POST'])
def update_vectors():
    req_data = request.get_json()
    data['vectors'] = req_data.get("vectors")
    print(data)
    return "success"

@app.route('/get_points', methods=['GET'])
def get_points():
    send_points = [j/1000.0 for sub in data["points"] for j in sub]
    print(send_points)
    return jsonify(send_points)

@app.route('/get_vectors', methods=['GET'])
def get_vectors():
    normalized_vectors = [j/np.linalg.norm(np.array(sub)) for sub in data["vectors"] for j in sub]
    print(normalized_vectors)
    return jsonify(normalized_vectors)

@app.route('/upload_stl', methods=['GET', 'POST'])
def upload_stl():
    if request.method == "POST":
        if request.files:
            #save stl file
            stl_file = request.files["stl"]
            data["curr_stl_name"] = stl_file.filename
            stl_file.save(os.path.join(OBJECTS_PTH, data["curr_stl_name"]))

            #save obj file
            obj_file = request.files["obj"]
            data["curr_obj_name"] = obj_file.filename
            obj_file.save(os.path.join(OBJECTS_PTH, data["curr_obj_name"]))
            print(data["curr_obj_name"], " and ", data["curr_stl_name"], " saved")

            return redirect("/")

    user = {'username': 'meee'}
    return render_template("index.html", title='Home', user=user)


@app.route('/get_uploaded_obj', methods = ['GET','POST'])
def get_uploaded_obj():
    return send_from_directory(OBJECTS_PTH, data["curr_obj_name"], as_attachment=True)

@app.route('/run_optimization', methods = ['POST'])
def run_optimization():
    print("\nrunning optimization")

    #extract data from request
    req_data = request.get_json()
    spacePoints = np.array(req_data.get("spacePoints"))
    normalVectors = np.array(req_data.get("normalVectors"))
    print("space", spacePoints)
    print("normals", normalVectors)
    modelPoints = data["points"]
    print("model", modelPoints)

    # modelPoints = [[0,0,0], [304.8,0,0] , [0, 1828.8, 0]]
    # spacePoints = np.array([[730.6, -1540.2, 2078.6], [958.3, -1538.1, 1867.9], [-500.1, -1520.3, 718]])/1000

    #try optimization
    (output_list, n) = solver.try_all_eqn_combos(modelPoints, (spacePoints)*1000)

    #extract angles and distances of points measured and log
    (distances, angles) = solver.extract_target_vals(spacePoints, True)
    logger.save_optimization_data(modelPoints, spacePoints, distances, angles, normalVectors)
    print("num optimization points: ", n)
    return jsonify(output_list)

@app.route('/optimize', methods = ['POST'])
def optimize():
    print("\n Optimizing!")

    func = minimize_nelder_abs_diff

    #extract data from request
    req_data = request.get_json()
    space_points = np.array(req_data.get("spacePoints"))*1000
    model_points = data["points"]
    print("space: ", space_points)
    print("model: ", model_points)
    print("space distances: ", extract_target_vals(space_points) )
    print("model distances: ", extract_target_vals(model_points) )
    print("distace ratio: ", np.divide(extract_target_vals(space_points), extract_target_vals(model_points) ))

    #run optimization with Nelder-Mead function
    updated_space_points =  func(space_points, model_points)
    print("updated space: ", updated_space_points)
    print("updated space distances: ", extract_target_vals(updated_space_points) )
    print("diff space distances (new-old): ", np.subtract(np.array(updated_space_points), np.array(space_points)))
    print("model distances: ", extract_target_vals(model_points) )
    print("updated distace ratio: ", np.divide(extract_target_vals(updated_space_points), extract_target_vals(model_points) ))

    return jsonify(updated_space_points)

    