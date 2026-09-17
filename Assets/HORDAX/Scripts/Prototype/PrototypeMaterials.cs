using UnityEngine;

namespace HORDAX.Prototype
{
    public static class PrototypeMaterials
    {
        private static Material road;
        private static Material rail;
        private static Material player;
        private static Material enemy;
        private static Material elite;
        private static Material boss;
        private static Material gate;
        private static Material pickup;
        private static Material bullet;
        private static Material finish;
        private static Material heal;
        private static Material reward;
        private static Material hazard;

        public static Material Road => road ?? (road = Create("Road", new Color(0.24f, 0.27f, 0.32f)));
        public static Material Rail => rail ?? (rail = Create("Rail", new Color(0.10f, 0.14f, 0.20f)));
        public static Material Player => player ?? (player = Create("Player", new Color(0.08f, 0.48f, 0.95f)));
        public static Material Enemy => enemy ?? (enemy = Create("Enemy", new Color(0.45f, 0.95f, 0.35f)));
        public static Material Elite => elite ?? (elite = Create("Elite", new Color(1.00f, 0.48f, 0.12f)));
        public static Material Boss => boss ?? (boss = Create("Boss", new Color(0.75f, 0.12f, 0.18f)));
        public static Material Gate => gate ?? (gate = Create("Gate", new Color(0.10f, 0.75f, 1.00f)));
        public static Material Pickup => pickup ?? (pickup = Create("Pickup", new Color(1.00f, 0.82f, 0.15f)));
        public static Material Bullet => bullet ?? (bullet = Create("Bullet", new Color(1.00f, 0.95f, 0.55f)));
        public static Material Finish => finish ?? (finish = Create("Finish", new Color(0.85f, 0.20f, 0.95f)));
        public static Material Heal => heal ?? (heal = Create("Heal", new Color(0.15f, 0.95f, 0.55f)));
        public static Material Reward => reward ?? (reward = Create("Reward", new Color(1.00f, 0.68f, 0.08f)));
        public static Material Hazard => hazard ?? (hazard = Create("Hazard", new Color(0.95f, 0.10f, 0.12f)));

        private static Material Create(string label, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            Material material = new Material(shader) { name = "HORDAX " + label };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            material.color = color;
            return material;
        }
    }
}
