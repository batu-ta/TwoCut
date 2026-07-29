using UnityEngine;
using UnityEngine.SceneManagement; // Sahneler arasý geçiþ için bunu eklememiz þart

public class MenuManager : MonoBehaviour
{
    // Oyna butonuna basýnca çalýþacak
    public void PlayGame()
    {
        // 1 numaralý sahneyi (oyunun olduðu sahne) yükle
        SceneManager.LoadScene(1);
    }

    // Çýkýþ butonuna basýnca çalýþacak
    public void QuitGame()
    {
        Debug.Log("Oyundan çýkýldý!"); // Editörde çýkýþý görebilmek için
        Application.Quit(); // Gerçek oyunda oyunu kapatýr
    }
}