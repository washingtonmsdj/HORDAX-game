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
        private Quaternion baseLocalRotation;
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
            baseLocalRotation = visual.localRotation;
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

            Quaternion desiredRotation = baseLocalRotation;
            if (weapon.TryGetAimPoint(out Vector3 aimPoint) && visual.parent != null)
            {
                Vector3 worldDirection = aimPoint - visual.position;
                if (worldDirection.sqrMagnitude > 0.001f)
                {
                    Vector3 localDirection = visual.parent.InverseTransformDirection(worldDirection.normalized);
                    desiredRotation = Quaternion.LookRotation(localDirection, Vector3.up);
                }
            }

            visual.localRotation = Quaternion.Slerp(
                visual.localRotation,
                desiredRotation,
                18f * Time.deltaTime);
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
