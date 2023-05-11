using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] private Image healthBarSprite;
        [SerializeField] private float reduceSpeed = 2;

        private float fillAmount = 1;

        public void UpdateHealthBar(float currentHealth, float maxHealth)
            => fillAmount = currentHealth / maxHealth;
        
        private void Update()
            => healthBarSprite.fillAmount = Mathf.MoveTowards(healthBarSprite.fillAmount, fillAmount, reduceSpeed * Time.deltaTime);
    }
}
