var scene = new THREE.Scene();
var largestDimension = 0;
var numPoints = 0;
var pointsList = []

function STLViewer(model, elementID) {
    var elem = document.getElementById(elementID);
    var camera = new THREE.PerspectiveCamera(70,
        elem.clientWidth / elem.clientHeight, 1, 1000);
    var renderer = new THREE.WebGLRenderer({
        antialias: true,
        alpha: true
    });
    renderer.setSize(elem.clientWidth, elem.clientHeight);
    elem.appendChild(renderer.domElement);
    window.addEventListener('resize', function () {
        renderer.setSize(elem.clientWidth, elem.clientHeight);
        camera.aspect = elem.clientWidth / elem.clientHeight;
        camera.updateProjectionMatrix();
    }, false);

    //set up controls
    var controls = new THREE.OrbitControls(camera, renderer.domElement);
    controls.enableDamping = true;
    controls.rotateSpeed = 0.05;
    controls.dampingFactor = 0.1;
    controls.enableZoom = true;
    // controls.autoRotate = true;
    controls.autoRotateSpeed = .75;

    //set up scene

    scene.add(new THREE.HemisphereLight(0xffffff, 1.5));



    //stl loader
    (new THREE.STLLoader()).load(model, function (geometry) {
        var material = new THREE.MeshPhongMaterial({
            color: 0xff5533,
            specular: 100,
            shininess: 100,
            opacity: 0.7,
            transparent: true
        });
        var mesh = new THREE.Mesh(geometry, material);
        scene.add(mesh);




        //place model
        var middle = new THREE.Vector3();
        geometry.computeBoundingBox();
        geometry.boundingBox.getCenter(middle);
        // mesh.geometry.applyMatrix(new THREE.Matrix4().makeTranslation(-middle.x, -middle.y, -middle.z));

        largestDimension = Math.max(geometry.boundingBox.max.x,
            geometry.boundingBox.max.y,
            geometry.boundingBox.max.z)
        camera.position.z = largestDimension * 1.5;



        var size = largestDimension;
        var divisions = 50;

        var gridHelper = new THREE.GridHelper(largestDimension * 5, divisions);
        scene.add(gridHelper);

        //animate model
        var animate = function () {
            requestAnimationFrame(animate);
            controls.update();
            renderer.render(scene, camera);
        };
        animate();


    });

}

function addPoint(pt) {
    console.log("adding point");
    var geometry = new THREE.SphereGeometry(largestDimension / 30, 32, 32);
    var material = new THREE.MeshPhongMaterial({
        color: 0xFF0000
    });
    var sphere = new THREE.Mesh(geometry, material);
    sphere.position.set(pt[0], pt[1], pt[2]);
    scene.add(sphere);
    numPoints += 1;
    pointsList.push(sphere);
    return true;
}

function clearPoints(){
    for(let i=0; i<pointsList.length; i++){
        scene.remove(pointsList[i]);
    }
}

