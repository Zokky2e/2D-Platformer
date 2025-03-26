using UnityEngine;
using UnityEngine.SceneManagement;

public class SensorManager : Singleton<SensorManager>
{
    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reset sensors to avoid references to destroyed objects
        ResetSensorReferences();
    }

    private void ResetSensorReferences()
    {
        foreach (Transform sensor in transform)
        {
            Collider2D sensorCollider = sensor.GetComponent<Collider2D>();
            if (sensorCollider != null)
            {
                sensorCollider.enabled = false; // Disable and re-enable to reset any references
                sensorCollider.enabled = true;
            }
        }
    }
}
