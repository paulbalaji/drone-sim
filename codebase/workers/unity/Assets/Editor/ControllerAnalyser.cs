using UnityEditor;
using UnityEngine;
using System.Collections;
using System.IO;
using Assets.Gamelogic.Core;

public class ControllerAnalyser: MonoBehaviour
{
    [MenuItem("Improbable/Controller Analysis")]
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
}
