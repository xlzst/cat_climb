using UnityEngine;

namespace MyProject
{
    public class Enemy : MonoBehaviour
    {
        public void TakeDamage()
        {
            Debug.Log($"[Enemy] {gameObject.name} took damage and was defeated!");
            
            // Deactivate the enemy GameObject to recycle it back into the PlatformManager's pool
            gameObject.SetActive(false);
        }
    }
}
