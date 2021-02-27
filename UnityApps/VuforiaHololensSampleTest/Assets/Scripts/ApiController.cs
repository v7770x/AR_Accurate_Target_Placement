using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System;
using System.IO;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine.Networking;
using MathNet.Numerics.Distributions;

public class JsonHelper
{
    public static T[] getJsonArray<T>(string json)
    {
        string newJson = "{ \"array\": " + json + "}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
        return wrapper.array;
    }

    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] array;
    }
}
public class ApiController : MonoBehaviour
{
    [Serializable]
    public class PointsData
    {
        public double[,] points;
    }

    public static string GetFullEndpoint(string endpoint)
    {
        return "http://" + ImageTargetHandler.storageData.ipAddress + ":5000/" +endpoint;
    }


    public static IEnumerator GetModelPointsAsync(Action<double[,]> onSuccess)
    {
        string endpoint = GetFullEndpoint("get_points");
        using (UnityWebRequest req = UnityWebRequest.Get(endpoint))
        {
            yield return req.Send();
            while (!req.isDone)
            {
                yield return null;
            }
            if (req.isNetworkError)
            {
                print("Get Request Failed");
            }
            else
            {
                byte[] result = req.downloadHandler.data;
                string jsonResponse = System.Text.Encoding.Default.GetString(result);
                print("json Response: " + jsonResponse);

                double[,] modelPoints = deserializeJsonDoubleArray(jsonResponse);
                /*
                double[] jsonPoints = JsonHelper.getJsonArray<double>(jsonResponse);
                double[,] modelPoints = new double[ImageTargetHandler.NUM_TARGETS, 3];
                for (int i = 0; i < ImageTargetHandler.NUM_TARGETS; i++)
                {
                    int start_ind = i * 3;
                    modelPoints[i, 0] = jsonPoints[start_ind];
                    modelPoints[i, 1] = jsonPoints[start_ind + 1];
                    modelPoints[i, 2] = jsonPoints[start_ind + 2];
                }
                */

                onSuccess(modelPoints);
            }

        }
    }

    public static IEnumerator GetModelVectorsAsync(Action<double[,]> onSuccess)
    {
        string endpoint = GetFullEndpoint("get_vectors");
        using (UnityWebRequest req = UnityWebRequest.Get(endpoint))
        {
            yield return req.Send();
            while (!req.isDone)
            {
                yield return null;
            }
            if (req.isNetworkError)
            {
                print("Get Request Failed");
            }
            else
            {
                byte[] result = req.downloadHandler.data;
                string jsonResponse = System.Text.Encoding.Default.GetString(result);
                print("json Response: " + jsonResponse);

                double[,] modelVectors = deserializeJsonDoubleArray(jsonResponse);
                onSuccess(modelVectors);
            }

        }
    }


    public static IEnumerator SetModelObjectAsync(GameObject objHolder)
    {
        string endpoint = GetFullEndpoint("get_uploaded_obj");
        using (UnityWebRequest uwr = new UnityWebRequest(endpoint))
        {
            string savePath = Application.persistentDataPath + "/object.obj";
            uwr.downloadHandler = new DownloadHandlerFile(savePath);
            yield return uwr.SendWebRequest();
            if (uwr.isNetworkError || uwr.isHttpError)
            {
                print("Setting model failed");
            }
            else
            {
                //load model
                Mesh holderMesh = new Mesh();
                ObjImporter newMesh = new ObjImporter();
                holderMesh = newMesh.ImportFile(Application.persistentDataPath + "/object.obj");

                //set mesh filter object to loaded model
                MeshFilter filter = objHolder.GetComponent<MeshFilter>();
                filter.mesh = holderMesh;
            }
        }
    }

    static string serializeDoubleArrayToJson(double[,] matrix, string name, bool openBracket, bool closeBracket)
    {
        string output = "";
        if (openBracket)
        {
            output += "{";
        }
        else
        {
            output += ",";
        }
        output += "\"" + name + "\":[";
        for (int i = 0; i < matrix.GetLength(0); i++)
        {
            output += "[";
            for (int j = 0; j < matrix.GetLength(1); j++)
            {
                output += matrix[i, j];
                if(j!= matrix.GetLength(1)-1)
                    output+= ", ";
            }
            if (i != matrix.GetLength(0) - 1)
                output += "],";
            else
                output += "]";
        }
        output += "]";
        if (closeBracket)
        {
            output += "}";
        }
        print("serialized double array: " + output);
        return output;
    }

    static double[,] deserializeJsonDoubleArray(string jsonResponse)
    {
        double[] jsonPoints = JsonHelper.getJsonArray<double>(jsonResponse);
        double[,] deserializedArray = new double[ImageTargetHandler.NUM_TARGETS, 3];
        for (int i = 0; i < ImageTargetHandler.NUM_TARGETS; i++)
        {
            int start_ind = i * 3;
            deserializedArray[i, 0] = jsonPoints[start_ind];
            deserializedArray[i, 1] = jsonPoints[start_ind + 1];
            deserializedArray[i, 2] = jsonPoints[start_ind + 2];
        }

        return deserializedArray;
    }

    public static IEnumerator RunOptimizationAsync(Action<List<double[,]>> onSuccess, double[,] spacePoints, double [,] normalVectors)
    {
        //call api
        string endpoint = GetFullEndpoint("run_optimization");
        using (UnityWebRequest uwr = new UnityWebRequest(endpoint, "POST"))
        {
            string json = serializeDoubleArrayToJson(spacePoints, "spacePoints", true, false) + serializeDoubleArrayToJson(normalVectors, "normalVectors", false, true);
            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
            uwr.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
            uwr.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");

            //send request and yield till returned
            yield return uwr.SendWebRequest();
            if (uwr.isNetworkError || uwr.isHttpError)
            {
                print("Running optimization failed");
            }
            else
            {
                byte[] result = uwr.downloadHandler.data;
                string jsonResponse = System.Text.Encoding.Default.GetString(result);
                print("json Response: " + jsonResponse);
                double[] jsonPoints = JsonHelper.getJsonArray<double>(jsonResponse);
                List<double[,]> possibleNewPoints = new List<double[,]>();
                int numSets = jsonPoints.Length/9;
                print(numSets);
                for(int j = 0; j<numSets; j++)
                {
                    double[,] set =  new double[ImageTargetHandler.NUM_TARGETS, 3];
                    for (int i = 0; i < ImageTargetHandler.NUM_TARGETS; i++)
                    {
                        int start_ind = j*9 + i * 3;
                        set[i, 0] = jsonPoints[start_ind];
                        set[i, 1] = jsonPoints[start_ind + 1];
                        set[i, 2] = jsonPoints[start_ind + 2];
                    }
                    possibleNewPoints.Add(set);
                }
                onSuccess(possibleNewPoints);

            }
        }

    }
}
