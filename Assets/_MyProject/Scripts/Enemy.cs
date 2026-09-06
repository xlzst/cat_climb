using UnityEngine;

namespace MyProject
{
    public class Enemy : MonoBehaviour
    {
        public void TakeDamage()
        {
            Debug.Log($"[Enemy] {gameObject.name} took damage and was defeated!");

            int defeated = PlayerPrefs.GetInt("CatClimb_DefeatedEnemies", 0);
            PlayerPrefs.SetInt("CatClimb_DefeatedEnemies", defeated + 1);
            PlayerPrefs.Save();

            // Deactivate the enemy GameObject to recycle it back into the PlatformManager's pool
            gameObject.SetActive(false);
        }
    }
}
