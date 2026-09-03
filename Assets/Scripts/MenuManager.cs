using UnityEngine;
using UnityEngine.SceneManagement; // Sahneler arası geçiş için bunu eklememiz şart

public class MenuManager : MonoBehaviour
{
    // Oyna butonuna basınca çalışacak
    public void PlayGame()
    {
        // 1 numaralı sahneyi (oyunun olduğu sahne) yükle
        SceneManager.LoadScene(1);
    }

    // Çıkış butonuna basınca çalışacak
    public void QuitGame()
    {
        Debug.Log("Oyundan çıkıldı!"); // Editörde çıkışı görebilmek için
        Application.Quit(); // Gerçek oyunda oyunu kapatır
    }
}