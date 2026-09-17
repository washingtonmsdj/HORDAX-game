using UnityEngine;
using HORDAX.Combat;
using HORDAX.Data;

namespace HORDAX.Prototype
{
    public sealed class PrototypeWeaponView : MonoBehaviour
    {
        private WeaponController weapon;
        private Transform visual;
        private Vector3 baseLocalPosition;
        private Vector3 targetScale;
        private float recoil;

        public void Initialize(WeaponController controller, Transform visualRoot)
        {
            if (weapon != null)
            {
                weapon.ShotFired -= OnShot;
                weapon.WeaponChanged -= Refresh;
            }

            weapon = controller;
            visual = visualRoot;
            if (visual == null || weapon == null) return;

            baseLocalPosition = visual.localPosition;
            weapon.ShotFired += OnShot;
            weapon.WeaponChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (weapon == null) return;
            weapon.ShotFired -= OnShot;
            weapon.WeaponChanged -= Refresh;
        }

        private void Update()
        {
            if (visual == null || weapon == null) return;

            recoil = Mathf.MoveTowards(recoil, 0f, Time.deltaTime * 2.8f);
            visual.localPosition = baseLocalPosition + Vector3.back * recoil;
            visual.localScale = Vector3.Lerp(visual.localScale, targetScale, 12f * Time.deltaTime);
        }

        private void OnShot()
        {
            recoil = Mathf.Max(recoil, weapon.RecoilKick);
        }

        private void Refresh()
        {
            if (visual == null || weapon == null) return;

            switch (weapon.Archetype)
            {
                case WeaponArchetype.SMG:
                    targetScale = new Vector3(0.22f, 0.20f, 0.78f);
                    break;
                case WeaponArchetype.Shotgun:
                    targetScale = new Vector3(0.24f, 0.22f, 1.38f);
                    break;
                case WeaponArchetype.Minigun:
                    targetScale = new Vector3(0.32f, 0.28f, 1.28f);
                    break;
                default:
                    targetScale = new Vector3(0.18f, 0.18f, 1.10f);
                    break;
            }
        }
    }
}
