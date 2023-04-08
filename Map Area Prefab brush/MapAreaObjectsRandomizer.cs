using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapAreaObjectsRandomizer : MonoBehaviour, IMapAreaRandomizer
{
    public GameObject MapAreaDefaultObjects;

    public List<GameObject> replacementObjects;

    public void Randomize()
    {
        //Get a random number between 0 and replacementObjects.Count
        Unity.Mathematics.Random random = new Unity.Mathematics.Random((uint)Time.time.ToString().GetHashCode());
        int replacementIndex = random.NextInt(0, replacementObjects.Count + 1);
        
        //If we get a value of count + 1 (outside or list), we use the default object and delete replacements
        if(replacementIndex == replacementObjects.Count)
        {
            for(int i = 0; i <  replacementObjects.Count; i++)
            {
                GameObject.DestroyImmediate(replacementObjects[i]);
            }
        }
        //Otherwise, we delete the defaults and the objects not at our selected index 
        else 
        {
            GameObject.DestroyImmediate(MapAreaDefaultObjects);
            replacementObjects[replacementIndex].SetActive(true);

            for (int i = 0; i < replacementObjects.Count; i++)
            {   
                if(i == replacementIndex)
                {
                    continue;
                }
                GameObject.DestroyImmediate(replacementObjects[i]);
            }
        }
        
    
    }
}
