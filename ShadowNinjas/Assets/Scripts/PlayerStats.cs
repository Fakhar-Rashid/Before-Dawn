using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerStats : MonoBehaviour
{
    public float shuriken;
    public float life;
    public int sceneIndex;
    public Image shuriken1;
    public Image shuriken2;
    public Image shuriken3;
    public Image life1;
    public Image life2;
    public Image life3;
    public GameObject levelLoseMsg;
    public GameObject retrieve;
    public GameObject plant;
    public GameObject explosive1;
    public GameObject explosive2;
    public GameObject goal;
    private bool lost;
    private bool explosives;
    private int planted;
    AsyncOperation asyncLoad;
    public Camera cam;

    void Start()
    {
        InitializePlayerData();
        SetupSceneTransition();
    }

    void InitializePlayerData()
    {
        // Set default values first
        shuriken = 3;
        life = 3;
        lost = false;
        explosives = false;
        planted = 0;
        sceneIndex = SceneManager.GetActiveScene().buildIndex;

        // Then try to load saved data
        try
        {
            PlayerData data = SaveSystem.LoadPlayer();
            if (data != null)
            {
                sceneIndex = data.scene;
                shuriken = data.shuriken;
                life = data.life;
                
                // Only apply position if we're loading the same scene
                if (sceneIndex == SceneManager.GetActiveScene().buildIndex)
                {
                    transform.position = new Vector3(
                        data.position[0],
                        data.position[1],
                        data.position[2]
                    );
                }
            }
            else
            {
                // Create initial save if none exists
                SavePlayer();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Loading failed, using defaults. Error: " + e.Message);
            SavePlayer(); // Create new save file
        }
    }

    void SetupSceneTransition()
    {
        if (SceneManager.GetActiveScene().buildIndex != SavedInfo.scene)
        {
            SavedInfo.scene = SceneManager.GetActiveScene().buildIndex;
            SavedInfo.life = 3;
        }
    }

    void Update()
    {
        UpdateGameState();
        UpdateUI();
    }

    void UpdateGameState()
    {
        if (planted == 2 && goal != null)
        {
            goal.gameObject.SetActive(true);
        }

        // Sync with SavedInfo
        life = Mathf.Clamp(SavedInfo.life, 0, 3);
        SavedInfo.shuriken = Mathf.Clamp(shuriken, 0, 3);
    }

    void UpdateUI()
    {
        // Update shuriken UI
        if (shuriken1 != null) shuriken1.enabled = shuriken >= 1;
        if (shuriken2 != null) shuriken2.enabled = shuriken >= 2;
        if (shuriken3 != null) shuriken3.enabled = shuriken >= 3;

        // Update life UI
        if (life1 != null) life1.enabled = life >= 1;
        if (life2 != null) life2.enabled = life >= 2;
        if (life3 != null) life3.enabled = life >= 3;

        if (life <= 0 && cam != null)
        {
            var controller = cam.GetComponentInParent<CameraController>();
            if (controller != null) controller.pan = false;
        }
    }

    public void UseShuriken()
    {
        shuriken = Mathf.Max(0, shuriken - 1);
    }

    public void AddShuriken()
    {
        shuriken = Mathf.Min(3, shuriken + 1);
    }

    public void UseLife()
    {
        life = Mathf.Max(0, life - 1);
        SavedInfo.life = life;
    }

    public void AddLife()
    {
        life = Mathf.Min(3, life + 1);
        SavedInfo.life = life;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Lose") && !lost)
        {
            UseLife();
            lost = true;
        }
        else if (other.gameObject.CompareTag("Arrow") && !lost)
        {
            UseLife();
            lost = true;
            if (levelLoseMsg != null)
            {
                levelLoseMsg.SetActive(true);
                StartCoroutine(Close(3));
            }
        }

        if (other.gameObject.CompareTag("Ammo"))
        {
            shuriken = 3;
            other.gameObject.SetActive(false);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Retrieve") && retrieve != null)
        {
            retrieve.SetActive(false);
        }

        if ((other.gameObject.CompareTag("Plant1") || other.gameObject.CompareTag("Plant2")) && plant != null)
        {
            plant.SetActive(false);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        HandleRetrieveInteraction(other);
        HandlePlantInteraction(other);
    }

    void HandleRetrieveInteraction(Collider other)
    {
        if (other.gameObject.CompareTag("Retrieve"))
        {
            if (retrieve != null) retrieve.SetActive(true);

            if (Input.GetKey(KeyCode.E))
            {
                other.gameObject.SetActive(false);
                if (retrieve != null) retrieve.SetActive(false);
                explosives = true;
            }
        }
    }

    void HandlePlantInteraction(Collider other)
    {
        if ((other.gameObject.CompareTag("Plant1") || other.gameObject.CompareTag("Plant2")) && plant != null)
        {
            plant.SetActive(true);

            if (Input.GetKey(KeyCode.E) && explosives)
            {
                other.gameObject.SetActive(false);
                plant.SetActive(false);
                planted++;

                if (other.gameObject.CompareTag("Plant1") && explosive1 != null)
                {
                    explosive1.SetActive(true);
                }
                else if (other.gameObject.CompareTag("Plant2") && explosive2 != null)
                {
                    explosive2.SetActive(true);
                }
            }
        }
    }

    public void SavePlayer()
    {
        try
        {
            SaveSystem.SavePlayer(this);
            Debug.Log("Game saved successfully to: " + Application.persistentDataPath);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Save failed: " + e.Message);
        }
    }

    public void LoadPlayer()
    {
        try
        {
            PlayerData data = SaveSystem.LoadPlayer();
            if (data != null)
            {
                StartCoroutine(LoadLevelAsync(data.scene));
                shuriken = data.shuriken;
                life = data.life;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Load failed: " + e.Message);
        }
    }

    IEnumerator LoadLevelAsync(int sceneIndex)
    {
        asyncLoad = SceneManager.LoadSceneAsync(sceneIndex);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    IEnumerator Close(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        if (levelLoseMsg != null) levelLoseMsg.SetActive(false);
    }
}