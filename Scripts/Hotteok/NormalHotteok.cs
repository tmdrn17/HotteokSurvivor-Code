using Study.Utilities;
using UnityEngine;
using UnityEngine.UIElements;

namespace HotteokSurvivor
{
    public class NormalHotteok : Hotteok
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            RotateZ();
        }

        protected override void Move()
        {
            transform.Translate(Vector3.up * (shotSpeed * Time.deltaTime));
        }

        protected override void SendDamage()
        {
            enemy.TakeDamage(damage);
            transform.position = startPos.position;
            gameObject.SetActive(false);
        }

        private void RotateZ()
        {
            if (enemy != null)
            {
                Vector3 goalPosition = enemy.transform.position;
                Vector3 myPosition = transform.position;
                Vector3 dir = (goalPosition - myPosition).normalized;
                transform.up = dir;
            }
            else
            {
                Vector2 mousePosition = SimpleInput.GetMousePosition();
                
                Vector3 worldMousePoint = Camera.main.ScreenToWorldPoint(mousePosition);

                Vector3 myPosition = transform.position;
                
                worldMousePoint.z = transform.position.z;

                Vector3 dir = (worldMousePoint - myPosition).normalized;
                transform.up = dir;
            }
        }

    }
}