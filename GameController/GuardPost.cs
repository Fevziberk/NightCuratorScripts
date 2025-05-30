using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class GuardPost : MonoBehaviour
{
    [Tooltip("Name of the scene to load when the player walks in")]
    public CanvasGroup finishCanvas;
    public CanvasGroup breakCanvas;
    public float fadeDuration = 1f;
    public string nextSceneName = "MAP2";
    public AudioClip enterSfx;

    AudioSource audioSource;
    float t2 = 0f;
    void Start()
    {
        if (finishCanvas != null) finishCanvas.alpha = 0f;
        
        
    }
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }
    public IEnumerator OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            breakCanvas.alpha = 0f;

        if (enterSfx != null)
            audioSource.PlayOneShot(enterSfx);
        while (t2 < fadeDuration)
        {
            t2 += Time.deltaTime;
            finishCanvas.alpha = Mathf.Clamp01(t2 / fadeDuration);

        }
        yield return new WaitForSeconds(6f);

        SceneManager.LoadScene(nextSceneName);
    }
}