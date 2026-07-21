using UnityEngine;

namespace HairSalonGame
{
    public enum ItemType
    {
        Scissors,       // Makas
        ShampooBottle,  // Şampuan
        DyeBottle_Red,  // Saç Boyası (Kırmızı)
        DyeBottle_Blonde,// Saç Boyası (Sarı)
        HairDryer,      // Fön Makinesi
        Towel,          // Havlu
        Broom           // Süpürge / Temizlik Aleti
    }

    public class SalonItem : MonoBehaviour
    {
        [Header("Item Properties")]
        public ItemType itemType = ItemType.Scissors;
        public string itemName = "TwoCut Tool";
        public Color itemColor = Color.white;

        private void Start()
        {
            Renderer rend = GetComponent<Renderer>();
            if (rend != null && itemType.ToString().StartsWith("DyeBottle"))
            {
                rend.material.color = itemColor;
            }
        }
    }
}
