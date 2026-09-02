using UnityEngine;

namespace HairSalonGame
{
    /// <summary>
    /// Karakter animasyonu ve yaylanma/dönme efektleri kullanıcının talebi doğrultusunda tamamen kaldırılmıştır.
    /// </summary>
    public class PlayerProceduralAnimation : MonoBehaviour
    {
        private void Awake()
        {
            Destroy(this);
        }
    }
}
