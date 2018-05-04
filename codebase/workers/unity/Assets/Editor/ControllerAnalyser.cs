using UnityEditor;
using UnityEngine;
using System.Collections;
using System.IO;
using System.Text;
using Assets.Gamelogic.Core;

public class ControllerAnalyser: MonoBehaviour
{
    [MenuItem("Drone Sim/Controller Analysis")]
    private static void AnalyseControllers()
    {
        int x = 15750; //31500m
        int y = 7000; //14000m

        int squareSize = SimulationSettings.BIT_SIZE;

        int maxX = x / squareSize;
        int maxZ = y / squareSize;

        ControllerBehaviour[] controllerScripts = FindObjectsOfType<ControllerBehaviour>();
        int numControllers = controllerScripts.Length;
        Vector3[] controllers = new Vector3[numControllers];
        int c = 0;
        foreach (ControllerBehaviour controllerScript in controllerScripts)
        {
            controllers[c++] = controllerScript.gameObject.transform.position;
        }

        Texture2D texture = new Texture2D(maxX*2, maxZ*2, TextureFormat.RGB24, false);
        float floatControllers = controllers.Length;

        for (int i = -maxX; i < maxX; i++)
        {
            for (int j = -maxZ; j < maxZ; j++)
            {
                Vector3 currentPosition = new Vector3(i * squareSize, 0, j * squareSize);

                float closestController = -1;

                float currentClosest = float.MaxValue;
                for (int k = 0; k < numControllers; k++)
                {
                    float calculatedDistance = Vector3.Distance(controllers[k], currentPosition);
                    if (calculatedDistance < currentClosest)
                    {
                        closestController = k;
                        currentClosest = calculatedDistance;
                    }
                }

                float colourVal = closestController / floatControllers;
                texture.SetPixel(i + maxX, j + maxZ, new Color(colourVal, colourVal, colourVal));
            }
        }
        texture.Apply();

        byte[] bytes = texture.EncodeToPNG();
        string filepath = Application.dataPath + "/../../../Controller_Analysis_" + squareSize + ".png";
        File.WriteAllBytes(filepath, bytes);

        Debug.Log("Successfully generated image at " + filepath);


    }

    [MenuItem("Drone Sim/Generate Points of Interest")]
    private static void GeneratePointsOfInterest()
    {
        string filepath = Application.dataPath + "/../../../Points_Of_Interest.txt";

        StringBuilder stringBuilder = new StringBuilder("[");

        ControllerBehaviour[] controllerScripts = FindObjectsOfType<ControllerBehaviour>();
        AppendPoint(stringBuilder, controllerScripts[0].gameObject.transform.position);
        for (int i = 1; i < controllerScripts.Length; i++)
        {
            stringBuilder.Append(",");
            AppendPoint(stringBuilder, controllerScripts[i].gameObject.transform.position);
        }

        stringBuilder.Append("]");

        StreamWriter writer = new StreamWriter(filepath, false);
        writer.WriteLine(stringBuilder.ToString());
        writer.Close();

        Debug.Log("Generated file at " + filepath);
    }

    private static void AppendPoint(StringBuilder stringBuilder, Vector3 point)
    {
        stringBuilder.AppendFormat("{{ \"x\": {0}, \"z\": {1} }}", point.x, point.z);
    }
}
