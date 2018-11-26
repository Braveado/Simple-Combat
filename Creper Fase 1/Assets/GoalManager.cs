using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoalManager : MonoBehaviour
{
    public GameObject goal;
    public Transform[] goalPositions;        
	
	void Start ()
    {
        float chance = 1f / goalPositions.Length;

        bool spawned = false;
        int index = 0;

        while(!spawned)
        {
            if (Random.value <= chance)
            {
                SpawnGoal(goal, goalPositions[index]);
                spawned = true;
            }

            index++;
            if (index >= goalPositions.Length)
                index = 0;
        }
	}

    private void SpawnGoal(GameObject obj, Transform pos)
    {
        Instantiate(obj, pos.position, Quaternion.identity, transform);
    }
	
}
