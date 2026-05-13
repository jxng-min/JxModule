using UnityEngine;

namespace JxModule.CharFX
{
    public struct CharQuad
    {
        public Vector3 V0;
        public Vector3 V1;
        public Vector3 V2;
        public Vector3 V3;
        
        public Vector3 MidPoint => (V0 + V2) * 0.5f;
        
        public CharQuad(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3)
        {
            V0 = v0;
            V1 = v1;
            V2 = v2;
            V3 = v3;
        }

        public void Translate(Vector3 offset)
        {
            V0 += offset;
            V1 += offset;
            V2 += offset;
            V3 += offset;
        }

        public void ScaleAround(Vector3 center, float scale)
        {
            V0 = center + (V0 - center) * scale;
            V1 = center + (V1 - center) * scale;
            V2 = center + (V2 - center) * scale;
            V3 = center + (V3 - center) * scale;
        }

        public void RotateAround(Vector3 center, float angleDegrees)
        {
            var rotation = Quaternion.Euler(0f, 0f, angleDegrees);

            V0 = center + rotation * (V0 - center);
            V1 = center + rotation * (V1 - center);
            V2 = center + rotation * (V2 - center);
            V3 = center + rotation * (V3 - center);
        }
    }
}